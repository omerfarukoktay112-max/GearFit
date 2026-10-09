using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GearFit.Localization;
using GearFit.Services;

namespace GearFit.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    static readonly string[] ThemeKeys = { "dark", "light", "system" };
    static readonly string[] GenderKeys = { "male", "female", "other" };

    readonly SettingsService _settings;
    readonly ThemeService _theme;
    readonly AppDb _db;

    public SettingsViewModel(SettingsService settings, ThemeService theme, AppDb db)
    {
        _settings = settings;
        _theme = theme;
        _db = db;
        unitIndex = settings.UseLb ? 1 : 0;
        themeIndex = Math.Max(0, Array.IndexOf(ThemeKeys, settings.Theme));
        weeklyGoal = settings.WeeklyGoal;
        restSeconds = settings.RestSeconds;
        languageIndex = LocalizationService.Instance.LanguageIndex;
        genderIndex = Math.Max(0, Array.IndexOf(GenderKeys, settings.Gender));
        heightCm = settings.HeightCm;
    }

    public string[] LanguageNames => Strings.LanguageNames;

    [ObservableProperty] int unitIndex;
    [ObservableProperty] int themeIndex;
    [ObservableProperty] int weeklyGoal;
    [ObservableProperty] int restSeconds;
    [ObservableProperty] int languageIndex;
    [ObservableProperty] int genderIndex;
    [ObservableProperty] int heightCm;

    public string RestText => SettingsService.FormatClock(RestSeconds);

    partial void OnUnitIndexChanged(int value) => _settings.UseLb = value == 1;

    partial void OnThemeIndexChanged(int value)
    {
        _settings.Theme = ThemeKeys[Math.Clamp(value, 0, ThemeKeys.Length - 1)];
        _theme.Apply();
    }

    partial void OnWeeklyGoalChanged(int value) => _settings.WeeklyGoal = value;

    partial void OnRestSecondsChanged(int value)
    {
        _settings.RestSeconds = value;
        OnPropertyChanged(nameof(RestText));
    }

    partial void OnLanguageIndexChanged(int value) => LocalizationService.Instance.SetLanguage(value);

    partial void OnGenderIndexChanged(int value) => _settings.Gender = GenderKeys[Math.Clamp(value, 0, 2)];

    partial void OnHeightCmChanged(int value) => _settings.HeightCm = value;

    [RelayCommand]
    void GoalDown() => WeeklyGoal = Math.Max(1, WeeklyGoal - 1);

    [RelayCommand]
    void GoalUp() => WeeklyGoal = Math.Min(7, WeeklyGoal + 1);

    [RelayCommand]
    void RestDown() => RestSeconds = Math.Max(15, RestSeconds - 15);

    [RelayCommand]
    void RestUp() => RestSeconds = Math.Min(600, RestSeconds + 15);

    [RelayCommand]
    void HeightDown() => HeightCm = Math.Max(100, HeightCm - 1);

    [RelayCommand]
    void HeightUp() => HeightCm = Math.Min(250, HeightCm + 1);

    [RelayCommand]
    void ShowWelcome() => ((App)Application.Current!).ShowOnboarding();

    [RelayCommand]
    Task Back() => Shell.Current.GoToAsync("..");

    [RelayCommand]
    async Task Export()
    {
        var csv = await _db.ExportCsvAsync();
        if (csv.Count(c => c == '\n') <= 1)
        {
            await Shell.Current.DisplayAlertAsync(Loc.T("export_none_title"), Loc.T("export_none_msg"), Loc.T("ok"));
            return;
        }
        var path = Path.Combine(FileSystem.CacheDirectory, "gearfit-workouts.csv");
        await File.WriteAllTextAsync(path, csv, new UTF8Encoding(true));
        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = "GearFit",
            File = new ShareFile(path, "text/csv")
        });
    }
}
