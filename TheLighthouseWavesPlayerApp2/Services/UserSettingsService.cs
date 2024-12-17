using TheLighthouseWavesPlayerApp2.Database;
using TheLighthouseWavesPlayerApp2.Models;
using TheLighthouseWavesPlayerApp2.Services.Interfaces;

namespace TheLighthouseWavesPlayerApp2.Services;

public class UserSettingsService : IUserSettingsService
{
    private readonly DatabaseService _database;

    public UserSettingsService(DatabaseService database)
    {
        _database = database;
    }

    public async Task<UserSettings> GetUserSettingsAsync()
    {
        return await _database.GetUserSettingsAsync();
    }

    public async Task<int> SaveUserSettingsAsync(UserSettings settings)
    {
        return await _database.SaveUserSettingsAsync(settings);
    }
}