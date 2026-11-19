using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Application;
using EquipmentBorrowing.Application.Abstractions;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Desktop.ViewModels;
public partial class HistoryViewModel : ViewModelBase, IRefreshable
{
    private readonly StudentService _students;
    private readonly EquipmentService _equipment;
    private readonly BorrowingService _borrowing;
    private CancellationTokenSource? _studentHistoryCts;
    private CancellationTokenSource? _equipmentHistoryCts;

    public HistoryViewModel(StudentService students, EquipmentService equipment, BorrowingService borrowing)
    {
        _students = students;
        _equipment = equipment;
        _borrowing = borrowing;
    }

    public ObservableCollection<Student> Students { get; } = new();
    public ObservableCollection<Equipment> EquipmentList { get; } = new();
    public ObservableCollection<BorrowingRecord> StudentHistory { get; } = new();
    public ObservableCollection<BorrowingRecord> EquipmentHistory { get; } = new();

    [ObservableProperty] private string _studentSearch = "";
    [ObservableProperty] private string _equipmentSearch = "";
    [ObservableProperty] private Student? _selectedStudent;
    [ObservableProperty] private Equipment? _selectedEquipment;
    [ObservableProperty] private string _studentHistoryNote = "Select a student to see their borrowing history.";
    [ObservableProperty] private string _equipmentHistoryNote = "Select an item to see its borrowing history.";

    partial void OnSelectedStudentChanged(Student? value) => _ = LoadStudentHistoryAsync(value);
    partial void OnSelectedEquipmentChanged(Equipment? value) => _ = LoadEquipmentHistoryAsync(value);

    public async Task RefreshAsync()
    {
        await LoadListAsync(ct => _students.SearchAsync(StudentSearch, ct), Students, "No students match the search.");
        await LoadListAsync(ct => _equipment.SearchAsync(EquipmentSearch, false, ct), EquipmentList, "No equipment matches the search.");
        ShowEmpty = false;
    }

    [RelayCommand]
    private Task SearchAsync() => RefreshAsync();

    private async Task LoadStudentHistoryAsync(Student? student)
    {
        _studentHistoryCts?.Cancel();
        StudentHistory.Clear();
        if (student is null) { StudentHistoryNote = "Select a student to see their borrowing history."; return; }

        _studentHistoryCts = new CancellationTokenSource();
        var token = _studentHistoryCts.Token;
        StudentHistoryNote = "Loading…";
        try
        {
            var result = await _borrowing.GetStudentHistoryAsync(student.Id, token);
            if (token.IsCancellationRequested) return;
            if (!result.Succeeded) { StudentHistoryNote = result.Message; ShowError(result.Message); return; }
            foreach (var r in result.Value!) StudentHistory.Add(r);
            StudentHistoryNote = StudentHistory.Count == 0
                ? $"{student.FullName} has never borrowed equipment."
                : $"{StudentHistory.Count} borrowing(s) for {student.FullName}, newest first.";
        }
        catch (OperationCanceledException) { }
    }

    private async Task LoadEquipmentHistoryAsync(Equipment? item)
    {
        _equipmentHistoryCts?.Cancel();
        EquipmentHistory.Clear();
        if (item is null) { EquipmentHistoryNote = "Select an item to see its borrowing history."; return; }

        _equipmentHistoryCts = new CancellationTokenSource();
        var token = _equipmentHistoryCts.Token;
        EquipmentHistoryNote = "Loading…";
        try
        {
            var result = await _borrowing.GetEquipmentHistoryAsync(item.Id, token);
            if (token.IsCancellationRequested) return;
            if (!result.Succeeded) { EquipmentHistoryNote = result.Message; ShowError(result.Message); return; }
            foreach (var r in result.Value!) EquipmentHistory.Add(r);
            EquipmentHistoryNote = EquipmentHistory.Count == 0
                ? $"{item.Name} has never been borrowed."
                : $"{EquipmentHistory.Count} borrowing(s) of {item.Name}, newest first.";
        }
        catch (OperationCanceledException) { }
    }
}
