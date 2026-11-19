using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class CatalogViewModel : ViewModelBase, IRefreshable
{
    private readonly EquipmentService _equipment;

    public CatalogViewModel(EquipmentService equipment) => _equipment = equipment;

    public ObservableCollection<Equipment> Items { get; } = new();
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private bool _availableOnly;

    public Task RefreshAsync() => LoadAsync();

    [RelayCommand]
    private Task LoadAsync() => LoadListAsync(
        ct => _equipment.SearchAsync(SearchText, AvailableOnly, ct), Items,
        AvailableOnly || !string.IsNullOrWhiteSpace(SearchText)
            ? "No equipment matches the current search or filter."
            : "The catalog is empty. Register equipment on the Equipment tab.");

    [RelayCommand]
    private Task ClearAsync()
    {
        SearchText = "";
        AvailableOnly = false;
        return LoadAsync();
    }
}
