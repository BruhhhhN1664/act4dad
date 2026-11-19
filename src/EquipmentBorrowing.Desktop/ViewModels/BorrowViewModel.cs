using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Application;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Desktop.ViewModels;
public partial class BorrowViewModel : ViewModelBase, IRefreshable
{
    private readonly StudentService _students;
    private readonly EquipmentService _equipment;
    private readonly BorrowingService _borrowing;
    private readonly DemoFaultSwitch _fault;

    public BorrowViewModel(StudentService students, EquipmentService equipment, BorrowingService borrowing, DemoFaultSwitch fault)
    {
        _students = students;
        _equipment = equipment;
        _borrowing = borrowing;
        _fault = fault;
    }

    public ObservableCollection<Student> Students { get; } = new();
    public ObservableCollection<SelectableEquipment> Equipment { get; } = new();

    [ObservableProperty] private string _studentSearch = "";
    [ObservableProperty] private string _equipmentSearch = "";
    [ObservableProperty] private Student? _selectedStudent;
    [ObservableProperty] private DateTimeOffset? _expectedReturn = DateTimeOffset.Now.AddDays(7);
    [ObservableProperty] private bool _simulateFault;
    public bool DemoMode => Environment.GetEnvironmentVariable("EQUIPMENT_DEMO") == "1";

    public Task RefreshAsync() => ReloadAsync(announce: true);

    private async Task ReloadAsync(bool announce)
    {
        var keep = SelectedStudent?.Id;
        await LoadListAsync(ct => _students.SearchAsync(StudentSearch, ct), Students,
            "No students match the search.", announce);
        SelectedStudent = Students.FirstOrDefault(s => s.Id == keep);

        await LoadEquipmentAsync(announce);
    }

    private async Task LoadEquipmentAsync(bool announce)
    {
        var rows = new ObservableCollection<Domain.Equipment>();
        var ok = await LoadListAsync(ct => _equipment.SearchAsync(EquipmentSearch, availableOnly: true, ct), rows,
            "No available equipment matches the search.", announce);
        if (!ok) { Equipment.Clear(); return; }
        Equipment.Clear();
        foreach (var e in rows) Equipment.Add(new SelectableEquipment(e));
    }

    [RelayCommand]
    private Task SearchAsync() => ReloadAsync(announce: true);

    [RelayCommand]
    private async Task BorrowOneAsync()
    {
        var picked = Picked();
        if (picked.Count != 1) { ShowError("Pick exactly one item for a single borrowing, or use 'Check out ticked items' for several."); return; }
        await RunAsync(() => _borrowing.BorrowAsync(SelectedStudent!.Id, picked[0], ExpectedDate()));
    }

    [RelayCommand]
    private async Task CheckoutMultipleAsync()
    {
        var picked = Picked();
        await RunAsync(() => _borrowing.CheckoutMultipleAsync(SelectedStudent!.Id, picked, ExpectedDate()));
    }

    private List<int> Picked() => Equipment.Where(e => e.IsSelected).Select(e => e.Item.Id).ToList();

    private DateOnly? ExpectedDate() => ExpectedReturn is null ? null : DateOnly.FromDateTime(ExpectedReturn.Value.DateTime);

    private async Task RunAsync(Func<Task<Result<IReadOnlyList<int>>>> action)
    {
        if (SelectedStudent is null) { ShowError("Select a student first."); return; }
        IsBusy = true;
        try
        {
            _fault.Enabled = DemoMode && SimulateFault;
            var result = await action();
            Report(result);
            await ReloadAsync(announce: false);
        }
        finally
        {
            _fault.Enabled = false;
            IsBusy = false;
        }
    }
}
