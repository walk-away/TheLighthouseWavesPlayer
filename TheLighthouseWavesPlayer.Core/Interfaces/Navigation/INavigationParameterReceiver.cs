namespace TheLighthouseWavesPlayer.Core.Interfaces.Navigation;

public interface INavigationParameterReceiver
{
    Task OnNavigatedTo(Dictionary<string, object> parameters);
}