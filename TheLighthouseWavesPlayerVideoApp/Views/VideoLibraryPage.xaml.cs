using TheLighthouseWavesPlayerVideoApp.ViewModels;

namespace TheLighthouseWavesPlayerVideoApp.Views;

public partial class VideoLibraryPage : ContentPage
{
    private readonly VideoLibraryViewModel _viewModel;

    public VideoLibraryPage(VideoLibraryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_viewModel != null)
        {
            await _viewModel.OnAppearing();
            AnimateItems();
        }
    }
    
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
    }
    
    private async void AnimateItems()
    {
        if (Content is Grid grid && grid.Children.Count > 0)
        {
            CollectionView collectionView = null;
                
            foreach (var child in grid.Children)
            {
                if (child is CollectionView cv)
                {
                    collectionView = cv;
                    break;
                }
            }
                
            if (collectionView != null)
            {
                await Task.Delay(100);
                    
                var itemsLayout = collectionView.GetVisualTreeDescendants()
                        .FirstOrDefault(x => x is Microsoft.Maui.Controls.Compatibility.StackLayout) 
                    as Microsoft.Maui.Controls.Compatibility.StackLayout;
                            
                if (itemsLayout != null)
                {
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
            }
        }
    }
}