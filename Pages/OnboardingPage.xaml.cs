using System.ComponentModel;
using GearFit.Controls;
using GearFit.ViewModels;

namespace GearFit.Pages;

public partial class OnboardingPage : ContentPage
{
    readonly OnboardingViewModel _vm;
    int _lastStep;

    public OnboardingPage(OnboardingViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        _vm.PropertyChanged += OnVmChanged;
        _vm.Completed += () => ((App)Application.Current!).ShowMain();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await Motion.PageInAsync(this, 420);
        _ = Motion.SlideInAsync(Logo, -24, 500);
    }

    void OnOptionTapped(object? sender, TappedEventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not OptionItem item) return;
        if (sender is VisualElement ve) _ = Motion.TapAsync(ve, 0.97);
        if (_vm.Languages.Contains(item)) _vm.PickLanguageCommand.Execute(item);
        else _vm.PickGenderCommand.Execute(item);
    }

    async void OnVmChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(OnboardingViewModel.ThemeChoice))
        {
            // Onayla butonu, tema seçilince aşağıdan belirir.
            if (_vm.ThemeChoice >= 0) Dispatcher.Dispatch(() => _ = Motion.RiseAsync(NextButton, 40, 280));
            return;
        }
        if (e.PropertyName != nameof(OnboardingViewModel.Step)) return;
        var forward = _vm.Step > _lastStep;
        _lastStep = _vm.Step;

        View panel = _vm.Step switch { 0 => Panel0, 1 => Panel1, 2 => Panel2, _ => Panel3 };
        _ = Motion.SlideInAsync(panel, forward ? 48 : -48);

        // ilerleme noktaları: aktif olan uzar ve vurgu rengine geçer
        var dots = new[] { Dot0, Dot1, Dot2, Dot3 };
        for (var i = 0; i < dots.Length; i++)
        {
            var active = i <= _vm.Step;
            var target = i == _vm.Step ? 28.0 : 10.0;
            var dot = dots[i];
            dot.SetDynamicResource(BoxView.ColorProperty, active ? "Accent" : "Surface3");
            var from = dot.WidthRequest;
            new Animation(v => dot.WidthRequest = v, from, target, Easing.CubicOut).Commit(dot, "dot", 16, 260);
        }
        await Task.CompletedTask;
    }
}
