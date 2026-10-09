using Microsoft.Maui.Controls.Shapes;
using Path = Microsoft.Maui.Controls.Shapes.Path;

namespace GearFit.Controls;

/// <summary>24x24 viewBox'lı çizgi ikonu. Renk tema kaynağına bağlanabilir.</summary>
public class Icon : ContentView
{
    const double ViewBox = 24;

    static readonly Dictionary<string, (string Data, bool Filled)> Glyphs = new()
    {
        ["plus"] = ("M12 5v14M5 12h14", false),
        ["minus"] = ("M5 12h14", false),
        ["back"] = ("M15 5l-7 7 7 7", false),
        ["chevron"] = ("M9 5l7 7-7 7", false),
        ["chevron-down"] = ("M6 9l6 6 6-6", false),
        ["close"] = ("M6 6l12 12M18 6L6 18", false),
        ["check"] = ("M5 12.5l4.5 4.5L19 7", false),
        ["play"] = ("M8 5.5v13l11-6.5z", true),
        ["dots"] = ("M3.2 12a1.8 1.8 0 1 0 3.6 0a1.8 1.8 0 1 0 -3.6 0M10.2 12a1.8 1.8 0 1 0 3.6 0a1.8 1.8 0 1 0 -3.6 0M17.2 12a1.8 1.8 0 1 0 3.6 0a1.8 1.8 0 1 0 -3.6 0", true),
        ["flame"] = ("M12 2.5c.8 3.2 5.5 5.2 5.5 10.5a5.5 5.5 0 0 1-11 0c0-2.2 1-3.6 2.2-4.8.1 1.8.9 2.8 2 3 .3-3.1-.4-5.4 1.3-8.7z", false),
        ["trophy"] = ("M8 21h8M12 17v4M7 4h10v5a5 5 0 0 1-10 0V4zM7 6H4v1a3 3 0 0 0 3 3M17 6h3v1a3 3 0 0 1-3 3", false),
        ["dumbbell"] = ("M6.5 6.5v11M17.5 6.5v11M3.5 9v6M20.5 9v6M6.5 12h11", false),
        ["clock"] = ("M3 12a9 9 0 1 0 18 0a9 9 0 1 0 -18 0M12 7v5l3 2", false),
        ["chart"] = ("M3 3v18h18M7 15l4-5 3 3 5-7", false),
        ["search"] = ("M4 11a7 7 0 1 0 14 0a7 7 0 1 0 -14 0M20 20l-4-4", false),
        ["edit"] = ("M4 20h4L19 9l-4-4L4 16v4zM13.5 6.5l4 4", false),
        ["trash"] = ("M4 7h16M10 11v6M14 11v6M6 7l1 12a2 2 0 0 0 2 2h6a2 2 0 0 0 2-2l1-12M9 7V4h6v3", false),
        ["repeat"] = ("M17 2l4 4-4 4M3 11v-1a4 4 0 0 1 4-4h14M7 22l-4-4 4-4M21 13v1a4 4 0 0 1-4 4H3", false),
        ["download"] = ("M12 4v11M7 11l5 5 5-5M5 20h14", false),
        ["arrow-up"] = ("M12 19V5M5 12l7-7 7 7", false),
        ["arrow-down"] = ("M12 5v14M5 12l7 7 7-7", false),
        ["gear"] = ("M9 12a3 3 0 1 0 6 0a3 3 0 1 0 -6 0M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1 0 2.83 2 2 0 0 1-2.83 0l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-2 2 2 2 0 0 1-2-2v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83 0 2 2 0 0 1 0-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1-2-2 2 2 0 0 1 2-2h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 0-2.83 2 2 0 0 1 2.83 0l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 2-2 2 2 0 0 1 2 2v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 0 2 2 0 0 1 0 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 2 2 2 2 0 0 1-2 2h-.09a1.65 1.65 0 0 0-1.51 1z", false),
    };

    public static readonly BindableProperty NameProperty =
        BindableProperty.Create(nameof(Name), typeof(string), typeof(Icon), "", propertyChanged: (b, _, _) => ((Icon)b).Update());

    public static readonly BindableProperty IconColorProperty =
        BindableProperty.Create(nameof(IconColor), typeof(Color), typeof(Icon), Colors.White, propertyChanged: (b, _, _) => ((Icon)b).Update());

    public static readonly BindableProperty SizeProperty =
        BindableProperty.Create(nameof(Size), typeof(double), typeof(Icon), 24.0, propertyChanged: (b, _, _) => ((Icon)b).Update());

    public static readonly BindableProperty ThicknessProperty =
        BindableProperty.Create(nameof(Thickness), typeof(double), typeof(Icon), 1.9, propertyChanged: (b, _, _) => ((Icon)b).Update());

    readonly Path _path = new()
    {
        Aspect = Stretch.None,
        StrokeLineCap = PenLineCap.Round,
        StrokeLineJoin = PenLineJoin.Round,
        WidthRequest = ViewBox,
        HeightRequest = ViewBox,
        HorizontalOptions = LayoutOptions.Center,
        VerticalOptions = LayoutOptions.Center,
    };

    public Icon()
    {
        Content = _path;
        InputTransparent = true;
        Update();
    }

    public string Name { get => (string)GetValue(NameProperty); set => SetValue(NameProperty, value); }
    public Color IconColor { get => (Color)GetValue(IconColorProperty); set => SetValue(IconColorProperty, value); }
    public double Size { get => (double)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }
    public double Thickness { get => (double)GetValue(ThicknessProperty); set => SetValue(ThicknessProperty, value); }

    void Update()
    {
        WidthRequest = Size;
        HeightRequest = Size;
        _path.Scale = Size / ViewBox;
        _path.StrokeThickness = Thickness;

        if (string.IsNullOrEmpty(Name) || !Glyphs.TryGetValue(Name, out var glyph))
        {
            _path.Data = new PathGeometry();
            return;
        }
        _path.Data = (Geometry?)new PathGeometryConverter().ConvertFromInvariantString(glyph.Data);
        var brush = new SolidColorBrush(IconColor);
        var none = new SolidColorBrush(Colors.Transparent);
        _path.Fill = glyph.Filled ? brush : none;
        _path.Stroke = glyph.Filled ? none : brush;
    }
}
