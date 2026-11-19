using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Application.Abstractions;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IDatabaseInitializer _initializer;
    private readonly IRefreshable[] _tabs;

    public MainWindowViewModel(
        IDatabaseInitializer initializer,
        CatalogViewModel catalog,
        BorrowViewModel borrow,
        ActiveBorrowingsViewModel active,
        StudentsViewModel students,
        EquipmentManagementViewModel equipment,
        HistoryViewModel history,
        SearchBorrowingsViewModel search)
    {
        _initializer = initializer;
        Catalog = catalog; Borrow = borrow; Active = active; Students = students;
        Equipment = equipment; History = history; Search = search;
        _tabs = new IRefreshable[] { catalog, borrow, active, students, equipment, history, search };
        _ = InitializeAsync();
    }

    public CatalogViewModel Catalog { get; }
    public BorrowViewModel Borrow { get; }
    public ActiveBorrowingsViewModel Active { get; }
    public StudentsViewModel Students { get; }
    public EquipmentManagementViewModel Equipment { get; }
    public HistoryViewModel History { get; }
    public SearchBorrowingsViewModel Search { get; }

    [ObservableProperty] private int _selectedTabIndex;
    [ObservableProperty] private bool _isReady;
    [ObservableProperty] private bool _hasStartupError;
    [ObservableProperty] private string _startupMessage = "Connecting to PostgreSQL…";

    partial void OnSelectedTabIndexChanged(int value)
    {
        if (IsReady && value >= 0 && value < _tabs.Length) _ = _tabs[value].RefreshAsync();
    }

    [RelayCommand]
    private Task RetryAsync() => InitializeAsync();

    private async Task InitializeAsync()
    {
        IsBusy = true;
        HasStartupError = false;
        StartupMessage = "Connecting to PostgreSQL…";
        var result = await _initializer.InitializeAsync(CancellationToken.None);
        IsBusy = false;
        if (!result.Succeeded)
        {
            HasStartupError = true;
            StartupMessage = result.Message;
            IsReady = false;
            return;
        }
        StartupMessage = "";
        IsReady = true;
        await _tabs[SelectedTabIndex].RefreshAsync();
    }
}
