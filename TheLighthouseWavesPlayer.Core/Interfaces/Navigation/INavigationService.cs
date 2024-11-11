namespace TheLighthouseWavesPlayer.Core.Interfaces.Navigation;

public interface INavigationService
{
    Task GoToOverview();
    Task GoBack();
    Task GoBackAndReturn(Dictionary<string, object> parameters);
    Task GoToChooseLanguage(string currentLanguage);

}