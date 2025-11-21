using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TheLighthouseWavesPlayerVideoApp.Interfaces;
using TheLighthouseWavesPlayerVideoApp.Localization.Interfaces;
using TheLighthouseWavesPlayerVideoApp.Models;

namespace TheLighthouseWavesPlayerVideoApp.ViewModels;

public sealed partial class SettingsViewModel : BaseViewModel
{
    private readonly ILocalizationManager _localizationManager;
    private readonly IThemeService _themeService;
    private readonly ILocalizedResourcesProvider _resourcesProvider;
    private bool _isInitializing;

    [ObservableProperty]
    private ObservableCollection<LanguageOption> _availableLanguages = new();

    [ObservableProperty]
    private LanguageOption _selectedLanguage = new LanguageOption();

    [ObservableProperty]
    private ObservableCollection<ThemeOption> _availableThemes = new();

    [ObservableProperty]
    private ThemeOption _selectedTheme = new ThemeOption();

    [ObservableProperty]
    private string _pageTitle;

    [ObservableProperty]
    private string _languageLabel;

    public SettingsViewModel(
        ILocalizationManager localizationManager,
        IThemeService themeService,
        ILocalizedResourcesProvider resourcesProvider)
    {
        ArgumentNullException.ThrowIfNull(localizationManager);
        ArgumentNullException.ThrowIfNull(themeService);
        ArgumentNullException.ThrowIfNull(resourcesProvider);

        _localizationManager = localizationManager;
        _themeService = themeService;
        _resourcesProvider = resourcesProvider;

        _pageTitle = _resourcesProvider["Settings_PageTitle"];
        _languageLabel = _resourcesProvider["Settings_LanguageLabel"];

        InitializeLanguages();
        InitializeThemes();
    }

    private void InitializeLanguages()
    {
        AvailableLanguages = new ObservableCollection<LanguageOption>
        {
            new() { Name = "English", Culture = new CultureInfo("en-US") },
            new() { Name = "Русский", Culture = new CultureInfo("ru-RU") }
        };

        var currentCulture = _localizationManager.GetUserCulture() ?? CultureInfo.CurrentCulture;
        SelectedLanguage = AvailableLanguages.FirstOrDefault(l =>
                               l.Culture?.Name == currentCulture.Name)
                           ?? AvailableLanguages[0];
    }

    private void InitializeThemes()
    {
        _isInitializing = true;

        AvailableThemes = new ObservableCollection<ThemeOption>
        {
            new() { Name = _resourcesProvider["Settings_ThemeLight"], Theme = AppTheme.Light },
            new() { Name = _resourcesProvider["Settings_ThemeDark"], Theme = AppTheme.Dark },
            new() { Name = _resourcesProvider["Settings_ThemeSystem"], Theme = AppTheme.Unspecified }
        };

        var currentTheme = _themeService.CurrentTheme;

        SelectedTheme = AvailableThemes.FirstOrDefault(t => t.Theme == currentTheme)
                        ?? AvailableThemes[0];

        _isInitializing = false;
    }

    private void UpdateThemeOptionsLabels()
    {
        _isInitializing = true;

        var selectedThemeValue = SelectedTheme?.Theme ?? AppTheme.Unspecified;

        AvailableThemes.Clear();
        AvailableThemes.Add(
            new ThemeOption { Name = _resourcesProvider["Settings_ThemeLight"], Theme = AppTheme.Light });
        AvailableThemes.Add(new ThemeOption { Name = _resourcesProvider["Settings_ThemeDark"], Theme = AppTheme.Dark });
        AvailableThemes.Add(new ThemeOption
        {
            Name = _resourcesProvider["Settings_ThemeSystem"], Theme = AppTheme.Unspecified
        });

        SelectedTheme = AvailableThemes.FirstOrDefault(t => t.Theme == selectedThemeValue)
                        ?? AvailableThemes[0];

        _isInitializing = false;
    }

    partial void OnSelectedLanguageChanged(LanguageOption value)
    {
        if (_isInitializing || value?.Culture == null)
        {
            return;
        }

        _localizationManager.UpdateUserCulture(value.Culture);

        PageTitle = _resourcesProvider["Settings_PageTitle"];
        LanguageLabel = _resourcesProvider["Settings_LanguageLabel"];

        UpdateThemeOptionsLabels();
    }

    partial void OnSelectedThemeChanged(ThemeOption value)
    {
        if (_isInitializing || value == null)
        {
            return;
        }

        _themeService.SetTheme(value.Theme);
    }

    [RelayCommand]
    private void ResetSettings()
    {
        _isInitializing = true;

        SelectedLanguage = AvailableLanguages[0];
        _localizationManager.UpdateUserCulture(AvailableLanguages[0].Culture!);

        SelectedTheme = AvailableThemes.FirstOrDefault(t => t.Theme == AppTheme.Unspecified)
                        ?? AvailableThemes[0];
        _themeService.SetTheme(AppTheme.Unspecified);

        _isInitializing = false;
    }
}
