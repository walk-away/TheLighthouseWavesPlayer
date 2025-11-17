using TheLighthouseWavesPlayerVideoApp.Models;

namespace TheLighthouseWavesPlayerVideoApp.Interfaces;

public interface ISubtitleService
{
    Task<List<SubtitleItem>> LoadSubtitlesAsync(string filePath);
}
