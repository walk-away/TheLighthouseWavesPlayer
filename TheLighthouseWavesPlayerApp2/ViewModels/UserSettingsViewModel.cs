using System.Windows.Input;
using TheLighthouseWavesPlayerApp2.Models;
using TheLighthouseWavesPlayerApp2.Services.Interfaces;

namespace TheLighthouseWavesPlayerApp2.ViewModels;

public class UserSettingsViewModel : BaseViewModel
{
    private readonly IUserSettingsService _userSettingsService;

    private UserSettings _settings;
    public UserSettings Settings
    {
        get => _settings;
        set => SetProperty(ref _settings, value);
    }

    public UserSettingsViewModel(IUserSettingsService userSettingsService)
    {
        _userSettingsService = userSettingsService;
        LoadSettingsCommand = new Command(async () => await LoadSettings());
    }

    public ICommand LoadSettingsCommand { get; }

    private async Task LoadSettings()
    {
        Settings = await _userSettingsService.GetUserSettingsAsync();
    }

    public async Task SaveSettings()
    {
        await _userSettingsService.SaveUserSettingsAsync(Settings);
    }
}
