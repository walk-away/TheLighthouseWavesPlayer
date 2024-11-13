using SQLite;
using TheLighthouseWavesPlayer.Core.Models;

namespace TheLighthouseWavesPlayerApp.Data;

public class DatabaseProvider
{
    private static string _dbPath = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TheLighthouseWavesPlayerApp"), "TheLighthouseWavesPlayerAppDB.db");

    private DatabaseProvider()
    {
    }

    public static void Initialize()
    {
        Database.CreateTable<Rating>();
        Database.CreateTable<Favorite>();
        Database.CreateTable<Playlist>();
        Database.CreateTable<Album>();
        Database.CreateTable<AlbumMetadata>();
        Database.CreateTable<Artist>();
        Database.CreateTable<PlaybackHistory>();
        Database.CreateTable<Track>();
        Database.CreateTable<TrackMetadata>();
        Database.CreateTable<AudioCacheMetadata>();
        Database.CreateTable<ScanAudioFolder>();
        Database.CreateTable<Genre>();
    }

    private static SQLiteConnection? _database;
    public static SQLiteConnection Database
    {
        get
        {
            if (_database == null)
            {
                if (string.IsNullOrEmpty(_dbPath))
                {
                    throw new Exception("Database path is not configured");
                }
                _database = new SQLiteConnection(_dbPath);
            }
            return _database;
        }
    }

    private static SQLiteAsyncConnection? _databaseAsync;
    public static SQLiteAsyncConnection DatabaseAsync
    {
        get
        {
            if (_databaseAsync == null)
            {
                if (string.IsNullOrEmpty(_dbPath))
                {
                    throw new Exception("Database path is not configured");
                }
                _databaseAsync = new SQLiteAsyncConnection(_dbPath);
            }
            return _databaseAsync;
        }
    }
}