using GearFit.Controls;
using GearFit.ViewModels;

namespace GearFit.Pages;

public partial class ProgressPage : ContentPage
{
    readonly ProgressViewModel _vm;

    public ProgressPage(ProgressViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        _vm.ChartChanged += points => Chart.SetData(points, fillArea: true);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _ = GearFit.Controls.Motion.PageInAsync(this);
        await _vm.LoadAsync();
    }

    void OnViewSelected(object? sender, int index)
    {
        if (index == 1) _vm.ShowBodyCommand.Execute(null);
    }
}
