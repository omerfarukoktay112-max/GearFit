using System.Windows.Input;

namespace GearFit.Controls;

/// <summary>Uzun basma (Windows'ta sağ tık) ile komut çalıştırır.</summary>
public class LongPressBehavior : Behavior<View>
{
    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(LongPressBehavior));

    public static readonly BindableProperty CommandParameterProperty =
        BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(LongPressBehavior));

    /// <summary>Uzun basmanın tetiklendiği an; ardından gelen "Tapped" olayını yok saymak için.</summary>
    public static DateTime LastFired { get; private set; } = DateTime.MinValue;
    public static bool JustFired => (DateTime.UtcNow - LastFired).TotalMilliseconds < 700;

    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }
    public object? CommandParameter { get => GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty, value); }

    View? _view;
    object? _hooked;

    protected override void OnAttachedTo(View bindable)
    {
        base.OnAttachedTo(bindable);
        _view = bindable;
        bindable.HandlerChanged += OnHandlerChanged;
        Hook();
    }

    protected override void OnDetachingFrom(View bindable)
    {
        bindable.HandlerChanged -= OnHandlerChanged;
        base.OnDetachingFrom(bindable);
    }

    void OnHandlerChanged(object? sender, EventArgs e) => Hook();

    /// <summary>Uzun basma algılandığında; sender'ın BindingContext'i dokunulan öğedir.</summary>
    public event EventHandler? LongPressed;

    void Fire()
    {
        LastFired = DateTime.UtcNow;
        LongPressed?.Invoke(this, EventArgs.Empty);
        if (Command?.CanExecute(CommandParameter) == true) Command.Execute(CommandParameter);
    }

    void Hook()
    {
        var platformView = _view?.Handler?.PlatformView;
        if (platformView == null || ReferenceEquals(platformView, _hooked)) return;
        _hooked = platformView;
#if ANDROID
        if (platformView is Android.Views.View av)
            av.LongClick += (_, e) => { e.Handled = true; Fire(); };
#elif WINDOWS
        if (platformView is Microsoft.UI.Xaml.UIElement el)
        {
            el.Holding += (_, e) =>
            {
                if (e.HoldingState == Microsoft.UI.Input.HoldingState.Started) Fire();
            };
            el.RightTapped += (_, e) => { e.Handled = true; Fire(); };
        }
#endif
    }
}
