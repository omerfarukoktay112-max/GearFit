using GearFit.ViewModels;

namespace GearFit.Pages;

public partial class DetailPage : ContentPage
{
    readonly DetailViewModel _vm;

    public DetailPage(DetailViewModel vm)
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
}
