using Core.Interfaces;
using Core.Models;
using FFMpegCore;
using FFMpegCore.Pipes;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Services;

public class VideoGenerator : IVideoGenerator
{
    private readonly ILogger<VideoGenerator> _logger;
    private readonly ITextToSpeech _textToSpeech;
    private readonly IAnimationGenerator _animationGenerator;

    public VideoGenerator(
        ILogger<VideoGenerator> logger,
        ITextToSpeech textToSpeech,
        IAnimationGenerator animationGenerator)
    {
        _logger = logger;
        _textToSpeech = textToSpeech;
        _animationGenerator = animationGenerator;
    }

    public async Task<string> CreateVideoAsync(GeneratedStory story, VideoConfig config, string outputPath)
    {
        try
        {
            Directory.CreateDirectory(outputPath);

            // 1. 生成语音
            _logger.LogInformation("开始生成语音...");
            var audioData = await _textToSpeech.SynthesizeAsync(
                story.Content,
                config.VoiceType,
                config.VoiceSpeed);

            var audioPath = Path.Combine(outputPath, $"{story.Id}_audio.wav");
            await File.WriteAllBytesAsync(audioPath, audioData);

            // 2. 获取音频时长
            var audioDuration = await _textToSpeech.GetAudioDurationAsync(audioData);
            _logger.LogInformation("音频时长: {Duration} 秒", audioDuration.TotalSeconds);

            // 3. 生成动画场景
            _logger.LogInformation("开始生成动画场景...");
            var sceneImages = await _animationGenerator.GenerateSceneAnimationsAsync(
                story.Paragraphs,
                config,
                Path.Combine(outputPath, "scenes"));

            // 4. 创建场景配置
            var scenes = await CreateSceneConfigAsync(story.Paragraphs, audioDuration, sceneImages);
            var sceneConfigPath = Path.Combine(outputPath, $"{story.Id}_scenes.json");
            await File.WriteAllTextAsync(sceneConfigPath, JsonSerializer.Serialize(scenes, new JsonSerializerOptions { WriteIndented = true }));

            // 5. 合成视频
            _logger.LogInformation("开始合成视频...");
            var videoPath = await ComposeVideoAsync(scenes, audioPath, config, outputPath);

            // 6. 添加字幕
            _logger.LogInformation("开始添加字幕...");
            var finalVideoPath = await AddSubtitlesAsync(
                videoPath,
                story.Paragraphs,
                scenes.Select(s => s.StartTime).ToArray(),
                outputPath);

            _logger.LogInformation("视频生成完成: {Path}", finalVideoPath);
            return finalVideoPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建视频失败");
            throw;
        }
    }

