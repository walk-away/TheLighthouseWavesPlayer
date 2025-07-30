using CommunityToolkit.Mvvm.ComponentModel;

namespace TheLighthouseWavesPlayerVideoApp.ViewModels;

public partial class BaseViewModel : ObservableObject
{
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    bool _isBusy;

    [ObservableProperty] private string? _title;

    public bool IsNotBusy => !IsBusy;
}