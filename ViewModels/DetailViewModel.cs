using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GearFit.Localization;
using GearFit.Models;
using GearFit.Services;

namespace GearFit.ViewModels;

public record SetRow(int Number, string Text, bool IsPr);

public record ExerciseBlock(string Name, List<SetRow> Sets);

[QueryProperty(nameof(WorkoutId), "id")]
public partial class DetailViewModel : ObservableObject
{
    readonly AppDb _db;
    readonly SettingsService _settings;
    readonly ActiveWorkoutService _active;

    public DetailViewModel(AppDb db, SettingsService settings, ActiveWorkoutService active)
    {
        _db = db;
        _settings = settings;
        _active = active;
    }

    public ObservableCollection<ExerciseBlock> Blocks { get; } = new();

    [ObservableProperty] int workoutId;
    [ObservableProperty] string title = "";
    [ObservableProperty] string dateText = "";
    [ObservableProperty] string durationText = "";
    [ObservableProperty] string volumeText = "";
    [ObservableProperty] string setCountText = "";

    partial void OnWorkoutIdChanged(int value) => _ = LoadAsync();

    public async Task LoadAsync()
    {
        if (WorkoutId == 0) return;
        var detail = await _db.GetWorkoutDetailAsync(WorkoutId);
        if (detail == null) return;
        var w = detail.Workout;
        Title = w.Name;
        DateText = w.StartedAt.ToString("dddd d MMMM · HH:mm", SettingsService.Tr);
        DurationText = $"{Math.Max(1, w.DurationSeconds / 60)} {Loc.T("min_short")}";
        VolumeText = _settings.FormatVolume(w.VolumeKg);
        SetCountText = w.SetCount.ToString();

        Blocks.Clear();
        foreach (var e in detail.Exercises)
        {
            var rows = e.Sets.Select(s => new SetRow(s.Number,
                $"{_settings.FormatWeightUnit(s.WeightKg)} × {s.Reps}", s.IsPr)).ToList();
            Blocks.Add(new ExerciseBlock(e.Exercise.ExerciseName, rows));
        }
    }

    [RelayCommand]
    Task Back() => Shell.Current.GoToAsync("..");

    [RelayCommand]
    async Task Rename()
    {
        var name = await Shell.Current.DisplayPromptAsync(Loc.T("rename_title"), null, Loc.T("save"), Loc.T("cancel"),
            initialValue: Title, maxLength: 40);
        if (string.IsNullOrWhiteSpace(name)) return;
        await _db.RenameWorkoutAsync(WorkoutId, name.Trim());
        Title = name.Trim();
    }

    [RelayCommand]
    async Task Delete()
    {
        var ok = await Shell.Current.DisplayAlertAsync(Loc.T("delete_workout"),
            Loc.T("delete_workout_msg"), Loc.T("delete"), Loc.T("cancel"));
        if (!ok) return;
        await _db.DeleteWorkoutAsync(WorkoutId);
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    async Task Repeat()
    {
        if (_active.IsActive)
        {
            var replace = await Shell.Current.DisplayAlertAsync(Loc.T("active_exists_title"),
                Loc.T("active_exists_msg"), Loc.T("start_new"), Loc.T("cancel"));
            if (!replace) return;
        }
        var detail = await _db.GetWorkoutDetailAsync(WorkoutId);
        if (detail == null) return;
        var all = await _db.GetExercisesAsync();
        var exercises = detail.Exercises
            .Select(e => all.FirstOrDefault(x => x.Id == e.Exercise.ExerciseId))
            .Where(x => x != null)
            .Cast<Exercise>()
            .ToList();
        await _active.StartAsync(detail.Workout.Name, exercises);
        await Shell.Current.GoToAsync("active");
    }
}
