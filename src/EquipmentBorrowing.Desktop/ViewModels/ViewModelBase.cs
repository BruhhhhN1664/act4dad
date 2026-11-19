using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using EquipmentBorrowing.Application;

namespace EquipmentBorrowing.Desktop.ViewModels;

public interface IRefreshable
{
    Task RefreshAsync();
}

public abstract partial class ViewModelBase : ObservableObject
{
    private int _loadVersion;
    private CancellationTokenSource? _cts;

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private bool _statusIsError;
    [ObservableProperty] private bool _showEmpty;
    [ObservableProperty] private string _emptyMessage = "";

    protected void ShowInfo(string message) { StatusIsError = false; StatusMessage = message; }
    protected void ShowError(string message) { StatusIsError = true; StatusMessage = message; }

    protected void Report<T>(Result<T> result)
    {
        if (result.Succeeded) ShowInfo(result.Message); else ShowError(result.Message);
    }
    protected async Task<bool> LoadListAsync<T>(
        Func<CancellationToken, Task<Result<IReadOnlyList<T>>>> fetch,
        ObservableCollection<T> target,
        string emptyText,
        bool announce = true)
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        var version = ++_loadVersion;

        IsBusy = true;
        ShowEmpty = false;
        if (announce) ShowInfo("Loading…");
        try
        {
            var result = await fetch(token);
            if (token.IsCancellationRequested) return false;

            target.Clear();
            if (!result.Succeeded)
            {
                ShowError(result.Message);
                return false;
            }
            foreach (var item in result.Value!) target.Add(item);
            EmptyMessage = emptyText;
            ShowEmpty = target.Count == 0;
            if (announce) ShowInfo(target.Count == 0 ? "" : $"{target.Count} record(s) shown.");
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        finally
        {
            if (version == _loadVersion) IsBusy = false;
        }
    }
}
