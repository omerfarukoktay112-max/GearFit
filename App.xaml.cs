using GearFit.Pages;
using GearFit.Services;

namespace GearFit;

public partial class App : Application
{
    readonly IServiceProvider _services;
    Window? _window;

    public App(IServiceProvider services)
    {
        InitializeComponent();
        _services = services;
        var theme = services.GetRequiredService<ThemeService>();
        var settings = services.GetRequiredService<SettingsService>();
        if (!settings.OnboardingDone) settings.Theme = "system"; // karşılama sistem temasıyla açılır
        theme.Apply();
        RequestedThemeChanged += (_, _) =>
        {
            if (services.GetRequiredService<SettingsService>().Theme == "system") theme.Apply();
        };
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var settings = _services.GetRequiredService<SettingsService>();
        Page first = settings.OnboardingDone
            ? _services.GetRequiredService<AppShell>()
            : _services.GetRequiredService<OnboardingPage>();
        _window = new Window(first);
#if WINDOWS
        // Masaüstünde telefon oranında aç.
        _window.Width = 430;
        _window.Height = 900;
#endif
        return _window;
    }

    /// <summary>Karşılama bittiğinde ana uygulamaya yumuşak geçiş yapar.</summary>
    public async void ShowMain()
    {
        if (_window == null) return;
        var old = _window.Page;
        if (old != null) await old.FadeToAsync(0, 220, Easing.CubicIn);

        // Seçilen tema, eski sayfa görünmezken uygulanır (ani renk değişimi olmasın).
        _services.GetRequiredService<ThemeService>().Apply();
        var shell = _services.GetRequiredService<AppShell>();
        shell.Opacity = 0;
        _window.Page = shell;
        await shell.FadeToAsync(1, 320, Easing.CubicOut);
    }

    /// <summary>Ayarlar'dan karşılama akışını yeniden başlatır.</summary>
    public async void ShowOnboarding()
    {
        if (_window == null) return;
        var settings = _services.GetRequiredService<SettingsService>();
        settings.OnboardingDone = false;
        var old = _window.Page;
        if (old != null) await old.FadeToAsync(0, 220, Easing.CubicIn);

        settings.Theme = "system";
        _services.GetRequiredService<ThemeService>().Apply();
        var page = _services.GetRequiredService<OnboardingPage>();
        page.Opacity = 0;
        _window.Page = page;
        await page.FadeToAsync(1, 320, Easing.CubicOut);
    }
}