using Core.Interfaces;
using Core.Models;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using System.IO;
using System.Text.RegularExpressions;

namespace Services;

public class AnimationGenerator : IAnimationGenerator
{
    private readonly ILogger<AnimationGenerator> _logger;

    // 背景图片模板（实际项目中应该有更多预设背景）
    private readonly Dictionary<string, string> _backgroundTemplates = new()
    {
        { "现代都市", "urban_background.jpg" },
        { "玄幻世界", "fantasy_background.jpg" },
        { "古代江湖", "ancient_background.jpg" },
        { "校园", "school_background.jpg" },
        { "办公室", "office_background.jpg" },
        { "战斗场景", "battle_background.jpg" },
        { "修炼场景", "cultivation_background.jpg" }
    };

    // 文字位置模板
    private readonly List<Rectangle> _textPositions = new()
    {
        new Rectangle(100, 100, 1720, 200),  // 上方
        new Rectangle(100, 440, 1720, 200),  // 中间
        new Rectangle(100, 780, 1720, 200),  // 下方
        new Rectangle(100, 300, 800, 400),   // 左侧
        new Rectangle(1020, 300, 800, 400)   // 右侧
    };

    public AnimationGenerator(ILogger<AnimationGenerator> logger)
    {
        _logger = logger;
    }

    public async Task<string> GenerateAnimationAsync(GeneratedStory story, VideoConfig config, string outputPath)
    {
        try
        {
            Directory.CreateDirectory(outputPath);

            var paragraphs = story.Paragraphs.Where(p => p.Trim().Length > 0).ToArray();
            var scenePaths = await GenerateSceneAnimationsAsync(paragraphs, config, outputPath);

            return Path.Combine(outputPath, $"{story.Id}_animation.mp4");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate animation for story: {StoryId}", story.Id);
            throw;
        }
    }

    public async Task<List<string>> GenerateSceneAnimationsAsync(string[] paragraphs, VideoConfig config, string outputPath)
    {
        var scenePaths = new List<string>();

        for (int i = 0; i < paragraphs.Length; i++)
        {
            var paragraph = paragraphs[i].Trim();
            if (string.IsNullOrEmpty(paragraph)) continue;

            var scenePath = await GenerateSceneImageAsync(paragraph, i, config, outputPath);
            scenePaths.Add(scenePath);
        }

        return scenePaths;
    }

    private async Task<string> GenerateSceneImageAsync(string text, int sceneIndex, VideoConfig config, string outputPath)
    {
        var imagePath = Path.Combine(outputPath, $"scene_{sceneIndex:D4}.png");

        // 创建画布
        using var surface = SKSurface.Create(new SKImageInfo(config.VideoWidth, config.VideoHeight));
        using var canvas = surface.Canvas;

        // 绘制背景
        await DrawBackgroundAsync(canvas, config, sceneIndex);

        // 分段处理文字
        var textSegments = SplitTextIntoSegments(text, 50); // 每段最多50字
        var positionIndex = sceneIndex % _textPositions.Count;

        for (int i = 0; i < textSegments.Count; i++)
        {
            var position = _textPositions[positionIndex];
            var yPos = position.Y + (i * 60); // 每行间隔60像素

            // 绘制文字背景框
            DrawTextBackground(canvas, new Rectangle(position.X, yPos - 10, position.Width, 70));

            // 绘制文字
            DrawText(canvas, textSegments[i], new SKPoint(position.X + 20, yPos + 40));
        }

        // 添加动画效果标识
        DrawAnimationIndicator(canvas, config.AnimationStyle, sceneIndex);

        // 保存图片
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.OpenWrite(imagePath);
        await data.SaveToAsync(stream);

        return imagePath;
    }

    private async Task DrawBackgroundAsync(SKCanvas canvas, VideoConfig config, int sceneIndex)
    {
        try
        {
            // 尝试加载背景图片
            var backgroundPath = GetBackgroundPath(sceneIndex);
            if (File.Exists(backgroundPath))
            {
                using var bitmap = SKBitmap.Decode(backgroundPath);
                if (bitmap != null)
                {
                    canvas.DrawBitmap(bitmap, new SKRect(0, 0, config.VideoWidth, config.VideoHeight));
                    return;
                }
            }

            // 如果没有背景图片，生成渐变背景
            var gradientType = sceneIndex % 3;
            switch (gradientType)
            {
                case 0:
                    DrawLinearGradient(canvas, config, new SKColor(25, 25, 112), new SKColor(70, 130, 180)); // 深蓝到浅蓝
                    break;
                case 1:
                    DrawLinearGradient(canvas, config, new SKColor(75, 0, 130), new SKColor(138, 43, 226)); // 深紫到浅紫
                    break;
                case 2:
                    DrawLinearGradient(canvas, config, new SKColor(25, 25, 25), new SKColor(105, 105, 105)); // 深灰到浅灰
                    break;
            }

            // 添加装饰元素
            DrawDecorations(canvas, config, sceneIndex);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to draw background for scene {SceneIndex}", sceneIndex);
            // 绘制纯色背景作为备选
            canvas.Clear(SKColors.Black);
        }
    }

