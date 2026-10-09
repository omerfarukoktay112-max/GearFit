using GearFit.Controls;
using GearFit.ViewModels;

namespace GearFit.Pages;

public partial class BodyPage : ContentPage
{
    readonly BodyViewModel _vm;

    public BodyPage(BodyViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        _vm.ChartChanged += points => Chart.SetData(points, fillArea: false);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _ = GearFit.Controls.Motion.PageInAsync(this);
        await _vm.LoadAsync();
    }

    void OnViewSelected(object? sender, int index)
    {
        if (index == 0) _vm.ShowExercisesCommand.Execute(null);
    }
}
