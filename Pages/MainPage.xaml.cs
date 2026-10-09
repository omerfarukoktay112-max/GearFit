using GearFit.Controls;
using GearFit.Models;
using GearFit.ViewModels;

namespace GearFit.Pages;

public partial class MainPage : ContentPage
{
    readonly MainViewModel _vm;

    public MainPage(MainViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _ = GearFit.Controls.Motion.PageInAsync(this);
        await _vm.LoadAsync();
    }

    void OnRoutineLongPressed(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is RoutineSummary routine)
            _vm.RoutineMenuCommand.Execute(routine);
    }

    void OnRoutineTapped(object? sender, TappedEventArgs e)
    {
        // Uzun basma menüsü açıldıysa aynı dokunuşu "başlat" sayma.
        if (LongPressBehavior.JustFired) return;
        if ((sender as BindableObject)?.BindingContext is RoutineSummary routine)
        {
            if (sender is VisualElement ve) _ = GearFit.Controls.Motion.TapAsync(ve, 0.97);
            _vm.StartRoutineCommand.Execute(routine);
        }
    }
}
