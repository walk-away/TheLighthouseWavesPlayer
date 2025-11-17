using TheLighthouseWavesPlayerVideoApp.ViewModels;

namespace TheLighthouseWavesPlayerVideoApp.Views;

public partial class VideoLibraryPage
{
    private readonly VideoLibraryViewModel _viewModel;
    private bool _isAnimating;

    public VideoLibraryPage(VideoLibraryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        Task.Run(async () =>
        {
            try
            {
                if (_viewModel != null)
                {
                    await _viewModel.OnAppearing();
                    await MainThread.InvokeOnMainThreadAsync(() => AnimateItemsAsync());
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in VideoLibraryPage.OnAppearing: {ex.Message}");
            }
        });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
    }

    private async Task AnimateItemsAsync()
    {
        if (_isAnimating)
        {
            return;
        }

        try
        {
            _isAnimating = true;

            if (Content is not Grid grid || grid.Children.Count == 0)
            {
                return;
            }

            CollectionView? collectionView = null;

            foreach (var child in grid.Children)
            {
                if (child is CollectionView cv)
                {
                    collectionView = cv;
                    break;
                }
            }

            if (collectionView == null)
            {
                return;
            }

            await Task.Delay(100);

            var itemsLayout = collectionView.GetVisualTreeDescendants()
                    .FirstOrDefault(x => x is Microsoft.Maui.Controls.Compatibility.StackLayout)
                as Microsoft.Maui.Controls.Compatibility.StackLayout;

            if (itemsLayout == null)
            {
                return;
            }

            uint delay = 0;
            foreach (var item in itemsLayout.Children)
            {
                if (item is Grid itemGrid)
                {
                    itemGrid.Opacity = 0;
                    itemGrid.TranslationY = 50;

                    await Task.Delay((int)delay);
                    await Task.WhenAll(
                        itemGrid.FadeTo(1, 300),
                        itemGrid.TranslateTo(0, 0, 300, Easing.SpringOut)
                    );

                    delay += 50;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in AnimateItems: {ex.Message}");
        }
        finally
        {
            _isAnimating = false;
        }
    }
}
