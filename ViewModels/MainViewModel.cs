using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GearFit.Localization;
using GearFit.Models;
using GearFit.Services;

namespace GearFit.ViewModels;

public partial class MainViewModel : ObservableObject
{
    readonly AppDb _db;
    readonly SettingsService _settings;
    readonly SelectionService _selection;

    public MainViewModel(AppDb db, SettingsService settings, ActiveWorkoutService active, SelectionService selection)
    {
        _db = db;
        _settings = settings;
        _selection = selection;
        Active = active;
        active.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(ActiveWorkoutService.IsActive) or nameof(ActiveWorkoutService.ElapsedText))
                OnPropertyChanged(nameof(StartButtonText));
        };
    }

    public ActiveWorkoutService Active { get; }
    public ObservableCollection<RoutineSummary> Routines { get; } = new();

    [ObservableProperty] int weekCount;
    [ObservableProperty] int goal = 4;
    [ObservableProperty] int streak;

    public double GoalProgress => Goal <= 0 ? 0 : Math.Min(1, (double)WeekCount / Goal);
    public string GoalText => $"{WeekCount}/{Goal}";
    public string GoalCaption => Loc.T("week_caption", WeekCount, Goal);
    public string StreakText => Loc.T("streak", Streak);
    public bool HasStreak => Streak > 0;
    public string StartButtonText => Active.IsActive ? Loc.T("resume_workout", Active.ElapsedText) : Loc.T("start_empty");
    public string StartIcon => Active.IsActive ? "play" : "plus";

    partial void OnWeekCountChanged(int value) => NotifyGoal();
    partial void OnGoalChanged(int value) => NotifyGoal();
    partial void OnStreakChanged(int value)
    {
        OnPropertyChanged(nameof(StreakText));
        OnPropertyChanged(nameof(HasStreak));
    }

    void NotifyGoal()
    {
        OnPropertyChanged(nameof(GoalProgress));
        OnPropertyChanged(nameof(GoalText));
        OnPropertyChanged(nameof(GoalCaption));
    }

    public async Task LoadAsync()
    {
        Goal = _settings.WeeklyGoal;
        var routines = await _db.GetRoutinesAsync();
        Routines.Clear();
        foreach (var r in routines) Routines.Add(r);

        var workouts = await _db.GetWorkoutsAsync();
        var weekStart = StartOfWeek(DateTime.Today);
        WeekCount = workouts.Count(w => w.StartedAt >= weekStart);
        Streak = ComputeStreak(workouts, Goal);
        OnPropertyChanged(nameof(StartButtonText));
        OnPropertyChanged(nameof(StartIcon));
    }

    static DateTime StartOfWeek(DateTime d)
    {
        var diff = ((int)d.DayOfWeek + 6) % 7; // Pazartesi = 0
        return d.Date.AddDays(-diff);
    }

    static int ComputeStreak(List<Workout> workouts, int goal)
    {
        var counts = workouts.GroupBy(w => StartOfWeek(w.StartedAt)).ToDictionary(g => g.Key, g => g.Count());
        var week = StartOfWeek(DateTime.Today);
        var streak = 0;
        if (counts.GetValueOrDefault(week) >= goal) streak++;
        week = week.AddDays(-7);
        while (counts.GetValueOrDefault(week) >= goal)
        {
            streak++;
            week = week.AddDays(-7);
        }
        return streak;
    }

    async Task<bool> ConfirmReplaceAsync()
    {
        if (!Active.IsActive) return true;
        var replace = await Shell.Current.DisplayAlertAsync(Loc.T("active_exists_title"),
            Loc.T("active_exists_msg"), Loc.T("start_new"), Loc.T("cancel"));
        return replace;
    }

    [RelayCommand]
    async Task StartEmpty()
    {
        if (!Active.IsActive) await Active.StartAsync(Loc.T("default_workout"), Array.Empty<Exercise>());
        await Shell.Current.GoToAsync("active");
    }

    [RelayCommand]
    async Task StartRoutine(RoutineSummary routine)
    {
        if (!await ConfirmReplaceAsync()) return;
        var exercises = await _db.GetRoutineExercisesAsync(routine.Id);
        await Active.StartAsync(routine.Name, exercises);
        await Shell.Current.GoToAsync("active");
    }

    [RelayCommand]
    Task NewRoutine() => Shell.Current.GoToAsync("routine-edit?id=0");

    [RelayCommand]
    async Task RoutineMenu(RoutineSummary routine)
    {
        var edit = Loc.T("edit");
        var delete = Loc.T("delete");
        var choice = await Shell.Current.DisplayActionSheetAsync(routine.Name, Loc.T("cancel"), null, edit, delete);
        if (choice == edit)
        {
            await Shell.Current.GoToAsync($"routine-edit?id={routine.Id}");
        }
        else if (choice == delete)
        {
            var ok = await Shell.Current.DisplayAlertAsync(Loc.T("delete_routine"), Loc.T("delete_routine_msg", routine.Name), delete, Loc.T("cancel"));
            if (!ok) return;
            await _db.DeleteRoutineAsync(routine.Id);
            await LoadAsync();
        }
    }

    [RelayCommand]
    Task OpenSettings() => Shell.Current.GoToAsync("settings");
}