    private void DrawLinearGradient(SKCanvas canvas, VideoConfig config, SKColor startColor, SKColor endColor)
    {
        var colors = new SKColor[] { startColor, endColor };
        var shader = SKShader.CreateLinearGradient(
            new SKPoint(0, 0),
            new SKPoint(0, config.VideoHeight),
            colors);

        var paint = new SKPaint
        {
            Shader = shader,
            Style = SKPaintStyle.Fill
        };

        canvas.DrawRect(0, 0, config.VideoWidth, config.VideoHeight, paint);
    }

    private void DrawDecorations(SKCanvas canvas, VideoConfig config, int sceneIndex)
    {
        var paint = new SKPaint
        {
            Color = new SKColor(255, 255, 255, 20),
            Style = SKPaintStyle.Fill
        };

        // 添加圆形装饰
        var random = new Random(sceneIndex);
        for (int i = 0; i < 5; i++)
        {
            var x = random.Next(0, config.VideoWidth);
            var y = random.Next(0, config.VideoHeight);
            var radius = random.Next(20, 100);

            canvas.DrawCircle(x, y, radius, paint);
        }

        // 添加线条装饰
        paint.StrokeWidth = 2;
        paint.Style = SKPaintStyle.Stroke;
        for (int i = 0; i < 3; i++)
        {
            var x1 = random.Next(0, config.VideoWidth);
            var y1 = random.Next(0, config.VideoHeight);
            var x2 = random.Next(0, config.VideoWidth);
            var y2 = random.Next(0, config.VideoHeight);

            canvas.DrawLine(x1, y1, x2, y2, paint);
        }
    }

    private void DrawTextBackground(SKCanvas canvas, Rectangle rect)
    {
        var paint = new SKPaint
        {
            Color = new SKColor(0, 0, 0, 180), // 半透明黑色背景
            Style = SKPaintStyle.Fill
        };

        // 绘制圆角矩形背景
        var radius = 20;
        canvas.DrawRoundRect(rect.X, rect.Y, rect.Width, rect.Height, radius, radius, paint);

        // 绘制边框
        paint.Color = new SKColor(255, 255, 255, 100);
        paint.Style = SKPaintStyle.Stroke;
        paint.StrokeWidth = 2;
        canvas.DrawRoundRect(rect.X, rect.Y, rect.Width, rect.Height, radius, radius, paint);
    }

    private void DrawText(SKCanvas canvas, string text, SKPoint position)
    {
        var paint = new SKPaint
        {
            Color = SKColors.White,
            TextSize = 36,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Microsoft YaHei", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
        };

        // 添加文字阴影
        var shadowPaint = new SKPaint
        {
            Color = new SKColor(0, 0, 0, 150),
            TextSize = 36,
            IsAntialias = true,
            Typeface = paint.Typeface
        };

        canvas.DrawText(text, position.X + 2, position.Y + 2, shadowPaint);
        canvas.DrawText(text, position, paint);
    }

    private void DrawAnimationIndicator(SKCanvas canvas, string animationStyle, int sceneIndex)
    {
        var paint = new SKPaint
        {
            Color = new SKColor(255, 215, 0, 200), // 金色
            TextSize = 24,
            IsAntialias = true
        };

        var indicatorText = GetAnimationIndicatorText(animationStyle, sceneIndex);
        canvas.DrawText(indicatorText, new SKPoint(20, 50), paint);
    }

    private string GetAnimationIndicatorText(string animationStyle, int sceneIndex)
    {
        return animationStyle switch
        {
            "fade" => $"淡入 {sceneIndex + 1}",
            "slide" => $"滑动 {sceneIndex + 1}",
            "zoom" => $"缩放 {sceneIndex + 1}",
            "typewriter" => $"打字机 {sceneIndex + 1}",
            _ => $"场景 {sceneIndex + 1}"
        };
    }

    private List<string> SplitTextIntoSegments(string text, int maxLength)
    {
        var segments = new List<string>();
        var sentences = Regex.Split(text, @"[。！？]");

        var currentSegment = "";
        foreach (var sentence in sentences)
        {
            if (string.IsNullOrWhiteSpace(sentence)) continue;

            if (currentSegment.Length + sentence.Length > maxLength && !string.IsNullOrEmpty(currentSegment))
            {
                segments.Add(currentSegment.Trim());
                currentSegment = sentence;
            }
            else
            {
                currentSegment += sentence + "。";
            }
        }

        if (!string.IsNullOrEmpty(currentSegment))
        {
            segments.Add(currentSegment.Trim());
        }

        return segments;
    }

    private string GetBackgroundPath(int sceneIndex)
    {
        var backgrounds = new[]
        {
            "backgrounds/urban_night.jpg",
            "backgrounds/fantasy_world.jpg",
            "backgrounds/ancient_china.jpg",
            "backgrounds/modern_office.jpg",
            "backgrounds/school_campus.jpg",
            "backgrounds/battle_arena.jpg"
        };

        var index = sceneIndex % backgrounds.Length;
        return backgrounds[index];
    }

    private Rectangle GetRectangle(int x, int y, int width, int height)
    {
        return new Rectangle(x, y, width, height);
    }
}