using SQLite;
using TheLighthouseWavesPlayerVideoApp.Models;

namespace TheLighthouseWavesPlayerVideoApp.Data
{
    public class DatabaseService : IDatabaseService
    {
        SQLiteAsyncConnection Database;

        public DatabaseService()
        {
        }

        async Task Init()
        {
            if (Database is not null)
                return;

            Database = new SQLiteAsyncConnection(Constants.Constants.DatabasePath, Constants.Constants.Flags);
            var result = await Database.CreateTableAsync<Video>();
        }

        public async Task<Video> GetByIdAsync(int id)
        {
            await Init();
            return await Database.Table<Video>().Where(i => i.Id == id).FirstOrDefaultAsync();
        }
        
        public async Task<Video> GetVideoByPathAsync(string path)
        {
            try
            {
                await Init();
                return await Database.Table<Video>()
                    .Where(v => v.FilePath == path)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                return null; // Return null on error to allow fallback
            }
        }

        public async Task<IEnumerable<Video>> GetVideosAsync()
        {
            await Init();
            return await Database.Table<Video>().OrderByDescending(v => v.DateAdded).ToListAsync();
        }

        public async Task<IEnumerable<Video>> GetFavoriteVideosAsync()
        {
            await Init();
            return await Database.Table<Video>().Where(v => v.IsFavorite).ToListAsync();
        }

        public async Task AddVideoAsync(Video video)
        {
            await Init();
            await Database.InsertAsync(video);
        }

        public async Task UpdateVideoAsync(Video video)
        {
            await Init();
            await Database.UpdateAsync(video);
        }

        public async Task DeleteVideoAsync(Video video)
        {
            await Init();
            await Database.DeleteAsync(video);
        }

        public async Task ToggleFavoriteAsync(int videoId)
        {
            await Init();
            var video = await GetByIdAsync(videoId);
            if (video != null)
            {
                video.IsFavorite = !video.IsFavorite;
                
                if (video.IsFavorite && video.Duration.TotalSeconds < 1)
                {
                    Console.WriteLine($"Warning: Adding video to favorites with no duration: {video.Title}");
                }
        
                await UpdateVideoAsync(video);
            }
        }
    }
}