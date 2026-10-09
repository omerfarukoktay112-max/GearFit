using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GearFit.Localization;
using GearFit.Models;
using GearFit.Services;

namespace GearFit.ViewModels;

public class WorkoutRow
{
    public int Id { get; init; }
    public string Dow { get; init; } = "";
    public string Day { get; init; } = "";
    public string Name { get; init; } = "";
    public string Subtitle { get; init; } = "";
}

public class MonthGroup : ObservableCollection<WorkoutRow>
{
    public MonthGroup(string title, IEnumerable<WorkoutRow> rows) : base(rows) => Title = title;
    public string Title { get; }
}

public record CalendarCell(string Day, int WorkoutId, bool IsToday, bool IsBlank)
{
    public bool HasWorkout => WorkoutId > 0;
    public bool IsReal => !IsBlank;
}

public partial class HistoryViewModel : ObservableObject
{
    readonly AppDb _db;
    readonly SettingsService _settings;
    List<Workout> _workouts = new();

    public HistoryViewModel(AppDb db, SettingsService settings)
    {
        _db = db;
        _settings = settings;
        month = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
    }

    public ObservableCollection<MonthGroup> Groups { get; } = new();
    public ObservableCollection<CalendarCell> Cells { get; } = new();
    public ObservableCollection<WorkoutRow> MonthRows { get; } = new();

    [ObservableProperty] int modeIndex;
    [ObservableProperty] DateTime month;
    [ObservableProperty] bool isEmpty;

    public bool IsList => ModeIndex == 0;
    public bool IsCalendar => ModeIndex == 1;
    public string MonthTitle => Month.ToString("MMMM yyyy", SettingsService.Tr);

    partial void OnModeIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsList));
        OnPropertyChanged(nameof(IsCalendar));
    }

    partial void OnMonthChanged(DateTime value)
    {
        OnPropertyChanged(nameof(MonthTitle));
        BuildCalendar();
    }

    public async Task LoadAsync()
    {
        _workouts = await _db.GetWorkoutsAsync();
        IsEmpty = _workouts.Count == 0;
        Groups.Clear();
        foreach (var g in _workouts.GroupBy(w => new DateTime(w.StartedAt.Year, w.StartedAt.Month, 1)))
            Groups.Add(new MonthGroup(g.Key.ToString("MMMM yyyy", SettingsService.Tr), g.Select(ToRow)));
        BuildCalendar();
    }

    WorkoutRow ToRow(Workout w) => new()
    {
        Id = w.Id,
        Dow = w.StartedAt.ToString("ddd", SettingsService.Tr),
        Day = w.StartedAt.Day.ToString(),
        Name = w.Name,
        Subtitle = $"{Math.Max(1, w.DurationSeconds / 60)} {Loc.T("min_short")} · {_settings.FormatVolume(w.VolumeKg)}"
    };

    void BuildCalendar()
    {
        Cells.Clear();
        MonthRows.Clear();
        var first = Month;
        var offset = ((int)first.DayOfWeek + 6) % 7; // Pazartesi başlangıç
        for (var i = 0; i < offset; i++) Cells.Add(new CalendarCell("", 0, false, true));

        var inMonth = _workouts.Where(w => w.StartedAt.Year == first.Year && w.StartedAt.Month == first.Month).ToList();
        for (var d = 1; d <= DateTime.DaysInMonth(first.Year, first.Month); d++)
        {
            var date = new DateTime(first.Year, first.Month, d);
            var workout = inMonth.FirstOrDefault(w => w.StartedAt.Date == date);
            Cells.Add(new CalendarCell(d.ToString(), workout?.Id ?? 0, date == DateTime.Today, false));
        }
        foreach (var w in inMonth) MonthRows.Add(ToRow(w));
    }

    [RelayCommand]
    void PreviousMonth() => Month = Month.AddMonths(-1);

    [RelayCommand]
    void NextMonth() => Month = Month.AddMonths(1);

    [RelayCommand]
    Task Open(WorkoutRow row) => Shell.Current.GoToAsync($"detail?id={row.Id}");

    [RelayCommand]
    async Task OpenCell(CalendarCell cell)
    {
        if (cell.HasWorkout) await Shell.Current.GoToAsync($"detail?id={cell.WorkoutId}");
    }

    [RelayCommand]
    Task OpenSettings() => Shell.Current.GoToAsync("settings");
}
