using NAudio.Wave;
using TheLighthouseWavesPlayer.Core.Interfaces.Explorer;
using TheLighthouseWavesPlayer.Core.Models;
using TheLighthouseWavesPlayer.Core.Models.Explorer;

namespace TheLighthouseWavesPlayer.Core.Services.Explorer;

public class TrackScanService : ITrackScanService
{
    public List<Track> SearchTracks(List<ScanAudioFolder> folders)
    {
        var result = new List<Track>();
        var processedFolders = new List<string>();

        foreach (var path in folders.Select(f => f.Path))
        {
            var folder = new FolderInfo(path);
            result.AddRange(ParseTemplate1(folder.Children, ref processedFolders));
            result.AddRange(ParseTemplate2(folder.Children, ref processedFolders));
            result.AddRange(ParseTemplate3(new List<FolderInfo> { folder }, ref processedFolders));
        }

        return result;
    }

    private List<Track> ParseTemplate1(List<FolderInfo> folders, ref List<string> processedFolders)
    {
        var result = new List<Track>();
        foreach (var folder in folders)
        {
            if (processedFolders.Contains(folder.Path))
            {
                continue;
            }

            var leaves = FolderInfo.GetLeaves(folder);
            foreach (var leave in leaves)
            {
                if (processedFolders.Contains(leave.Path))
                {
                    continue;
                }

                if (leave.IsNumeric)
                {
                    continue;
                }

                foreach (var file in leave.AudioFiles.OrderBy(a => a))
                {
                    var track = new Track
                    {
                        Title = Path.GetFileNameWithoutExtension(file),
                        Duration = GetAudioDuration(file),
                        AlbumId = Guid.Empty
                    };
                    result.Add(track);
                }

                processedFolders.Add(leave.Path);
            }
        }

        return result;
    }

    private List<Track> ParseTemplate2(List<FolderInfo> folders, ref List<string> processedFolders)
    {
        var result = new List<Track>();
        foreach (var folder in folders)
        {
            if (processedFolders.Contains(folder.Path))
            {
                continue;
            }

            var leaves = FolderInfo.GetLeaves(folder);
            if (leaves.Count == 0)
            {
                continue;
            }

            foreach (var leave in leaves.OrderBy(a => a.Name))
            {
                if (processedFolders.Contains(leave.Path))
                {
                    continue;
                }

                if (!leave.IsNumeric)
                {
                    continue;
                }

                foreach (var file in leave.AudioFiles.OrderBy(a => a))
                {
                    var track = new Track
                    {
                        Title = Path.GetFileNameWithoutExtension(file),
                        Duration = GetAudioDuration(file),
                        AlbumId = Guid.Empty
                    };
                    result.Add(track);
                }

                processedFolders.Add(leave.Path);
            }

            processedFolders.Add(folder.Path);
        }

        return result;
    }

    private List<Track> ParseTemplate3(List<FolderInfo> folders, ref List<string> processedFolders)
    {
        var result = new List<Track>();
        foreach (var folder in folders)
        {
            if (processedFolders.Contains(folder.Path))
            {
                continue;
            }

            if (!folder.IsNumeric)
            {
                foreach (var file in folder.AudioFiles)
                {
                    var track = new Track
                    {
                        Title = Path.GetFileNameWithoutExtension(file),
                        Duration = GetAudioDuration(file),
                        AlbumId = Guid.Empty
                    };
                    result.Add(track);
                }

                processedFolders.Add(folder.Path);
            }

            if (folder.HasChildren)
            {
                result.AddRange(ParseTemplate3(folder.Children, ref processedFolders));
            }
        }

        return result;
    }

    private TimeSpan GetAudioDuration(string filePath)
    {
        try
        {
            using (var reader = new Mp3FileReader(filePath))
            {
                return reader.TotalTime;
            }
        }
        catch (Exception ex)
        {
            return TimeSpan.Zero;
        }
    }
}