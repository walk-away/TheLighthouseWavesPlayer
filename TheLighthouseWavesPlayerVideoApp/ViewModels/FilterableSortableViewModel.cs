using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TheLighthouseWavesPlayerVideoApp.Localization.Interfaces;
using TheLighthouseWavesPlayerVideoApp.Models;

namespace TheLighthouseWavesPlayerVideoApp.ViewModels;

public abstract partial class FilterableSortableViewModel<T> : BaseViewModel where T : class
{
    protected readonly ILocalizedResourcesProvider ResourcesProvider;

    [ObservableProperty]
    private ObservableCollection<T> _allItems = [];

    [ObservableProperty]
    private ObservableCollection<T> _filteredItems = [];

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<SortOption> _sortOptions = [];

    [ObservableProperty]
    private SortOption _selectedSortOption = new(string.Empty, string.Empty, true);

    private string _lastSelectedSortProperty = "Title";
    private bool _lastSelectedSortIsAscending = true;

    protected FilterableSortableViewModel(ILocalizedResourcesProvider resourcesProvider)
    {
        ResourcesProvider = resourcesProvider ?? throw new ArgumentNullException(nameof(resourcesProvider));
        InitializeSortOptions();
    }

    protected virtual void InitializeSortOptions()
    {
        SortOptions =
        [
            new SortOption(ResourcesProvider["Sort_TitleAsc"], "Title", true),
            new SortOption(ResourcesProvider["Sort_TitleDesc"], "Title", false),
            new SortOption(ResourcesProvider["Sort_DurationAsc"], "DurationMilliseconds", true),
            new SortOption(ResourcesProvider["Sort_DurationDesc"], "DurationMilliseconds", false)
        ];
        SelectedSortOption = SortOptions.First();
    }

    protected void UpdateSortOptions()
    {
        _lastSelectedSortProperty = SelectedSortOption.Property;
        _lastSelectedSortIsAscending = SelectedSortOption.IsAscending;

        var newOptions = new ObservableCollection<SortOption>
        {
            new(ResourcesProvider["Sort_TitleAsc"], "Title", true),
            new(ResourcesProvider["Sort_TitleDesc"], "Title", false),
            new(ResourcesProvider["Sort_DurationAsc"], "DurationMilliseconds", true),
            new(ResourcesProvider["Sort_DurationDesc"], "DurationMilliseconds", false)
        };

        SortOptions = newOptions;
        SelectedSortOption = SortOptions.FirstOrDefault(o =>
                                 o.Property == _lastSelectedSortProperty &&
                                 o.IsAscending == _lastSelectedSortIsAscending)
                             ?? SortOptions.First();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();

    partial void OnSelectedSortOptionChanged(SortOption value)
    {
        if (value == null) return;

        _lastSelectedSortProperty = value.Property;
        _lastSelectedSortIsAscending = value.IsAscending;
        ApplyFilters();
    }

    protected virtual void ApplyFilters()
    {
        IEnumerable<T> filtered = AllItems;

        filtered = ApplySearch(filtered);
        filtered = ApplySort(filtered);

        FilteredItems.Clear();
        foreach (var item in filtered)
        {
            FilteredItems.Add(item);
        }
    }

    protected abstract IEnumerable<T> ApplySearch(IEnumerable<T> items);

    protected abstract IEnumerable<T> ApplySort(IEnumerable<T> items);

    [RelayCommand]
    private void ClearSearch()
    {
        SearchText = string.Empty;
    }
}
