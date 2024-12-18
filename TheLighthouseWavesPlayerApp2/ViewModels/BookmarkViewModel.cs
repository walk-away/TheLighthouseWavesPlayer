using System.Collections.ObjectModel;
using System.Windows.Input;
using TheLighthouseWavesPlayerApp2.Models;
using TheLighthouseWavesPlayerApp2.Services.Interfaces;

namespace TheLighthouseWavesPlayerApp2.ViewModels;

public class BookmarkViewModel : BaseViewModel
{
    private readonly IBookmarkService _bookmarkService;
    public ObservableCollection<Bookmark> Bookmarks { get; } = new();

    public BookmarkViewModel(IBookmarkService bookmarkService)
    {
        _bookmarkService = bookmarkService;
        LoadBookmarksCommand = new Command<int>(async (videoId) => await LoadBookmarks(videoId));
    }

    public ICommand LoadBookmarksCommand { get; }

    private async Task LoadBookmarks(int videoId)
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            var bookmarks = await _bookmarkService.GetBookmarksForVideoAsync(videoId);
            Bookmarks.Clear();
            foreach (var bookmark in bookmarks)
            {
                Bookmarks.Add(bookmark);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}
