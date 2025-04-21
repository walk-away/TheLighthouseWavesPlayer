using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TheLighthouseWavesPlayer.Localization.Interfaces;
using TheLighthouseWavesPlayerVideoApp.Models;

namespace TheLighthouseWavesPlayerVideoApp.ViewModels
{
    public partial class SettingsViewModel : BaseViewModel
    {
        private readonly ILocalizationManager _localizationManager;

        [ObservableProperty] private ObservableCollection<LanguageOption> _availableLanguages;

        [ObservableProperty] private LanguageOption _selectedLanguage;

        public SettingsViewModel(ILocalizationManager localizationManager)
        {
            _localizationManager = localizationManager;
            Title = "Settings";

            AvailableLanguages = new ObservableCollection<LanguageOption>
            {
                new LanguageOption { Name = "English", Culture = new CultureInfo("en-US") },
                new LanguageOption { Name = "Русский", Culture = new CultureInfo("ru-RU") }
            };

            var currentCulture = _localizationManager.GetUserCulture();
            SelectedLanguage = AvailableLanguages.FirstOrDefault(l =>
                l.Culture.Name == currentCulture.Name) ?? AvailableLanguages[0];
        }

        partial void OnSelectedLanguageChanged(LanguageOption value)
        {
            if (value != null)
            {
                _localizationManager.UpdateUserCulture(value.Culture);
            }
        }

        [RelayCommand]
        private void ResetSettings()
        {
            SelectedLanguage = AvailableLanguages[0];
        }
    }
}