namespace GearFit.Services;

/// <summary>Renk kaynaklarını çalışma anında değiştirir; sayfalar DynamicResource ile bağlıdır.</summary>
public class ThemeService
{
    static readonly Dictionary<string, string> Dark = new()
    {
        ["Bg"] = "#121110", ["Surface"] = "#1C1A18", ["Surface2"] = "#262320", ["Surface3"] = "#332F2A",
        ["Line"] = "#2B2723", ["Text"] = "#F3EFEA", ["Muted"] = "#A39B92", ["Faint"] = "#948C83",
        ["Accent"] = "#FF7A3D", ["OnAccent"] = "#14120F", ["AccentDim"] = "#3A2216", ["AccentRow"] = "#2A1D15",
        ["AccentLine"] = "#7A3D1E", ["Dashed"] = "#4A443D", ["Scrim"] = "#99000000",
    };

    static readonly Dictionary<string, string> Light = new()
    {
        ["Bg"] = "#F6F2EC", ["Surface"] = "#FFFFFF", ["Surface2"] = "#EEE9E2", ["Surface3"] = "#E2DCD3",
        ["Line"] = "#E4DED5", ["Text"] = "#1B1815", ["Muted"] = "#6B635A", ["Faint"] = "#7A7167",
        ["Accent"] = "#D4521A", ["OnAccent"] = "#FFFFFF", ["AccentDim"] = "#FBE3D6", ["AccentRow"] = "#FDF0E8",
        ["AccentLine"] = "#F0B999", ["Dashed"] = "#CFC7BC", ["Scrim"] = "#66000000",
    };

    readonly SettingsService _settings;

    public ThemeService(SettingsService settings)
    {
        _settings = settings;
    }

    public bool IsDark { get; private set; } = true;

    public void Apply()
    {
        var app = Application.Current;
        if (app == null) return;

        app.UserAppTheme = _settings.Theme switch
        {
            "dark" => AppTheme.Dark,
            "light" => AppTheme.Light,
            _ => AppTheme.Unspecified
        };
        IsDark = app.RequestedTheme != AppTheme.Light;

        var palette = IsDark ? Dark : Light;
        foreach (var (key, hex) in palette)
        {
            var color = Color.FromArgb(hex);
            app.Resources[key] = color;
            app.Resources[key + "Brush"] = new SolidColorBrush(color);
        }
    }
}
