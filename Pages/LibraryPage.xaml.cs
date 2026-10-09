using GearFit.ViewModels;

namespace GearFit.Pages;

public partial class LibraryPage : ContentPage
{
    readonly LibraryViewModel _vm;

    public LibraryPage(LibraryViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(LibraryViewModel.IsSheetOpen) && _vm.IsSheetOpen)
                Dispatcher.Dispatch(() =>
                {
                    Scrim.Opacity = 0;
                    _ = Scrim.FadeToAsync(1, 220);
                    _ = GearFit.Controls.Motion.RiseAsync(Sheet, 420, 320);
                });
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _ = GearFit.Controls.Motion.PageInAsync(this);
        await _vm.LoadAsync();
    }

    void OnItemTapped(object? sender, TappedEventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is LibraryItem item) _vm.ToggleCommand.Execute(item);
    }

    void OnChipTapped(object? sender, TappedEventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is GroupChip chip) _vm.SelectGroupCommand.Execute(chip);
    }

    void OnNewChipTapped(object? sender, TappedEventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is GroupChip chip) _vm.SelectNewGroupCommand.Execute(chip);
    }
}
