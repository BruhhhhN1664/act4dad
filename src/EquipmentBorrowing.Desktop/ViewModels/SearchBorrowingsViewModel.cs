using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Application.Abstractions;
using EquipmentBorrowing.Application.Services;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class SearchBorrowingsViewModel : ViewModelBase, IRefreshable
{
    private readonly BorrowingService _borrowing;

    public SearchBorrowingsViewModel(BorrowingService borrowing) => _borrowing = borrowing;

    public ObservableCollection<BorrowingRecord> Items { get; } = new();
    public StatusFilter[] StatusOptions { get; } = Enum.GetValues<StatusFilter>();
    public SortField[] SortOptions { get; } = Enum.GetValues<SortField>();

    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private StatusFilter _selectedStatus = StatusFilter.All;
    [ObservableProperty] private SortField _selectedSort = SortField.CheckoutDate;
    [ObservableProperty] private bool _descending = true;

    public Task RefreshAsync() => SearchAsync();

    [RelayCommand]
    private Task SearchAsync() => LoadListAsync(
        ct => _borrowing.SearchAsync(new BorrowingQuery(SearchText, SelectedStatus, SelectedSort, Descending), ct),
        Items,
        "No borrowing records match the current search or filters.");

    [RelayCommand]
    private Task ClearAsync()
    {
        SearchText = "";
        SelectedStatus = StatusFilter.All;
        SelectedSort = SortField.CheckoutDate;
        Descending = true;
        return SearchAsync();
    }
}
