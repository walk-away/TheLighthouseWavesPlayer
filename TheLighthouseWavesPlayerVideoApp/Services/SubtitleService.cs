using System.Text.RegularExpressions;
using TheLighthouseWavesPlayerVideoApp.Interfaces;
using TheLighthouseWavesPlayerVideoApp.Models;

namespace TheLighthouseWavesPlayerVideoApp.Services;

public class SubtitleService : ISubtitleService
{
    public async Task<List<SubtitleItem>> LoadSubtitlesAsync(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return new List<SubtitleItem>();
        }

        if (!File.Exists(filePath))
        {
            return new List<SubtitleItem>();
        }

        try
        {
            var content = await File.ReadAllTextAsync(filePath);
            return ParseSrt(content);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading subtitles: {ex.Message}");
            return new List<SubtitleItem>();
        }
    }

    private List<SubtitleItem> ParseSrt(string content)
    {
        var result = new List<SubtitleItem>();
        var blocks = Regex.Split(content, @"\r\n\r\n|\n\n");

        foreach (var block in blocks)
        {
            if (string.IsNullOrWhiteSpace(block))
            {
                continue;
            }

            var lines = Regex.Split(block, @"\r\n|\n");
            if (lines.Length < 3)
            {
                continue;
            }

            if (!int.TryParse(lines[0], out int index))
            {
                continue;
            }

            var timecodeLine = lines[1];
            var timecodes = timecodeLine.Split(" --> ");
            if (timecodes.Length != 2)
            {
                continue;
            }

            if (!TryParseTimecode(timecodes[0], out TimeSpan startTime) ||
                !TryParseTimecode(timecodes[1], out TimeSpan endTime))
            {
                continue;
            }

            var text = string.Join(" ", lines.Skip(2));

            result.Add(new SubtitleItem
            {
                Index = index,
                StartTime = startTime,
                EndTime = endTime,
                Text = text
            });
        }

        return result;
    }

    private bool TryParseTimecode(string timecode, out TimeSpan result)
    {
        var match = Regex.Match(timecode.Trim(), @"(\d{2}):(\d{2}):(\d{2})[,\.](\d{3})");
        if (match.Success)
        {
            int hours = int.Parse(match.Groups[1].Value);
            int minutes = int.Parse(match.Groups[2].Value);
            int seconds = int.Parse(match.Groups[3].Value);
            int milliseconds = int.Parse(match.Groups[4].Value);

            result = new TimeSpan(0, hours, minutes, seconds, milliseconds);
            return true;
        }

        result = TimeSpan.Zero;
        return false;
    }
}
