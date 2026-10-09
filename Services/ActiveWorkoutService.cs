using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using GearFit.Models;

namespace GearFit.Services;

public partial class ActiveSetVM : ObservableObject
{
    readonly SettingsService _settings;
    bool _syncing;

    public ActiveSetVM(ActiveExerciseVM parent, SettingsService settings, int number, string previous, double weightKg, int reps)
    {
        Parent = parent;
        _settings = settings;
        Number = number;
        PreviousText = previous;
        WeightKg = weightKg;
        Reps = reps;
        RefreshTexts();
    }

    public ActiveExerciseVM Parent { get; }
    public double WeightKg { get; private set; }
    public int Reps { get; private set; }

    [ObservableProperty] int number;
    [ObservableProperty] string previousText = "–";
    [ObservableProperty] bool done;
    [ObservableProperty] bool isPr;
    [ObservableProperty] bool edited;
    [ObservableProperty] string weightText = "";
    [ObservableProperty] string repsText = "";

    partial void OnWeightTextChanged(string value)
    {
        if (_syncing) return;
        Edited = true;
        WeightKg = SettingsService.TryParse(value, out var v) ? _settings.FromDisplay(v) : 0;
    }

    partial void OnRepsTextChanged(string value)
    {
        if (_syncing) return;
        Edited = true;
        Reps = int.TryParse(value?.Trim(), out var r) && r >= 0 ? r : 0;
    }

    public void RefreshTexts()
    {
        _syncing = true;
        WeightText = WeightKg > 0 ? _settings.FormatWeight(WeightKg) : "";
        RepsText = Reps > 0 ? Reps.ToString() : "";
        _syncing = false;
    }
}

public partial class ActiveExerciseVM : ObservableObject
{
    public ActiveExerciseVM(Exercise exercise) => Exercise = exercise;

    public Exercise Exercise { get; }
    public string Name => Exercise.Name;
    public ObservableCollection<ActiveSetVM> Sets { get; } = new();
    public double Best1RmBefore { get; set; }
    public double RunningBest { get; set; }
}

/// <summary>Devam eden antrenmanın durumu, sayaçlar ve kayıt işlemleri.</summary>
public partial class ActiveWorkoutService : ObservableObject
{
    readonly AppDb _db;
    readonly SettingsService _settings;
    IDispatcherTimer? _timer;
    DateTime _restEnd;
    int _restTotal;

    public ActiveWorkoutService(AppDb db, SettingsService settings)
    {
        _db = db;
        _settings = settings;
    }

    public ObservableCollection<ActiveExerciseVM> Exercises { get; } = new();

    [ObservableProperty] bool isActive;
    [ObservableProperty] string name = "";
    [ObservableProperty] DateTime startedAt;
    [ObservableProperty] string elapsedText = "0:00";
    [ObservableProperty] bool restActive;
    [ObservableProperty] string restText = "0:00";
    [ObservableProperty] double restProgress;
    [ObservableProperty] bool hasPr;
    [ObservableProperty] string prText = "";

    public async Task StartAsync(string workoutName, IEnumerable<Exercise> exercises)
    {
        Discard();
        Name = workoutName;
        StartedAt = DateTime.Now;
        IsActive = true;
        await AddExercisesAsync(exercises);
        Tick();
        EnsureTimer();
    }

    public async Task AddExercisesAsync(IEnumerable<Exercise> exercises)
    {
        foreach (var e in exercises)
        {
            var vm = new ActiveExerciseVM(e) { Best1RmBefore = await _db.GetBest1RmAsync(e.Id) };
            vm.RunningBest = vm.Best1RmBefore;
            var previous = await _db.GetPreviousSetsAsync(e.Id);
            if (previous.Count == 0)
            {
                for (var i = 1; i <= 3; i++) vm.Sets.Add(new ActiveSetVM(vm, _settings, i, "–", 0, 0));
            }
            else
            {
                for (var i = 0; i < previous.Count; i++)
                {
                    var p = previous[i];
                    var text = $"{_settings.FormatWeight(p.WeightKg)} × {p.Reps}";
                    vm.Sets.Add(new ActiveSetVM(vm, _settings, i + 1, text, p.WeightKg, p.Reps));
                }
            }
            Exercises.Add(vm);
        }
    }

    public void AddSet(ActiveExerciseVM ex)
    {
        var last = ex.Sets.LastOrDefault();
        ex.Sets.Add(new ActiveSetVM(ex, _settings, ex.Sets.Count + 1, "–", last?.WeightKg ?? 0, last?.Reps ?? 0));
    }

    public void RemoveLastSet(ActiveExerciseVM ex)
    {
        if (ex.Sets.Count > 0) ex.Sets.RemoveAt(ex.Sets.Count - 1);
        RecomputeBest(ex);
    }

