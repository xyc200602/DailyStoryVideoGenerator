using Core.Models;

namespace Core.Interfaces;

public interface IStoryGenerator
{
    Task<GeneratedStory> GenerateStoryAsync(StoryConfig config);
    Task<GeneratedStory> GenerateStoryWithDefaultAsync();
}