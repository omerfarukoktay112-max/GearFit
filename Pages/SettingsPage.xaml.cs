using GearFit.ViewModels;

namespace GearFit.Pages;

public partial class SettingsPage : ContentPage
{
    public SettingsPage(SettingsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = GearFit.Controls.Motion.PageInAsync(this);
    }
}
