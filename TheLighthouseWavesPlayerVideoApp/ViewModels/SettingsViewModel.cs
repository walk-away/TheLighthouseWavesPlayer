using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TheLighthouseWavesPlayerVideoApp.Interfaces;
using TheLighthouseWavesPlayerVideoApp.Localization.Interfaces;
using TheLighthouseWavesPlayerVideoApp.Models;

namespace TheLighthouseWavesPlayerVideoApp.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    private readonly ILocalizationManager _localizationManager;
    private readonly IThemeService _themeService;
    private readonly ILocalizedResourcesProvider _resourcesProvider;
    [ObservableProperty] private ObservableCollection<LanguageOption> _availableLanguages;
    [ObservableProperty] private LanguageOption _selectedLanguage;
    [ObservableProperty] private ObservableCollection<ThemeOption> _availableThemes;
    [ObservableProperty] private ThemeOption _selectedTheme;
    [ObservableProperty] private string _pageTitle;
    [ObservableProperty] private string _languageLabel;

    public SettingsViewModel(
        ILocalizationManager localizationManager,
        IThemeService themeService,
        ILocalizedResourcesProvider resourcesProvider)
    {
        _localizationManager = localizationManager;
        _themeService = themeService;
        _resourcesProvider = resourcesProvider;
        _pageTitle = resourcesProvider["Settings_PageTitle"];
        _languageLabel = resourcesProvider["Settings_LanguageLabel"];

        AvailableLanguages = new ObservableCollection<LanguageOption>
        {
            new LanguageOption { Name = "English", Culture = new CultureInfo("en-US") },
            new LanguageOption { Name = "Русский", Culture = new CultureInfo("ru-RU") }
        };

        var currentCulture = _localizationManager.GetUserCulture();
        SelectedLanguage = AvailableLanguages.FirstOrDefault(l =>
            l.Culture.Name == currentCulture.Name) ?? AvailableLanguages[0];

        InitializeThemeOptions();
        var currentTheme = _themeService.CurrentTheme;
        SelectedTheme = AvailableThemes.FirstOrDefault(t =>
            t.Theme == currentTheme) ?? AvailableThemes[0];
    }

    private void InitializeThemeOptions()
    {
        AvailableThemes = new ObservableCollection<ThemeOption>
        {
            new ThemeOption { Name = _resourcesProvider["Settings_ThemeLight"], Theme = AppTheme.Light },
            new ThemeOption { Name = _resourcesProvider["Settings_ThemeDark"], Theme = AppTheme.Dark },
            new ThemeOption { Name = _resourcesProvider["Settings_ThemeSystem"], Theme = AppTheme.Unspecified }
        };
    }

    private void UpdateThemeOptionsLabels()
    {
        var themes = new ObservableCollection<ThemeOption>
        {
            new() { Name = _resourcesProvider["Settings_ThemeLight"], Theme = AppTheme.Light },
            new() { Name = _resourcesProvider["Settings_ThemeDark"], Theme = AppTheme.Dark },
            new() { Name = _resourcesProvider["Settings_ThemeSystem"], Theme = AppTheme.Unspecified }
        };

        var selectedTheme = SelectedTheme?.Theme ?? AppTheme.Unspecified;
        AvailableThemes = themes;
        SelectedTheme = AvailableThemes.FirstOrDefault(t => t.Theme == selectedTheme);
    }

    partial void OnSelectedLanguageChanged(LanguageOption value)
    {
        if (value != null)
        {
            _localizationManager.UpdateUserCulture(value.Culture);

            UpdateThemeOptionsLabels();
        }
    }

    partial void OnSelectedThemeChanged(ThemeOption value)
    {
        if (value != null)
        {
            _themeService.SetTheme(value.Theme);
        }
    }

    [RelayCommand]
    private void ResetSettings()
    {
        SelectedLanguage = AvailableLanguages[0];

        SelectedTheme = AvailableThemes.FirstOrDefault(t =>
            t.Theme == AppTheme.Unspecified) ?? AvailableThemes[2];
    }
}