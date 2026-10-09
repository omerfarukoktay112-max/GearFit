using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GearFit.Localization;
using GearFit.Models;
using GearFit.Services;

namespace GearFit.ViewModels;

[QueryProperty(nameof(RoutineId), "id")]
public partial class RoutineEditViewModel : ObservableObject
{
    readonly AppDb _db;
    readonly SelectionService _selection;
    bool _loaded;

    public RoutineEditViewModel(AppDb db, SelectionService selection)
    {
        _db = db;
        _selection = selection;
    }

    public ObservableCollection<Exercise> Exercises { get; } = new();

    [ObservableProperty] int routineId;
    [ObservableProperty] string name = "";

    public string Title => Loc.T(RoutineId == 0 ? "routine_new_title" : "routine_edit_title");
    public bool CanSave => !string.IsNullOrWhiteSpace(Name) && Exercises.Count > 0;
    public bool IsEmpty => Exercises.Count == 0;

    partial void OnNameChanged(string value) => OnPropertyChanged(nameof(CanSave));

    public async Task LoadAsync()
    {
        if (_loaded) return;
        _loaded = true;
        OnPropertyChanged(nameof(Title));
        if (RoutineId == 0) return;
        Name = await _db.GetRoutineNameAsync(RoutineId);
        foreach (var e in await _db.GetRoutineExercisesAsync(RoutineId)) Exercises.Add(e);
        Notify();
    }

    void Notify()
    {
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(IsEmpty));
    }

    [RelayCommand]
    async Task AddExercises()
    {
        _selection.Exclude.Clear();
        foreach (var e in Exercises) _selection.Exclude.Add(e.Id);
        _selection.OnPicked = list =>
        {
            foreach (var e in list) Exercises.Add(e);
            Notify();
            return Task.CompletedTask;
        };
        await Shell.Current.GoToAsync("library");
    }

    [RelayCommand]
    void Remove(Exercise exercise)
    {
        Exercises.Remove(exercise);
        Notify();
    }

    [RelayCommand]
    void MoveUp(Exercise exercise)
    {
        var i = Exercises.IndexOf(exercise);
        if (i > 0) Exercises.Move(i, i - 1);
    }

    [RelayCommand]
    async Task Save()
    {
        if (!CanSave) return;
        await _db.SaveRoutineAsync(RoutineId, Name.Trim(), Exercises.Select(e => e.Id));
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    Task Close() => Shell.Current.GoToAsync("..");
}