    public async Task<string> CombineVideoAndAudioAsync(string videoPath, byte[] audioData, string outputPath)
    {
        try
        {
            var audioPath = Path.Combine(outputPath, "temp_audio.wav");
            await File.WriteAllBytesAsync(audioPath, audioData);

            var outputVideoPath = Path.Combine(outputPath, $"final_{Guid.NewGuid()}.mp4");

            await FFMpegArguments
                .FromFileInput(videoPath)
                .AddFileInput(audioPath)
                .OutputToFile(outputVideoPath, true, options => options
                    .CopyChannel()
                    .WithAudioCodec("aac")
                    .WithAudioBitrate(192000))
                .ProcessAsynchronously();

            File.Delete(audioPath);
            return outputVideoPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "合并视频和音频失败");
            throw;
        }
    }

    public async Task<string> AddSubtitlesAsync(string videoPath, string[] subtitles, TimeSpan[] timings, string outputPath)
    {
        try
        {
            // 创建SRT字幕文件
            var srtPath = await CreateSRTFileAsync(subtitles, timings, outputPath);

            var outputVideoPath = Path.Combine(outputPath, $"with_subtitles_{Guid.NewGuid()}.mp4");

            // 使用FFmpeg添加字幕
            await FFMpegArguments
                .FromFileInput(videoPath)
                .OutputToFile(outputVideoPath, true, options => options
                    .CopyChannel()
                    .WithCustomArgument($"-vf \"subtitles='{srtPath}'\""))
                .ProcessAsynchronously();

            // 删除临时文件
            File.Delete(srtPath);

            return outputVideoPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加字幕失败");
            return videoPath; // 如果添加失败，返回原视频
        }
    }

    private async Task<List<VideoSegment>> CreateSceneConfigAsync(
        string[] paragraphs,
        TimeSpan totalDuration,
        List<string> sceneImages)
    {
        var scenes = new List<VideoSegment>();
        var timePerScene = totalDuration.TotalMilliseconds / paragraphs.Length;

        for (int i = 0; i < Math.Min(paragraphs.Length, sceneImages.Count); i++)
        {
            var startTime = TimeSpan.FromMilliseconds(i * timePerScene);
            var endTime = TimeSpan.FromMilliseconds((i + 1) * timePerScene);

            scenes.Add(new VideoSegment
            {
                ImagePath = sceneImages[i],
                StartTime = startTime,
                EndTime = endTime,
                Subtitle = paragraphs[i].Trim()
            });
        }

        return scenes;
    }

    private async Task<string> ComposeVideoAsync(
        List<VideoSegment> scenes,
        string audioPath,
        VideoConfig config,
        string outputPath)
    {
        var outputVideoPath = Path.Combine(outputPath, $"composed_{Guid.NewGuid()}.mp4");

        // 创建FFmpeg concat文件
        var concatListPath = Path.Combine(outputPath, "concat_list.txt");
        var concatContent = new StringBuilder();

        foreach (var scene in scenes)
        {
            // 计算每张图片的显示时长
            var duration = scene.EndTime - scene.StartTime;
            concatContent.AppendLine($"file '{scene.ImagePath}'");
            concatContent.AppendLine($"duration {duration.TotalSeconds}");
        }

        await File.WriteAllTextAsync(concatListPath, concatContent.ToString());

        // 使用FFmpeg合成视频
        await FFMpegArguments
            .FromFileInput(concatListPath, verifyExists: true, options => options
                .WithCustomArgument("-f concat -safe 0"))
            .AddFileInput(audioPath)
            .OutputToFile(outputVideoPath, true, options => options
                .WithVideoCodec("libx264")
                .WithVideoBitrate(2000)
                .WithAudioCodec("aac")
                .WithAudioBitrate(192000)
                .WithConstantRateFactor(23)
                .WithSpeedPreset(FFMpegCore.Enums.Speed.Fast)
                .WithFramerate(config.FramesPerSecond)
                .Size(config.VideoWidth, config.VideoHeight))
            .ProcessAsynchronously();

        // 清理临时文件
        File.Delete(concatListPath);

        return outputVideoPath;
    }

    private async Task<string> CreateSRTFileAsync(string[] subtitles, TimeSpan[] timings, string outputPath)
    {
        var srtPath = Path.Combine(outputPath, "subtitles.srt");
        var srtContent = new StringBuilder();

        for (int i = 0; i < subtitles.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(subtitles[i])) continue;

            var startTime = i < timings.Length ? timings[i] : TimeSpan.FromSeconds(i * 5);
            var endTime = i + 1 < timings.Length ? timings[i + 1] : startTime.Add(TimeSpan.FromSeconds(5));

            srtContent.AppendLine(i.ToString());
            srtContent.AppendLine($"{FormatTime(startTime)} --> {FormatTime(endTime)}");
            srtContent.AppendLine(subtitles[i].Trim());
            srtContent.AppendLine();
        }

        await File.WriteAllTextAsync(srtPath, srtContent.ToString(), Encoding.UTF8);
        return srtPath;
    }

    private string FormatTime(TimeSpan time)
    {
        var hours = (int)time.TotalHours;
        var minutes = time.Minutes;
        var seconds = time.Seconds;
        var milliseconds = time.Milliseconds;

        return $"{hours:D2}:{minutes:D2}:{seconds:D2},{milliseconds:D3}";
    }

    private async Task<string> CreateBackgroundMusicVideoAsync(
        string videoPath,
        string musicPath,
        float musicVolume,
        string outputPath)
    {
        try
        {
            if (!File.Exists(musicPath))
            {
                return videoPath;
            }

            var outputVideoPath = Path.Combine(outputPath, $"with_music_{Guid.NewGuid()}.mp4");

            // 混合音频
            await FFMpegArguments
                .FromFileInput(videoPath)
                .AddFileInput(musicPath)
                .OutputToFile(outputVideoPath, true, options => options
                    .CopyChannel()
                    .WithCustomArgument($"-filter_complex \"[1:a]volume={musicVolume}[bgm];[0:a][bgm]amix\"")
                    .WithAudioCodec("aac"))
                .ProcessAsynchronously();

            return outputVideoPath;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "添加背景音乐失败");
            return videoPath;
        }
    }

    private async Task<string> ApplyVideoTransitionAsync(
        string video1Path,
        string video2Path,
        string transitionType,
        string outputPath)
    {
        var outputVideoPath = Path.Combine(outputPath, $"transition_{Guid.NewGuid()}.mp4");

        var transitionCommand = transitionType switch
        {
            "fade" => "fade=in:0:30",
            "slide" => "hflip",
            "zoom" => "zoompan=z='if(lte(zoom,1.0),1.5,max(1.001,zoom-0.0015))':d=1",
            _ => ""
        };

        await FFMpegArguments
            .FromFileInput(video1Path)
            .OutputToFile(outputVideoPath, true, options => options
                .WithCustomArgument($"-vf {transitionCommand}"))
            .ProcessAsynchronously();

        return outputVideoPath;
    }

    private async Task<string> OptimizeVideoAsync(string videoPath, string outputPath)
    {
        var optimizedPath = Path.Combine(outputPath, $"optimized_{Path.GetFileName(videoPath)}");

        await FFMpegArguments
            .FromFileInput(videoPath)
            .OutputToFile(optimizedPath, true, options => options
                .WithVideoCodec("libx264")
                .WithAudioCodec("aac")
                .WithConstantRateFactor(28) // 更好的压缩
                .WithSpeedPreset(FFMpegCore.Enums.Speed.Medium))
            .ProcessAsynchronously();

        return optimizedPath;
    }
}