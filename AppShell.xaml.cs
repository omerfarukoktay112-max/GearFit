using GearFit.Pages;

namespace GearFit;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute("active", typeof(ActivePage));
        Routing.RegisterRoute("library", typeof(LibraryPage));
        Routing.RegisterRoute("detail", typeof(DetailPage));
        Routing.RegisterRoute("routine-edit", typeof(RoutineEditPage));
        Routing.RegisterRoute("settings", typeof(SettingsPage));
    }
}
