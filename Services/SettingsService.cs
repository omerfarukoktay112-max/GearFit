using System.Globalization;

namespace GearFit.Services;

public class SettingsService
{
    const double LbPerKg = 2.2046226218;
    /// <summary>Seçili dilin kültürü (tarih/sayı biçimi için). Adı eski, davranışı dile göre değişir.</summary>
    public static CultureInfo Tr => GearFit.Localization.LocalizationService.Instance.Culture;

    public bool OnboardingDone
    {
        get => Preferences.Get("onboarding_done", false);
        set => Preferences.Set("onboarding_done", value);
    }

    /// <summary>"male", "female" veya "other".</summary>
    public string Gender
    {
        get => Preferences.Get("gender", "other");
        set => Preferences.Set("gender", value);
    }

    public int HeightCm
    {
        get => Preferences.Get("height_cm", 175);
        set => Preferences.Set("height_cm", Math.Clamp(value, 100, 250));
    }

    public bool UseLb
    {
        get => Preferences.Get("unit_lb", false);
        set => Preferences.Set("unit_lb", value);
    }

    /// <summary>"dark", "light" veya "system".</summary>
    public string Theme
    {
        get => Preferences.Get("theme", "system");
        set => Preferences.Set("theme", value);
    }

    public int WeeklyGoal
    {
        get => Preferences.Get("weekly_goal", 4);
        set => Preferences.Set("weekly_goal", Math.Clamp(value, 1, 7));
    }

    public int RestSeconds
    {
        get => Preferences.Get("rest_seconds", 90);
        set => Preferences.Set("rest_seconds", Math.Clamp(value, 15, 600));
    }

    public string Unit => UseLb ? "lb" : "kg";

    public double ToDisplay(double kg) => UseLb ? kg * LbPerKg : kg;
    public double FromDisplay(double value) => UseLb ? value / LbPerKg : value;

    public string FormatNumber(double value) => Math.Round(value, 1).ToString("0.#", Tr);
    public string FormatWeight(double kg) => FormatNumber(ToDisplay(kg));
    public string FormatWeightUnit(double kg) => $"{FormatWeight(kg)} {Unit}";
    public string FormatVolume(double kg) => $"{Math.Round(ToDisplay(kg)).ToString("N0", Tr)} {Unit}";

    public static string FormatClock(int seconds)
    {
        seconds = Math.Max(0, seconds);
        var h = seconds / 3600;
        var m = seconds % 3600 / 60;
        var s = seconds % 60;
        return h > 0 ? $"{h}:{m:00}:{s:00}" : $"{m}:{s:00}";
    }

    /// <summary>"82,5" veya "82.5" gibi girdileri sayıya çevirir.</summary>
    public static bool TryParse(string? text, out double value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;
        text = text.Trim().Replace(',', '.');
        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && value >= 0;
    }
}
