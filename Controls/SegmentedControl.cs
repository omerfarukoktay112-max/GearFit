namespace GearFit.Controls;

/// <summary>Tasarımdaki bölmeli seçici (Liste/Takvim, kg/lb, 1A/3A/1Y/Tümü ...).</summary>
public class SegmentedControl : Border
{
    public static readonly BindableProperty ItemsProperty =
        BindableProperty.Create(nameof(Items), typeof(string), typeof(SegmentedControl), "", propertyChanged: (b, _, _) => ((SegmentedControl)b).Rebuild());

    public static readonly BindableProperty SelectedIndexProperty =
        BindableProperty.Create(nameof(SelectedIndex), typeof(int), typeof(SegmentedControl), 0,
            BindingMode.TwoWay, propertyChanged: (b, _, _) => ((SegmentedControl)b).Restyle());

    public static readonly BindableProperty AccentSelectionProperty =
        BindableProperty.Create(nameof(AccentSelection), typeof(bool), typeof(SegmentedControl), true, propertyChanged: (b, _, _) => ((SegmentedControl)b).Restyle());

    public static readonly BindableProperty InnerHeightProperty =
        BindableProperty.Create(nameof(InnerHeight), typeof(double), typeof(SegmentedControl), 44.0, propertyChanged: (b, _, _) => ((SegmentedControl)b).Rebuild());

    readonly Grid _grid = new() { ColumnSpacing = 4 };
    readonly List<(Border Cell, Label Text)> _cells = new();

    public SegmentedControl()
    {
        StrokeThickness = 0;
        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 14 };
        Padding = 4;
        SetDynamicResource(BackgroundColorProperty, "Surface");
        Content = _grid;
    }

    /// <summary>Virgülle ayrılmış etiketler: "Liste,Takvim".</summary>
    public string Items { get => (string)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }
    public int SelectedIndex { get => (int)GetValue(SelectedIndexProperty); set => SetValue(SelectedIndexProperty, value); }
    public bool AccentSelection { get => (bool)GetValue(AccentSelectionProperty); set => SetValue(AccentSelectionProperty, value); }
    public double InnerHeight { get => (double)GetValue(InnerHeightProperty); set => SetValue(InnerHeightProperty, value); }

    public event EventHandler<int>? Selected;

    void Rebuild()
    {
        _grid.Children.Clear();
        _grid.ColumnDefinitions.Clear();
        _cells.Clear();
        var labels = (Items ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (var i = 0; i < labels.Length; i++)
        {
            var index = i;
            _grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            var text = new Label
            {
                Text = labels[i],
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                FontSize = 15,
            };
            var cell = new Border
            {
                StrokeThickness = 0,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
                HeightRequest = InnerHeight,
                Content = text,
            };
            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) =>
            {
                SelectedIndex = index;
                Selected?.Invoke(this, index);
            };
            cell.GestureRecognizers.Add(tap);
            SemanticProperties.SetDescription(cell, labels[i]);
            _grid.Add(cell, i);
            _cells.Add((cell, text));
        }
        Restyle();
    }

    void Restyle()
    {
        for (var i = 0; i < _cells.Count; i++)
        {
            var (cell, text) = _cells[i];
            var selected = i == SelectedIndex;
            if (selected)
            {
                cell.SetDynamicResource(BackgroundColorProperty, AccentSelection ? "Accent" : "Surface3");
                text.SetDynamicResource(Label.TextColorProperty, AccentSelection ? "OnAccent" : "Text");
                text.FontFamily = "BarlowSemi";
            }
            else
            {
                cell.BackgroundColor = Colors.Transparent;
                text.SetDynamicResource(Label.TextColorProperty, "Muted");
                text.FontFamily = "Barlow";
            }
        }
    }
}
