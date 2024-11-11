using TheLighthouseWavesPlayer.Core.Enums;

namespace TheLighthouseWavesPlayer.Core.Interfaces.Navigation;

public interface INavigatable
{
    Task<bool> CanNavigateFrom(NavigationType navigationType);
}