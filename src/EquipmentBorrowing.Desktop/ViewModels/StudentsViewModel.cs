using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class StudentsViewModel : ViewModelBase, IRefreshable
{
    private readonly StudentService _students;

    public StudentsViewModel(StudentService students) => _students = students;

    public ObservableCollection<Student> Items { get; } = new();
    [ObservableProperty] private string _searchText = "";

    [ObservableProperty] private string _formNumber = "";
    [ObservableProperty] private string _formName = "";
    [ObservableProperty] private string _formContact = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(PermitCommand))]
    [NotifyCanExecuteChangedFor(nameof(RestrictCommand))]
    private Student? _selected;

    partial void OnSelectedChanged(Student? value)
    {
        if (value is null) return;
        FormNumber = value.StudentNumber;
        FormName = value.FullName;
        FormContact = value.Contact ?? "";
    }

    public Task RefreshAsync() => LoadAsync(announce: true);

    private async Task<bool> LoadAsync(bool announce)
    {
        var keep = Selected?.Id;
        var ok = await LoadListAsync(ct => _students.SearchAsync(SearchText, ct), Items,
            string.IsNullOrWhiteSpace(SearchText) ? "No students are registered yet." : "No students match the search.", announce);
        if (ok && keep is int id) Selected = Items.FirstOrDefault(s => s.Id == id);
        return ok;
    }

    [RelayCommand]
    private Task SearchAsync() => LoadAsync(announce: true);

    [RelayCommand]
    private void NewStudent()
    {
        Selected = null;
        FormNumber = FormName = FormContact = "";
        ShowInfo("Enter the details of the new student and press Register.");
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        var result = await _students.RegisterAsync(FormNumber, FormName, FormContact);
        Report(result);
        if (result.Succeeded)
        {
            Selected = null;
            FormNumber = FormName = FormContact = "";
            await LoadAsync(announce: false);
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task SaveAsync()
    {
        var result = await _students.UpdateAsync(Selected!.Id, FormNumber, FormName, FormContact);
        Report(result);
        await LoadAsync(announce: false);
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private Task PermitAsync() => ChangeEligibilityAsync(true);

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private Task RestrictAsync() => ChangeEligibilityAsync(false);

    private async Task ChangeEligibilityAsync(bool allowed)
    {
        var result = await _students.SetEligibilityAsync(Selected!.Id, allowed);
        Report(result);
        await LoadAsync(announce: false);
    }

    private bool HasSelection() => Selected is not null;
}
