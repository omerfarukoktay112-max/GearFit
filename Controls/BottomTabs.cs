using GearFit.Localization;

namespace GearFit.Controls;

/// <summary>Alt sekme çubuğu: Antrenman · Geçmiş · İlerleme.</summary>
public class BottomTabs : Border
{
    public static readonly BindableProperty ActiveProperty =
        BindableProperty.Create(nameof(Active), typeof(int), typeof(BottomTabs), 0, propertyChanged: (b, _, _) => ((BottomTabs)b).Restyle());

    static readonly (string LabelKey, string Icon, string Route)[] Tabs =
    {
        ("tab_workout", "dumbbell", "//main"),
        ("tab_history", "clock", "//history"),
        ("tab_progress", "chart", "//progress"),
    };

    readonly List<(Icon Icon, Label Label)> _items = new();

    public BottomTabs()
    {
        StrokeThickness = 0;
        Padding = new Thickness(8, 0, 8, 0);
        SetDynamicResource(BackgroundColorProperty, "Surface");

        var grid = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star), new(GridLength.Star) }, HeightRequest = 64 };
        for (var i = 0; i < Tabs.Length; i++)
        {
            var (labelKey, iconName, route) = Tabs[i];
            var icon = new Icon { Name = iconName, Size = 24, HorizontalOptions = LayoutOptions.Center };
            var text = new Label { FontSize = 12, FontFamily = "BarlowSemi", HorizontalOptions = LayoutOptions.Center };
            text.SetBinding(Label.TextProperty, new Binding($"[{labelKey}]", BindingMode.OneWay, source: LocalizationService.Instance));
            var stack = new VerticalStackLayout { Spacing = 3, VerticalOptions = LayoutOptions.Center, Children = { icon, text } };
            var tap = new TapGestureRecognizer();
            tap.Tapped += async (_, _) =>
            {
                if (Active == GetIndex(route)) return;
                _ = icon.ScaleToAsync(0.8, 70).ContinueWith(_ => MainThread.BeginInvokeOnMainThread(() => _ = icon.ScaleToAsync(1, 140, Easing.SpringOut)));
                await Shell.Current.GoToAsync(route);
            };
            stack.GestureRecognizers.Add(tap);
            grid.Add(stack, i);
            _items.Add((icon, text));
        }
        Content = grid;
        Restyle();
    }

    public int Active { get => (int)GetValue(ActiveProperty); set => SetValue(ActiveProperty, value); }

    static int GetIndex(string route) => Array.FindIndex(Tabs, t => t.Route == route);

    void Restyle()
    {
        for (var i = 0; i < _items.Count; i++)
        {
            var key = i == Active ? "Accent" : "Muted";
            _items[i].Icon.SetDynamicResource(Icon.IconColorProperty, key);
            _items[i].Label.SetDynamicResource(Label.TextColorProperty, key);
        }
    }
}
