using Core.Models;

namespace Core.Interfaces;

public interface IAnimationGenerator
{
    Task<string> GenerateAnimationAsync(GeneratedStory story, VideoConfig config, string outputPath);
    Task<List<string>> GenerateSceneAnimationsAsync(string[] paragraphs, VideoConfig config, string outputPath);
}

public class SceneAnimation
{
    public string Text { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public AnimationType AnimationType { get; set; }
}

public enum AnimationType
{
    FadeIn,
    SlideIn,
    ZoomIn,
    Typewriter,
    Dynamic
}