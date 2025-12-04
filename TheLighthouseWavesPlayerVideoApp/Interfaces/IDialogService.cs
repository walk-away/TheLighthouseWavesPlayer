namespace TheLighthouseWavesPlayerVideoApp.Interfaces;


public interface IDialogService
{
    Task<bool> ShowConfirmationAsync(string title, string message, string accept, string cancel);
    Task ShowAlertAsync(string title, string message, string cancel);
    Task NavigateBackAsync();
}
