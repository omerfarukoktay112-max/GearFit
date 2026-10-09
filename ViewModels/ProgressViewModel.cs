using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GearFit.Controls;
using GearFit.Localization;
using GearFit.Models;
using GearFit.Services;

namespace GearFit.ViewModels;

public record RecordRow(string Label, string Value, string Date);

public partial class ProgressViewModel : ObservableObject
{
    readonly AppDb _db;
    readonly SettingsService _settings;
    List<(SeriesPoint Point, List<WorkoutSet> Sets)> _history = new();
    List<Exercise> _choices = new();

    public ProgressViewModel(AppDb db, SettingsService settings)
    {
        _db = db;
        _settings = settings;
    }

    public ObservableCollection<RecordRow> Records { get; } = new();

    /// <summary>Grafik güncellenmesi gerektiğinde sayfa bu olaya bağlanır.</summary>
    public event Action<IReadOnlyList<ChartPoint>>? ChartChanged;

    [ObservableProperty] Exercise? selected;
    [ObservableProperty] int metricIndex = 1;
    [ObservableProperty] int rangeIndex = 1;
    [ObservableProperty] bool hasData;
    [ObservableProperty] string bigValue = "–";
    [ObservableProperty] string deltaText = "";
    [ObservableProperty] string deltaIcon = "arrow-up";
    [ObservableProperty] bool hasDelta;

    public string SelectedName => Selected?.Name ?? Loc.T("pick_exercise");
    public string MetricLabel => MetricIndex == 1 ? Loc.T("est_1rm") : Loc.T("max_weight");
    public string EmptyText => Loc.T(_choices.Count == 0 ? "progress_empty_none" : "progress_empty_range");

    public bool NoData => !HasData;

    partial void OnHasDataChanged(bool value) => OnPropertyChanged(nameof(NoData));

    partial void OnSelectedChanged(Exercise? value)
    {
        OnPropertyChanged(nameof(SelectedName));
        _ = LoadHistoryAsync();
    }

    partial void OnMetricIndexChanged(int value)
    {
        OnPropertyChanged(nameof(MetricLabel));
        Recompute();
    }

    partial void OnRangeIndexChanged(int value) => Recompute();

    public async Task LoadAsync()
    {
        _choices = await _db.GetExercisesWithHistoryAsync();
        if (Selected == null || _choices.All(c => c.Id != Selected.Id))
            Selected = _choices.FirstOrDefault();
        else await LoadHistoryAsync();
        OnPropertyChanged(nameof(EmptyText));
    }

    async Task LoadHistoryAsync()
    {
        _history = Selected == null ? new() : await _db.GetExerciseHistoryAsync(Selected.Id);
        Recompute();
        BuildRecords();
    }

    DateTime? RangeStart() => RangeIndex switch
    {
        0 => DateTime.Today.AddMonths(-1),
        1 => DateTime.Today.AddMonths(-3),
        2 => DateTime.Today.AddYears(-1),
        _ => null
    };

    void Recompute()
    {
        var start = RangeStart();
        var points = _history
            .Select(h => h.Point)
            .Where(p => start == null || p.Date >= start)
            .Select(p => new ChartPoint(p.Date, _settings.ToDisplay(MetricIndex == 1 ? p.Best1RmKg : p.BestWeightKg)))
            .ToList();

        HasData = points.Count > 0;
        if (HasData)
        {
            var last = points[^1].Value;
            BigValue = $"{_settings.FormatNumber(last)} {_settings.Unit}";
            var delta = last - points[0].Value;
            HasDelta = points.Count > 1 && Math.Abs(delta) >= 0.05;
            DeltaText = $"{_settings.FormatNumber(Math.Abs(delta))} {_settings.Unit}";
            DeltaIcon = delta >= 0 ? "arrow-up" : "arrow-down";
        }
        else
        {
            BigValue = "–";
            HasDelta = false;
        }
        ChartChanged?.Invoke(points);
    }

    void BuildRecords()
    {
        Records.Clear();
        var all = _history.SelectMany(h => h.Sets.Select(s => (Set: s, h.Point.Date))).ToList();
        if (all.Count == 0) return;

        var maxWeight = all.OrderByDescending(x => x.Set.WeightKg).ThenByDescending(x => x.Set.Reps).First();
        var best1Rm = all.OrderByDescending(x => Formulas.OneRm(x.Set.WeightKg, x.Set.Reps)).First();
        var maxReps = all.OrderByDescending(x => x.Set.Reps).ThenByDescending(x => x.Set.WeightKg).First();

        string Date(DateTime d) => d.ToString("d MMM", SettingsService.Tr);
        Records.Add(new RecordRow(Loc.T("max_weight"),
            $"{_settings.FormatWeightUnit(maxWeight.Set.WeightKg)} × {maxWeight.Set.Reps}", Date(maxWeight.Date)));
        Records.Add(new RecordRow(Loc.T("est_1rm"),
            _settings.FormatWeightUnit(Formulas.OneRm(best1Rm.Set.WeightKg, best1Rm.Set.Reps)), Date(best1Rm.Date)));
        Records.Add(new RecordRow(Loc.T("max_reps"),
            $"{_settings.FormatWeightUnit(maxReps.Set.WeightKg)} × {maxReps.Set.Reps}", Date(maxReps.Date)));
    }

    [RelayCommand]
    async Task PickExercise()
    {
        if (_choices.Count == 0) return;
        var choice = await Shell.Current.DisplayActionSheetAsync(Loc.T("pick_exercise"), Loc.T("cancel"), null,
            _choices.Select(c => c.Name).ToArray());
        var picked = _choices.FirstOrDefault(c => c.Name == choice);
        if (picked != null) Selected = picked;
    }

    [RelayCommand]
    Task OpenSettings() => Shell.Current.GoToAsync("settings");

    [RelayCommand]
    Task ShowBody() => Shell.Current.GoToAsync("//body");
}
