using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Application.Abstractions;
using EquipmentBorrowing.Application.Services;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class ActiveBorrowingsViewModel : ViewModelBase, IRefreshable
{
    private readonly BorrowingService _borrowing;

    public ActiveBorrowingsViewModel(BorrowingService borrowing) => _borrowing = borrowing;

    public ObservableCollection<BorrowingRecord> Items { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ReturnSelectedCommand))]
    private BorrowingRecord? _selected;

    public Task RefreshAsync() => LoadAsync(announce: true);

    private Task<bool> LoadAsync(bool announce) => LoadListAsync(
        ct => _borrowing.GetActiveAsync(ct), Items, "There are no active borrowings.", announce);

    [RelayCommand]
    private Task ReloadAsync() => LoadAsync(announce: true);

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ReturnSelectedAsync()
    {
        if (Selected is null) return;
        IsBusy = true;
        try
        {
            var result = await _borrowing.ReturnAsync(Selected.Id);
            Report(result);
        }
        finally
        {
            IsBusy = false;
        }
        await LoadAsync(announce: false);
    }

    private bool HasSelection() => Selected is not null;
}
