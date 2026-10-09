namespace GearFit.Controls;

/// <summary>Haftalık hedef halkası (yalnızca halkayı çizer; metin üstüne Label ile konur).</summary>
public class RingView : GraphicsView
{
    public static readonly BindableProperty ProgressProperty =
        BindableProperty.Create(nameof(Progress), typeof(double), typeof(RingView), 0.0, propertyChanged: (b, _, _) => ((RingView)b).Invalidate());

    public RingView()
    {
        Drawable = new RingDrawable(this);
        WidthRequest = 56;
        HeightRequest = 56;
    }

    public double Progress { get => (double)GetValue(ProgressProperty); set => SetValue(ProgressProperty, value); }

    class RingDrawable : IDrawable
    {
        readonly RingView _owner;
        public RingDrawable(RingView owner) => _owner = owner;

        static Color Res(string key, Color fallback) =>
            Application.Current?.Resources.TryGetValue(key, out var v) == true && v is Color c ? c : fallback;

        public void Draw(ICanvas canvas, RectF rect)
        {
            const float stroke = 5;
            var cx = rect.Width / 2;
            var cy = rect.Height / 2;
            var r = Math.Min(rect.Width, rect.Height) / 2 - stroke / 2;

            canvas.StrokeSize = stroke;
            canvas.StrokeColor = Res("Surface3", Color.FromArgb("#332F2A"));
            canvas.DrawCircle(cx, cy, r);

            var progress = Math.Clamp(_owner.Progress, 0, 1);
            if (progress <= 0) return;

            canvas.StrokeColor = Res("Accent", Color.FromArgb("#FF7A3D"));
            canvas.StrokeLineCap = LineCap.Round;
            if (progress >= 0.999)
            {
                canvas.DrawCircle(cx, cy, r);
                return;
            }
            // 12 o'clock'tan saat yönünde yay
            var steps = Math.Max(2, (int)(64 * progress));
            var path = new PathF();
            for (var i = 0; i <= steps; i++)
            {
                var angle = -Math.PI / 2 + 2 * Math.PI * progress * i / steps;
                var x = cx + r * (float)Math.Cos(angle);
                var y = cy + r * (float)Math.Sin(angle);
                if (i == 0) path.MoveTo(x, y);
                else path.LineTo(x, y);
            }
            canvas.DrawPath(path);
        }
    }
}
