using System.ComponentModel;
using GearFit.Controls;
using GearFit.Services;
using GearFit.ViewModels;

namespace GearFit.Pages;

public partial class ActivePage : ContentPage
{
    readonly ActiveViewModel _vm;

    public ActivePage(ActiveViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        _vm.Session.PropertyChanged += OnSessionChanged;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.Session.RefreshUnits();
        _ = Motion.PageInAsync(this);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
    }

    // Görünürlük bağlaması çalıştıktan sonra animasyonu başlat.
    void OnSessionChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ActiveWorkoutService.RestActive) && _vm.Session.RestActive)
            Dispatcher.Dispatch(() => _ = Motion.RiseAsync(RestPanel, 120));
        else if (e.PropertyName == nameof(ActiveWorkoutService.HasPr) && _vm.Session.HasPr)
            Dispatcher.Dispatch(() => _ = Motion.DropInAsync(PrBanner));
    }

    static T? ContextOf<T>(object? sender) where T : class => (sender as BindableObject)?.BindingContext as T;

    void OnSetTapped(object? sender, TappedEventArgs e)
    {
        if (ContextOf<ActiveSetVM>(sender) is { } set)
        {
            _vm.ToggleSetCommand.Execute(set);
            if (sender is VisualElement ve) _ = Motion.TapAsync(ve, 0.78);
        }
    }

    void OnAddSetClicked(object? sender, EventArgs e)
    {
        if (ContextOf<ActiveExerciseVM>(sender) is { } ex) _vm.AddSetCommand.Execute(ex);
    }

    void OnExerciseMenuTapped(object? sender, TappedEventArgs e)
    {
        if (ContextOf<ActiveExerciseVM>(sender) is { } ex) _vm.ExerciseMenuCommand.Execute(ex);
    }

    void OnEntryFocused(object? sender, FocusEventArgs e)
    {
        // Dokununca değeri komple seç; yeni sayıyı doğrudan yazabilsin.
        if (sender is Entry entry && !string.IsNullOrEmpty(entry.Text))
        {
            entry.CursorPosition = 0;
            entry.SelectionLength = entry.Text.Length;
        }
    }
}
