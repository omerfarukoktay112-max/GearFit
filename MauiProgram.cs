using System.Globalization;
using GearFit.Pages;
using GearFit.Services;
using GearFit.ViewModels;
using Microsoft.Extensions.Logging;

namespace GearFit;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        // Dil servisi kültürü kendi ayarlar (kayıtlı dil veya sistem dili).
        _ = GearFit.Localization.LocalizationService.Instance;

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("Barlow-Regular.ttf", "Barlow");
                fonts.AddFont("Barlow-SemiBold.ttf", "BarlowSemi");
                fonts.AddFont("BarlowCondensed-SemiBold.ttf", "BarlowCondSemi");
                fonts.AddFont("BarlowCondensed-Bold.ttf", "BarlowCond");
            });

        var s = builder.Services;
        s.AddSingleton<AppDb>();
        s.AddSingleton<SettingsService>();
        s.AddSingleton<ThemeService>();
        s.AddSingleton<SelectionService>();
        s.AddSingleton<ActiveWorkoutService>();
        s.AddSingleton<AppShell>();

        s.AddTransient<MainViewModel>();
        s.AddTransient<ActiveViewModel>();
        s.AddTransient<LibraryViewModel>();
        s.AddTransient<RoutineEditViewModel>();
        s.AddTransient<HistoryViewModel>();
        s.AddTransient<DetailViewModel>();
        s.AddTransient<ProgressViewModel>();
        s.AddTransient<BodyViewModel>();
        s.AddTransient<SettingsViewModel>();
        s.AddTransient<OnboardingViewModel>();
        s.AddTransient<OnboardingPage>();

        s.AddTransient<MainPage>();
        s.AddTransient<ActivePage>();
        s.AddTransient<LibraryPage>();
        s.AddTransient<RoutineEditPage>();
        s.AddTransient<HistoryPage>();
        s.AddTransient<DetailPage>();
        s.AddTransient<ProgressPage>();
        s.AddTransient<BodyPage>();
        s.AddTransient<SettingsPage>();

        // Girdi alanlarını çerçevesiz yap; çerçeveyi kartlar çizer.
        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("Borderless", (handler, view) =>
        {
#if ANDROID
            handler.PlatformView.BackgroundTintList =
                Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#elif WINDOWS
            var clear = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
            handler.PlatformView.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
            foreach (var key in new[]
            {
                "TextControlBackground", "TextControlBackgroundPointerOver", "TextControlBackgroundFocused",
                "TextControlBorderBrush", "TextControlBorderBrushPointerOver", "TextControlBorderBrushFocused"
            })
                handler.PlatformView.Resources[key] = clear;
            handler.PlatformView.Background = clear;
#endif
        });

#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}
