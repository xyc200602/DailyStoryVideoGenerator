using Core.Models;

namespace Core.Interfaces;

public interface IBilibiliUploader
{
    Task<UploadResult> UploadVideoAsync(string videoPath, GeneratedStory story, UploadConfig config);
    Task<bool> LoginAsync(string cookie, string csrf);
    Task<UploadStatus> GetUploadStatusAsync(string uploadId);
}

public class UploadResult
{
    public bool Success { get; set; }
    public string VideoId { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int Views { get; set; }
    public int Likes { get; set; }
}

public class UploadStatus
{
    public int Percent { get; set; }
    public string State { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}