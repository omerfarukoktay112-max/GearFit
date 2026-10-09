using GearFit.Models;
using GearFit.ViewModels;

namespace GearFit.Pages;

public partial class RoutineEditPage : ContentPage
{
    readonly RoutineEditViewModel _vm;

    public RoutineEditPage(RoutineEditViewModel vm)
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

    void OnMoveUpTapped(object? sender, TappedEventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is Exercise ex) _vm.MoveUpCommand.Execute(ex);
    }

    void OnRemoveTapped(object? sender, TappedEventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is Exercise ex) _vm.RemoveCommand.Execute(ex);
    }
}
