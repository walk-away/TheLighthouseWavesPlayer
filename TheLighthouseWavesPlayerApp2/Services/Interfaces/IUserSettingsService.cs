using TheLighthouseWavesPlayerApp2.Models;

namespace TheLighthouseWavesPlayerApp2.Services.Interfaces;

public interface IUserSettingsService
{
    Task<UserSettings> GetUserSettingsAsync();
    Task<int> SaveUserSettingsAsync(UserSettings settings);
}