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
        _localizationManager = localizationManager ?? throw new ArgumentNullException(nameof(localizationManager));
        _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
        _resourcesProvider = resourcesProvider ?? throw new ArgumentNullException(nameof(resourcesProvider));

        _pageTitle = _resourcesProvider["Settings_PageTitle"];
        _languageLabel = _resourcesProvider["Settings_LanguageLabel"];

        AvailableLanguages = new ObservableCollection<LanguageOption>
        {
            new LanguageOption { Name = "English", Culture = new CultureInfo("en-US") },
            new LanguageOption { Name = "Русский", Culture = new CultureInfo("ru-RU") }
        };

        var currentCulture = _localizationManager.GetUserCulture() ?? CultureInfo.CurrentCulture;
        SelectedLanguage = AvailableLanguages.FirstOrDefault(l =>
                               l.Culture?.Name == currentCulture.Name)
                           ?? AvailableLanguages[0];

        InitializeThemeOptions();

        if (AvailableThemes.Count > 0)
        {
            var currentTheme = _themeService.CurrentTheme;
            SelectedTheme = AvailableThemes.FirstOrDefault(t =>
                                t.Theme == currentTheme)
                            ?? AvailableThemes[0];
        }
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
        InitializeThemeOptions();
        var selected = SelectedTheme?.Theme ?? AppTheme.Unspecified;
        SelectedTheme = AvailableThemes.FirstOrDefault(t => t.Theme == selected)
                        ?? AvailableThemes[0];
    }

    partial void OnSelectedLanguageChanged(LanguageOption value)
    {
        if (value?.Culture != null)
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
                            t.Theme == AppTheme.Unspecified)
                        ?? AvailableThemes[0];
    }
}
