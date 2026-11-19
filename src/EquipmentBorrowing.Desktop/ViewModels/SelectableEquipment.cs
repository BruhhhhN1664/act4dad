using CommunityToolkit.Mvvm.ComponentModel;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class SelectableEquipment : ObservableObject
{
    public SelectableEquipment(Equipment item) => Item = item;
    public Equipment Item { get; }
    [ObservableProperty] private bool _isSelected;

    public string Identifier => Item.EquipmentIdentifier;
    public string Name => Item.Name;
    public string Description => Item.Description ?? "";
}
