using Core.Models;

namespace Core.Interfaces;

public interface ITextToSpeech
{
    Task<byte[]> SynthesizeAsync(string text, string voiceType, float speed = 1.0f);
    Task<TimeSpan> GetAudioDurationAsync(byte[] audioData);
}