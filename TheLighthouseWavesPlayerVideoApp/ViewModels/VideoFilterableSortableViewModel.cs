using TheLighthouseWavesPlayerVideoApp.Localization.Interfaces;
using TheLighthouseWavesPlayerVideoApp.Models;

namespace TheLighthouseWavesPlayerVideoApp.ViewModels;

public abstract partial class VideoFilterableSortableViewModel : FilterableSortableViewModel<VideoInfo>
{
    protected VideoFilterableSortableViewModel(ILocalizedResourcesProvider resourcesProvider)
        : base(resourcesProvider)
    {
    }

    protected override IEnumerable<VideoInfo> ApplySearch(IEnumerable<VideoInfo> items)
    {
        if (string.IsNullOrWhiteSpace(SearchText))
            return items;

        var searchLower = SearchText.ToLowerInvariant();
        return items.Where(v =>
            !string.IsNullOrEmpty(v.Title) &&
            v.Title.Contains(searchLower, StringComparison.OrdinalIgnoreCase));
    }

    protected override IEnumerable<VideoInfo> ApplySort(IEnumerable<VideoInfo> items)
    {
        if (SelectedSortOption == null)
            return items;

        return SelectedSortOption.Property switch
        {
            "Title" => SelectedSortOption.IsAscending
                ? items.OrderBy(v => v.Title)
                : items.OrderByDescending(v => v.Title),
            "DurationMilliseconds" => SelectedSortOption.IsAscending
                ? items.OrderBy(v => v.DurationMilliseconds)
                : items.OrderByDescending(v => v.DurationMilliseconds),
            _ => items
        };
    }
}
