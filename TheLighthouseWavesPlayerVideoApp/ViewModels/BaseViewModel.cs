using CommunityToolkit.Mvvm.ComponentModel;

namespace TheLighthouseWavesPlayerVideoApp.ViewModels;

public abstract partial class BaseViewModel : ObservableObject
{
    [ObservableProperty] private bool _isBusy;

    [ObservableProperty] private string _title = string.Empty;

    [ObservableProperty] private bool _isInitialized;

    public virtual Task InitializeAsync()
    {
        IsInitialized = true;
        return Task.CompletedTask;
    }
}