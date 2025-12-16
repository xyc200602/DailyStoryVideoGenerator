using Core.Interfaces;
using Core.Models;
using Microsoft.Extensions.Logging;

namespace DailyStoryVideoGenerator.Services;

public class VideoGenerationService
{
    private readonly IVideoGenerator _videoGenerator;
    private readonly ILogger<VideoGenerationService> _logger;

    public VideoGenerationService(
        IVideoGenerator videoGenerator,
        ILogger<VideoGenerationService> logger)
    {
        _videoGenerator = videoGenerator;
        _logger = logger;
    }

    public async Task<string> GenerateVideoAsync(
        GeneratedStory story,
        VideoConfig config,
        Action<int, string> onProgress)
    {
        try
        {
            var outputPath = Path.Combine("output", story.Id);
            Directory.CreateDirectory(outputPath);

            onProgress(20, "开始生成视频...");

            var videoPath = await _videoGenerator.CreateVideoAsync(story, config, outputPath);

            onProgress(90, "视频生成完成...");

            var finalPath = Path.Combine("output", $"final_{story.Id}.mp4");
            if (File.Exists(videoPath))
            {
                File.Move(videoPath, finalPath, true);
            }

            onProgress(100, "全部完成！");
            return finalPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "视频生成失败");
            throw;
        }
    }
}