using TheLighthouseWavesPlayerVideoApp.Interfaces;

namespace TheLighthouseWavesPlayerVideoApp.Services;

public class DialogService : IDialogService
{
    public async Task<bool> ShowConfirmationAsync(string title, string message, string accept, string cancel)
    {
        return await MainThread.InvokeOnMainThreadAsync(async () => await Shell.Current.DisplayAlert(title, message, accept, cancel));
    }

    public async Task ShowAlertAsync(string title, string message, string cancel)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await Shell.Current.DisplayAlert(title, message, cancel);
        });
    }

    public async Task NavigateBackAsync()
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await Shell.Current.GoToAsync("..");
        });
    }
}
