using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GearFit.Controls;
using GearFit.Localization;
using GearFit.Models;
using GearFit.Services;

namespace GearFit.ViewModels;

public record BodyRow(string Date, string Value);

public partial class BodyViewModel : ObservableObject
{
    readonly AppDb _db;
    readonly SettingsService _settings;
    List<BodyWeightEntry> _entries = new();

    public BodyViewModel(AppDb db, SettingsService settings)
    {
        _db = db;
        _settings = settings;
    }

    public ObservableCollection<BodyRow> Recent { get; } = new();

    public event Action<IReadOnlyList<ChartPoint>>? ChartChanged;

    [ObservableProperty] string inputText = "";
    [ObservableProperty] int rangeIndex = 1;
    [ObservableProperty] bool hasData;
    [ObservableProperty] string bigValue = "–";
    [ObservableProperty] string deltaText = "";
    [ObservableProperty] string deltaIcon = "arrow-down";
    [ObservableProperty] bool hasDelta;

    public string Unit => _settings.Unit;

    public bool NoData => !HasData;

    partial void OnHasDataChanged(bool value) => OnPropertyChanged(nameof(NoData));

    partial void OnRangeIndexChanged(int value) => Recompute();

    public async Task LoadAsync()
    {
        _entries = await _db.GetBodyWeightsAsync();
        if (string.IsNullOrEmpty(InputText) && _entries.Count > 0)
            InputText = _settings.FormatWeight(_entries[^1].Kg);
        OnPropertyChanged(nameof(Unit));
        Recompute();
        Recent.Clear();
        foreach (var e in _entries.AsEnumerable().Reverse().Take(5))
            Recent.Add(new BodyRow(e.Date.ToString("d MMM", SettingsService.Tr), _settings.FormatWeightUnit(e.Kg)));
    }

    void Recompute()
    {
        DateTime? start = RangeIndex switch
        {
            0 => DateTime.Today.AddMonths(-1),
            1 => DateTime.Today.AddMonths(-3),
            2 => DateTime.Today.AddYears(-1),
            _ => null
        };
        var points = _entries
            .Where(e => start == null || e.Date >= start)
            .Select(e => new ChartPoint(e.Date, _settings.ToDisplay(e.Kg)))
            .ToList();

        HasData = points.Count > 0;
        if (HasData)
        {
            BigValue = $"{_settings.FormatNumber(points[^1].Value)} {_settings.Unit}";
            var delta = points[^1].Value - points[0].Value;
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

    [RelayCommand]
    async Task Save()
    {
        if (!SettingsService.TryParse(InputText, out var value) || value <= 0)
        {
            await Shell.Current.DisplayAlertAsync(Loc.T("invalid_value"), Loc.T("invalid_weight_msg"), Loc.T("ok"));
            return;
        }
        await _db.SaveBodyWeightAsync(_settings.FromDisplay(value));
        await LoadAsync();
    }

    [RelayCommand]
    Task OpenSettings() => Shell.Current.GoToAsync("settings");

    [RelayCommand]
    Task ShowExercises() => Shell.Current.GoToAsync("//progress");
}
