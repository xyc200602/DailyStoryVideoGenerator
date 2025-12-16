using Core.Models;

namespace Core.Interfaces;

public interface IVideoGenerator
{
    Task<string> CreateVideoAsync(GeneratedStory story, VideoConfig config, string outputPath);
    Task<string> CombineVideoAndAudioAsync(string videoPath, byte[] audioData, string outputPath);
    Task<string> AddSubtitlesAsync(string videoPath, string[] subtitles, TimeSpan[] timings, string outputPath);
}

public class VideoSegment
{
    public string ImagePath { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string? Subtitle { get; set; }
}