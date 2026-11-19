using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Desktop.ViewModels;
public partial class EquipmentManagementViewModel : ViewModelBase, IRefreshable
{
    private readonly EquipmentService _equipment;

    public EquipmentManagementViewModel(EquipmentService equipment) => _equipment = equipment;

    public ObservableCollection<Equipment> Items { get; } = new();
    [ObservableProperty] private string _searchText = "";

    [ObservableProperty] private string _formIdentifier = "";
    [ObservableProperty] private string _formName = "";
    [ObservableProperty] private string _formDescription = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private Equipment? _selected;

    partial void OnSelectedChanged(Equipment? value)
    {
        if (value is null) return;
        FormIdentifier = value.EquipmentIdentifier;
        FormName = value.Name;
        FormDescription = value.Description ?? "";
    }

    public Task RefreshAsync() => LoadAsync(announce: true);

    private async Task<bool> LoadAsync(bool announce)
    {
        var keep = Selected?.Id;
        var ok = await LoadListAsync(ct => _equipment.SearchAsync(SearchText, false, ct), Items,
            string.IsNullOrWhiteSpace(SearchText) ? "No equipment is registered yet." : "No equipment matches the search.", announce);
        if (ok && keep is int id) Selected = Items.FirstOrDefault(e => e.Id == id);
        return ok;
    }

    [RelayCommand]
    private Task SearchAsync() => LoadAsync(announce: true);

    [RelayCommand]
    private void NewEquipment()
    {
        Selected = null;
        FormIdentifier = FormName = FormDescription = "";
        ShowInfo("Enter the details of the new equipment and press Register.");
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        var result = await _equipment.RegisterAsync(FormIdentifier, FormName, FormDescription);
        Report(result);
        if (result.Succeeded)
        {
            Selected = null;
            FormIdentifier = FormName = FormDescription = "";
            await LoadAsync(announce: false);
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task SaveAsync()
    {
        var result = await _equipment.UpdateAsync(Selected!.Id, FormIdentifier, FormName, FormDescription);
        Report(result);
        await LoadAsync(announce: false);
    }

    private bool HasSelection() => Selected is not null;
}
