using Azure;
using Azure.AI.OpenAI;
using Core.Interfaces;
using Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Services;

public class StoryGenerator : IStoryGenerator
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<StoryGenerator> _logger;
    private readonly OpenAIClient _openAIClient;
    private readonly string _deploymentName;

    private readonly Dictionary<string, string> _storyPrompts = new()
    {
        { "爽文", @"写一篇{0}字的爽文小说，风格要求{1}。
背景设定：{2}
主角：{3}
关键词：{4}

要求：
1. 情节曲折刺激，主角一路逆袭
2. 打脸情节要痛快淋漓
3. 语言要热血沸腾，节奏要快
4. 要有明确的升级感和爽快感
5. 适当加入一些现代元素，让读者有代入感" },

        { "玄幻", @"写一篇{0}字的玄幻小说，风格要求{1}。
背景设定：{2}
主角：{3}
关键词：{4}

要求：
1. 包含修炼体系、法宝、灵兽等玄幻元素
2. 战斗场面要宏大壮观
3. 要有神秘感和探索感
4. 设定要新颖有趣
5. 节奏紧凑，高潮迭起" },

        { "都市", @"写一篇{0}字的都市小说，风格要求{1}。
背景设定：{2}
主角：{3}
关键词：{4}

要求：
1. 贴近现实，但又要有戏剧性
2. 主角要有特殊能力或奇遇
3. 反映都市生活的方方面面
4. 有商战、情感等元素
5. 结局要大快人心" },

        { "修仙", @"写一篇{0}字的修仙小说，风格要求{1}。
背景设定：{2}
主角：{3}
关键词：{4}

要求：
1. 有完整的修仙等级体系
2. 包含炼丹、炼器、阵法等元素
3. 渡劫飞升要写得震撼
4. 师徒情谊、道侣情感要真挚
5. 仙界战斗场面要宏大" }
    };

    public StoryGenerator(IConfiguration configuration, ILogger<StoryGenerator> logger)
    {
        _configuration = configuration;
        _logger = logger;

        var apiKey = _configuration["AppConfig:OpenAIApiKey"];
        var endpoint = _configuration["AppConfig:OpenAIEndpoint"];
        _deploymentName = _configuration["AppConfig:OpenAIDeploymentName"] ?? "gpt-3.5-turbo";

        if (string.IsNullOrEmpty(apiKey))
        {
            throw new ArgumentException("OpenAI API key is not configured");
        }

        var options = new OpenAIClientOptions
        {
            Endpoint = new Uri(endpoint)
        };

        _openAIClient = new OpenAIClient(apiKey, options);
    }

    public async Task<GeneratedStory> GenerateStoryAsync(StoryConfig config)
    {
        try
        {
            var prompt = BuildPrompt(config);

            var chatCompletionsOptions = new ChatCompletionsOptions
            {
                DeploymentName = _deploymentName,
                Messages =
                {
                    new ChatMessage(ChatRole.System, "你是一个专业的网络小说作家，擅长写作各种类型的爽文小说。"),
                    new ChatMessage(ChatRole.User, prompt)
                },
                Temperature = 0.9f,
                MaxTokens = 3000,
                FrequencyPenalty = 0.5f,
                PresencePenalty = 0.5f
            };

            var response = await _openAIClient.GetChatCompletionsAsync(chatCompletionsOptions);
            var content = response.Value.Choices[0].Message.Content;

            if (string.IsNullOrEmpty(content))
            {
                throw new InvalidOperationException("AI returned empty content");
            }

            // 提取标题（通常在第一行）
            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var title = lines.Length > 0 ? lines[0].Replace("标题：", "").Replace("【", "").Replace("】", "").Trim() : GenerateRandomTitle(config);

            return new GeneratedStory
            {
                Title = title,
                Content = content,
                GeneratedAt = DateTime.Now
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate story");
            throw;
        }
    }

    public async Task<GeneratedStory> GenerateStoryWithDefaultAsync()
    {
        var defaultConfig = new StoryConfig
        {
            UseCustomPrompt = false,
            StoryType = "爽文",
            WordCount = 2000,
            Style = "热血沸腾",
            ProtagonistName = "叶凡",
            Setting = "现代都市",
            Keywords = new List<string> { "逆袭", "打脸", "系统", "美女总裁", "神豪" }
        };

        return await GenerateStoryAsync(defaultConfig);
    }

    private string BuildPrompt(StoryConfig config)
    {
        if (config.UseCustomPrompt && !string.IsNullOrEmpty(config.CustomPrompt))
        {
            return config.CustomPrompt;
        }

        var keywords = string.Join("、", config.Keywords);
        var storyType = _storyPrompts.ContainsKey(config.StoryType) ? config.StoryType : "爽文";
        var template = _storyPrompts[storyType];

        return string.Format(template, config.WordCount, config.Style, config.Setting, config.ProtagonistName, keywords);
    }

    private string GenerateRandomTitle(StoryConfig config)
    {
        var titles = new List<string>
        {
            $"《{config.ProtagonistName}的逆袭之路》",
            $"《都市最强{config.ProtagonistName}》",
            $"《重生之我是{config.ProtagonistName}》",
            $"《{config.ProtagonistName}：从零开始》",
            $"《{config.ProtagonistName}的传奇一生》",
            $"《{config.ProtagonistName}归来》",
            $"《{config.ProtagonistName}超神了》",
            $"《{config.ProtagonistName}的秘密》"
        };

        var random = new Random();
        return titles[random.Next(titles.Count)];
    }
}