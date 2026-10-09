using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GearFit.Localization;
using GearFit.Services;

namespace GearFit.ViewModels;

public record OptionItem(int Index, string Label, bool Selected);

public partial class OnboardingViewModel : ObservableObject
{
    static readonly string[] GenderKeys = { "male", "female", "other" };
    static readonly string[] GenderLabelKeys = { "gender_male", "gender_female", "gender_other" };

    readonly SettingsService _settings;
    readonly AppDb _db;

    public OnboardingViewModel(SettingsService settings, AppDb db)
    {
        _settings = settings;
        _db = db;
        languageIndex = LocalizationService.Instance.LanguageIndex;
        LocalizationService.Instance.PropertyChanged += (_, _) => RebuildOptions();
        RebuildOptions();
    }

    public const int StepCount = 4;
    const int ThemeStep = 3;

    public ObservableCollection<OptionItem> Languages { get; } = new();
    public ObservableCollection<OptionItem> Genders { get; } = new();

    [ObservableProperty] int step;
    [ObservableProperty] int languageIndex;
    [ObservableProperty] int genderIndex = -1;
    [ObservableProperty] string heightText = "175";
    [ObservableProperty] string weightText = "";
    [ObservableProperty] int unitIndex;

    /// <summary>-1: seçilmedi, 0: açık, 1: koyu.</summary>
    [ObservableProperty] int themeChoice = -1;

    public bool IsStep0 => Step == 0;
    public bool IsStep1 => Step == 1;
    public bool IsStep2 => Step == 2;
    public bool IsStep3 => Step == ThemeStep;
    public bool CanGoBack => Step > 0;
    public bool CanNext => Step != 1 || GenderIndex >= 0;

    /// <summary>Tema adımında buton, bir tema seçilene kadar gizli kalır.</summary>
    public bool ShowNext => Step != ThemeStep || ThemeChoice >= 0;
    public bool LightSelected => ThemeChoice == 0;
    public bool DarkSelected => ThemeChoice == 1;

    public string UnitLabel => UnitIndex == 1 ? "lb" : "kg";
    partial void OnUnitIndexChanged(int value) => OnPropertyChanged(nameof(UnitLabel));

    public string NextText => Loc.T(Step == ThemeStep ? "ob_confirm" : "ob_next");

    /// <summary>Tema onaylanınca sayfa bunu dinler ve ana kabuğa geçer.</summary>
    public event Action? Completed;

    partial void OnStepChanged(int value)
    {
        OnPropertyChanged(nameof(IsStep0));
        OnPropertyChanged(nameof(IsStep1));
        OnPropertyChanged(nameof(IsStep2));
        OnPropertyChanged(nameof(IsStep3));
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(CanNext));
        OnPropertyChanged(nameof(ShowNext));
        OnPropertyChanged(nameof(NextText));
    }

    partial void OnThemeChoiceChanged(int value)
    {
        OnPropertyChanged(nameof(ShowNext));
        OnPropertyChanged(nameof(LightSelected));
        OnPropertyChanged(nameof(DarkSelected));
    }

    partial void OnLanguageIndexChanged(int value) => LocalizationService.Instance.SetLanguage(value);

    partial void OnGenderIndexChanged(int value)
    {
        OnPropertyChanged(nameof(CanNext));
        RebuildOptions();
    }

    void RebuildOptions()
    {
        Languages.Clear();
        for (var i = 0; i < Strings.LanguageNames.Length; i++)
            Languages.Add(new OptionItem(i, Strings.LanguageNames[i], i == LanguageIndex));
        Genders.Clear();
        for (var i = 0; i < GenderKeys.Length; i++)
            Genders.Add(new OptionItem(i, Loc.T(GenderLabelKeys[i]), i == GenderIndex));
        OnPropertyChanged(nameof(NextText));
    }

    [RelayCommand]
    void PickLanguage(OptionItem item)
    {
        LanguageIndex = item.Index;
        RebuildOptions();
    }

    [RelayCommand]
    void PickGender(OptionItem item) => GenderIndex = item.Index;

    [RelayCommand]
    void PickTheme(string index) => ThemeChoice = int.Parse(index);

    [RelayCommand]
    void Back()
    {
        if (Step > 0) Step--;
    }

    [RelayCommand]
    async Task Next()
    {
        if (!CanNext || !ShowNext) return;

        // Boy/kilo adımından çıkarken değerleri doğrula ve kaydet.
        if (Step == 2 && !await SaveBodyAsync()) return;

        if (Step < ThemeStep)
        {
            Step++;
            return;
        }

        _settings.Theme = ThemeChoice == 0 ? "light" : "dark";
        _settings.OnboardingDone = true;
        Completed?.Invoke();
    }

    async Task<bool> SaveBodyAsync()
    {
        var okHeight = SettingsService.TryParse(HeightText, out var height) && height is >= 100 and <= 250;
        var okWeight = SettingsService.TryParse(WeightText, out var weight) && weight > 0;
        var previousUnit = _settings.UseLb;
        _settings.UseLb = UnitIndex == 1;
        var weightKg = _settings.FromDisplay(weight);
        if (!okHeight || !okWeight || weightKg is < 20 or > 400)
        {
            _settings.UseLb = previousUnit;
            await Application.Current!.Windows[0].Page!.DisplayAlertAsync(Loc.T("invalid_value"), Loc.T("ob_invalid"), Loc.T("ok"));
            return false;
        }

        _settings.Gender = GenderKeys[Math.Clamp(GenderIndex, 0, 2)];
        _settings.HeightCm = (int)Math.Round(height);
        await _db.SaveBodyWeightAsync(weightKg);
        return true;
    }
}
