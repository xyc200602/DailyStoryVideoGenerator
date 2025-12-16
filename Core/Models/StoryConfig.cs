using System.ComponentModel.DataAnnotations;

namespace Core.Models;

public class StoryConfig
{
    public string? CustomPrompt { get; set; }
    public bool UseCustomPrompt { get; set; }
    public string StoryType { get; set; } = "爽文"; // 爽文、玄幻、都市、修仙等
    public int WordCount { get; set; } = 2000; // 目标字数
    public string Style { get; set; } = "热血沸腾"; // 文风风格
    public List<string> Keywords { get; set; } = new(); // 关键词
    public string ProtagonistName { get; set; } = "叶凡"; // 主角名字
    public string Setting { get; set; } = "现代都市"; // 背景设定
}

public class GeneratedStory
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
    public string[] Paragraphs => Content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
    public int EstimatedReadingTime => (int)Math.Ceiling(Content.Length / 300.0); // 假设每分钟300字
}

public class VideoConfig
{
    public string BackgroundMusic { get; set; } = string.Empty;
    public string VoiceType { get; set; } = "xiaoxiao"; // 语音类型
    public float VoiceSpeed { get; set; } = 1.0f;
    public string AnimationStyle { get; set; } = "dynamic"; // 动画风格
    public int VideoWidth { get; set; } = 1920;
    public int VideoHeight { get; set; } = 1080;
    public int FramesPerSecond { get; set; } = 30;
}

public class UploadConfig
{
    public string BilibiliCookie { get; set; } = string.Empty;
    public string BilibiliCsrf { get; set; } = string.Empty;
    public string DefaultTitle { get; set; } = "今日爽文推荐";
    public string DefaultDescription { get; set; } = "每日更新精彩爽文，配有配音动画";
    public List<string> Tags { get; set; } = new() { "爽文", "小说", "配音", "动画" };
    public string Category { get; set; } = "文学";
    public bool Public { get; set; } = true;
}

public class AppConfig
{
    public StoryConfig StoryConfig { get; set; } = new();
    public VideoConfig VideoConfig { get; set; } = new();
    public UploadConfig UploadConfig { get; set; } = new();
    public string OpenAIApiKey { get; set; } = string.Empty;
    public string OpenAIEndpoint { get; set; } = "https://api.openai.com/";
    public string AzureSpeechKey { get; set; } = string.Empty;
    public string AzureSpeechRegion { get; set; } = "eastasia";
    public string OutputPath { get; set; } = "output";
    public bool EnableAutoUpload { get; set; } = true;
    public TimeSpan ScheduleTime { get; set; } = new TimeSpan(9, 0, 0); // 每天9点生成
}