    public void RemoveExercise(ActiveExerciseVM ex) => Exercises.Remove(ex);

    public void RefreshUnits()
    {
        foreach (var ex in Exercises)
            foreach (var s in ex.Sets)
                s.RefreshTexts();
    }

    public void ToggleSet(ActiveSetVM set)
    {
        if (!set.Done)
        {
            if (set.Reps <= 0) return;
            set.Done = true;
            var ex = set.Parent;
            var oneRm = Formulas.OneRm(set.WeightKg, set.Reps);
            if (ex.Best1RmBefore > 0 && oneRm > ex.RunningBest + 1e-6)
            {
                set.IsPr = true;
                PrText = $"{ex.Name} · {_settings.FormatWeightUnit(set.WeightKg)} × {set.Reps}";
                HasPr = true;
            }
            if (oneRm > ex.RunningBest) ex.RunningBest = oneRm;
            StartRest(_settings.RestSeconds);
        }
        else
        {
            set.Done = false;
            set.IsPr = false;
            RecomputeBest(set.Parent);
            HasPr = Exercises.Any(e => e.Sets.Any(s => s.IsPr));
        }
    }

    static void RecomputeBest(ActiveExerciseVM ex)
    {
        var best = ex.Best1RmBefore;
        foreach (var s in ex.Sets.Where(s => s.Done))
            best = Math.Max(best, Formulas.OneRm(s.WeightKg, s.Reps));
        ex.RunningBest = best;
    }

    // ---------- Dinlenme sayacı ----------

    public void StartRest(int seconds)
    {
        _restTotal = Math.Max(1, seconds);
        _restEnd = DateTime.Now.AddSeconds(_restTotal);
        RestActive = true;
        Tick();
        EnsureTimer();
    }

    public void AdjustRest(int deltaSeconds)
    {
        if (!RestActive) return;
        _restEnd = _restEnd.AddSeconds(deltaSeconds);
        _restTotal = Math.Max(1, _restTotal + deltaSeconds);
        Tick();
    }

    public void SkipRest() => RestActive = false;

    void EnsureTimer()
    {
        if (_timer != null) { _timer.Start(); return; }
        _timer = Application.Current?.Dispatcher.CreateTimer();
        if (_timer == null) return;
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (_, _) => Tick();
        _timer.Start();
    }

    void Tick()
    {
        if (IsActive) ElapsedText = SettingsService.FormatClock((int)(DateTime.Now - StartedAt).TotalSeconds);
        if (!RestActive) return;
        var remaining = (_restEnd - DateTime.Now).TotalSeconds;
        if (remaining <= 0)
        {
            RestActive = false;
            try { Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(400)); } catch { /* titreşim yok */ }
            return;
        }
        RestText = SettingsService.FormatClock((int)Math.Ceiling(remaining));
        RestProgress = Math.Clamp(remaining / _restTotal, 0, 1);
    }

    // ---------- Bitirme ----------

    public int CompletedSetCount => Exercises.Sum(e => e.Sets.Count(s => s.Done));

    /// <summary>Tamamlanan setleri kaydeder. Hiç set yoksa 0 döner ve hiçbir şey kaydetmez.</summary>
    public async Task<int> FinishAsync()
    {
        var details = new List<WorkoutExerciseDetail>();
        var position = 0;
        foreach (var ex in Exercises)
        {
            var done = ex.Sets.Where(s => s.Done).ToList();
            if (done.Count == 0) continue;
            var sets = done.Select((s, i) => new WorkoutSet
            {
                Number = i + 1,
                WeightKg = s.WeightKg,
                Reps = s.Reps,
                IsPr = s.IsPr
            }).ToList();
            details.Add(new WorkoutExerciseDetail(
                new WorkoutExercise { ExerciseId = ex.Exercise.Id, ExerciseName = ex.Name, Position = position++ }, sets));
        }
        if (details.Count == 0) return 0;

        var allSets = details.SelectMany(d => d.Sets).ToList();
        var workout = new Workout
        {
            Name = Name,
            StartedAt = StartedAt,
            DurationSeconds = (int)(DateTime.Now - StartedAt).TotalSeconds,
            VolumeKg = allSets.Sum(s => s.WeightKg * s.Reps),
            SetCount = allSets.Count
        };
        var id = await _db.SaveWorkoutAsync(workout, details);
        Discard();
        return id;
    }

    public void Discard()
    {
        _timer?.Stop();
        Exercises.Clear();
        IsActive = false;
        RestActive = false;
        HasPr = false;
        PrText = "";
        ElapsedText = "0:00";
    }
}
