using Azure.CognitiveServices.Speech;
using Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IO;

namespace Services;

public class AzureTextToSpeech : ITextToSpeech
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzureTextToSpeech> _logger;
    private readonly string _speechKey;
    private readonly string _speechRegion;

    private readonly Dictionary<string, string> _voiceMap = new()
    {
        { "xiaoxiao", "zh-CN-XiaoxiaoNeural" },  // 女声，温柔
        { "xiaoyan", "zh-CN-XiaoyanNeural" },    // 女声，成熟
        { "yunjian", "zh-CN-YunjianNeural" },    // 男声，成熟
        { "yunxi", "zh-CN-YunxiNeural" },        // 男声，年轻
        { "xiaochen", "zh-CN-XiaochenNeural" },  // 女声，活泼
        { "xiaohan", "zh-CN-XiaohanNeural" },    // 女声，甜美
        { "xiaomeng", "zh-CN-XiaomengNeural" },  // 女声，可爱
        { "xiaomo", "zh-CN-XiaomoNeural" },      // 男声，温和
        { "xiaoxuan", "zh-CN-XiaoxuanNeural" },  // 女声，优雅
        { "xiaoyou", "zh-CN-XiaoyouNeural" }     // 男声，磁性
    };

    public AzureTextToSpeech(IConfiguration configuration, ILogger<AzureTextToSpeech> logger)
    {
        _configuration = configuration;
        _logger = logger;

        _speechKey = _configuration["AppConfig:AzureSpeechKey"];
        _speechRegion = _configuration["AppConfig:AzureSpeechRegion"];

        if (string.IsNullOrEmpty(_speechKey) || string.IsNullOrEmpty(_speechRegion))
        {
            throw new ArgumentException("Azure Speech service is not properly configured");
        }
    }

    public async Task<byte[]> SynthesizeAsync(string text, string voiceType, float speed = 1.0f)
    {
        try
        {
            var speechConfig = SpeechConfig.FromSubscription(_speechKey, _speechRegion);
            speechConfig.SpeechSynthesisVoiceName = _voiceMap.GetValueOrDefault(voiceType, "zh-CN-XiaoxiaoNeural");
            speechConfig.SpeechSynthesisOutputFormat = SpeechSynthesisOutputFormat.Audio24Khz96KBitRateMonoMp3;

            // 设置语音速度
            var synthesisOptions = new SpeechSynthesisOptions
            {
                SpeakingRate = speed
            };

            using var synthesizer = new SpeechSynthesizer(speechConfig, null);
            using var result = await synthesizer.SpeakTextAsync(text, synthesisOptions);

            if (result.Reason == ResultReason.SynthesizingAudioCompleted)
            {
                using var stream = AudioDataStream.FromResult(result);
                using var memoryStream = new MemoryStream();

                await stream.SaveToWaveStreamAsync(memoryStream);
                return memoryStream.ToArray();
            }
            else if (result.Reason == ResultReason.Canceled)
            {
                var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                throw new InvalidOperationException($"Speech synthesis canceled: {cancellation.Reason}, {cancellation.ErrorDetails}");
            }

            throw new InvalidOperationException("Failed to synthesize speech");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to synthesize speech for text: {Text}", text.Substring(0, Math.Min(100, text.Length)));
            throw;
        }
    }

    public async Task<TimeSpan> GetAudioDurationAsync(byte[] audioData)
    {
        try
        {
            using var stream = new MemoryStream(audioData);
            using var audioConfig = AudioConfig.FromWavFileInput(stream);

            // 使用Windows Media Foundation来获取音频时长
            // 这里简化处理，实际应该解析音频头部获取精确时长
            // 假设语速为每分钟200字
            var estimatedDuration = TimeSpan.FromSeconds(audioData.Length / 16000.0 * 0.5); // 粗略估算
            return estimatedDuration;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get audio duration");
            return TimeSpan.Zero;
        }
    }

    private string CleanTextForSpeech(string text)
    {
        // 移除或替换不适合朗读的字符
        var cleanText = text
            .Replace("「", "\"")
            .Replace("」", "\"")
            .Replace『", "\"")
            .Replace』", "\"")
            .Replace("《", "《")
            .Replace("》", "》")
            .Replace("...", "。")
            .Replace("——", "，");

        // 处理数字
        cleanText = System.Text.RegularExpressions.Regex.Replace(cleanText, @"\d+", match =>
        {
            return NumberToChineseWords(match.Value);
        });

        return cleanText;
    }

    private string NumberToChineseWords(string number)
    {
        try
        {
            var num = int.Parse(number);
            if (num >= 10000)
            {
                return $"{num / 10000}万{num % 10000}";
            }
            else if (num >= 1000)
            {
                return $"{num / 1000}千{num % 1000}";
            }
            else if (num >= 100)
            {
                return $"{num / 100}百{num % 100}";
            }
            return number;
        }
        catch
        {
            return number;
        }
    }

    private async Task<byte[]> SynthesizeWithBreaksAsync(string text, string voiceType, float speed = 1.0f)
    {
        // 将长文本分段，添加适当的停顿
        var paragraphs = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var allAudio = new List<byte[]>();

        foreach (var paragraph in paragraphs)
        {
            if (!string.IsNullOrWhiteSpace(paragraph))
            {
                var cleanText = CleanTextForSpeech(paragraph.Trim());
                if (!string.IsNullOrEmpty(cleanText))
                {
                    var audio = await SynthesizeAsync(cleanText, voiceType, speed);
                    allAudio.Add(audio);

                    // 添加停顿
                    if (paragraph != paragraphs.Last())
                    {
                        var silence = await GenerateSilenceAsync(1000); // 1秒停顿
                        allAudio.Add(silence);
                    }
                }
            }
        }

        // 合并所有音频
        return CombineAudioFiles(allAudio);
    }

    private async Task<byte[]> GenerateSilenceAsync(int durationMs)
    {
        // 生成静音音频
        var sampleRate = 24000;
        var channels = 1;
        var bytesPerSample = 2;
        var totalBytes = sampleRate * channels * bytesPerSample * durationMs / 1000;

        var silence = new byte[totalBytes];
        await Task.CompletedTask;
        return silence;
    }

    private byte[] CombineAudioFiles(List<byte[]> audioFiles)
    {
        using var combinedStream = new MemoryStream();

        foreach (var audio in audioFiles)
        {
            combinedStream.Write(audio, 44, audio.Length - 44); // 跳过WAV头部
        }

        // 添加WAV头部
        var header = CreateWavHeader((int)combinedStream.Length, 24000, 2, 1);
        var finalStream = new MemoryStream();
        finalStream.Write(header, 0, header.Length);
        finalStream.Write(combinedStream.ToArray(), 0, (int)combinedStream.Length);

        return finalStream.ToArray();
    }

    private byte[] CreateWavHeader(int dataLength, int sampleRate, short bitsPerSample, short channels)
    {
        var header = new byte[44];

        // RIFF header
        System.Text.Encoding.ASCII.GetBytes("RIFF").CopyTo(header, 0);
        BitConverter.GetBytes(dataLength + 36).CopyTo(header, 4);
        System.Text.Encoding.ASCII.GetBytes("WAVE").CopyTo(header, 8);

        // fmt chunk
        System.Text.Encoding.ASCII.GetBytes("fmt ").CopyTo(header, 12);
        BitConverter.GetBytes(16).CopyTo(header, 16); // chunk size
        BitConverter.GetBytes((short)1).CopyTo(header, 20); // audio format (PCM)
        BitConverter.GetBytes(channels).CopyTo(header, 22);
        BitConverter.GetBytes(sampleRate).CopyTo(header, 24);
        BitConverter.GetBytes(sampleRate * channels * bitsPerSample / 8).CopyTo(header, 28);
        BitConverter.GetBytes((short)(channels * bitsPerSample / 8)).CopyTo(header, 32);
        BitConverter.GetBytes(bitsPerSample).CopyTo(header, 34);

        // data chunk
        System.Text.Encoding.ASCII.GetBytes("data").CopyTo(header, 36);
        BitConverter.GetBytes(dataLength).CopyTo(header, 40);

        return header;
    }
}