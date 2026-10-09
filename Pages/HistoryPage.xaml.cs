using GearFit.ViewModels;

namespace GearFit.Pages;

public partial class HistoryPage : ContentPage
{
    readonly HistoryViewModel _vm;

    public HistoryPage(HistoryViewModel vm)
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

    void OnRowTapped(object? sender, TappedEventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is WorkoutRow row) _vm.OpenCommand.Execute(row);
    }

    void OnCellTapped(object? sender, TappedEventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is CalendarCell cell) _vm.OpenCellCommand.Execute(cell);
    }
}
