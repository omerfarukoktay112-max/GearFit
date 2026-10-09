using GearFit.Services;

namespace GearFit.Controls;

public record ChartPoint(DateTime Date, double Value);

/// <summary>Zaman eksenli çizgi grafiği (ilerleme ve vücut ağırlığı ekranları).</summary>
public class ChartView : GraphicsView
{
    readonly ChartDrawable _drawable = new();

    public ChartView()
    {
        Drawable = _drawable;
        HeightRequest = 186;
    }

    public void SetData(IReadOnlyList<ChartPoint> points, bool fillArea)
    {
        _drawable.Points = points;
        _drawable.FillArea = fillArea;
        Invalidate();
    }
}

class ChartDrawable : IDrawable
{
    public IReadOnlyList<ChartPoint> Points { get; set; } = Array.Empty<ChartPoint>();
    public bool FillArea { get; set; }

    static Color Res(string key, Color fallback) =>
        Application.Current?.Resources.TryGetValue(key, out var v) == true && v is Color c ? c : fallback;

    public void Draw(ICanvas canvas, RectF rect)
    {
        var line = Res("Line", Color.FromArgb("#2B2723"));
        var faint = Res("Faint", Color.FromArgb("#948C83"));
        var accent = Res("Accent", Color.FromArgb("#FF7A3D"));
        var bg = Res("Bg", Color.FromArgb("#121110"));

        const float left = 36, right = 24, top = 12, bottom = 36;
        var plotW = rect.Width - left - right;
        var plotH = rect.Height - top - bottom;
        if (plotW <= 0 || plotH <= 0 || Points.Count == 0) return;

        var min = Points.Min(p => p.Value);
        var max = Points.Max(p => p.Value);
        var (lo, hi, step) = NiceScale(min, max);

        float Y(double v) => top + plotH - (float)((v - lo) / (hi - lo) * plotH);

        canvas.FontColor = faint;
        canvas.FontSize = 12;
        canvas.StrokeSize = 1;
        for (var v = lo; v <= hi + step / 2; v += step)
        {
            var y = Y(v);
            canvas.StrokeColor = line;
            canvas.DrawLine(left, y, left + plotW, y);
            canvas.DrawString(v.ToString("0.#", SettingsService.Tr), 0, y - 8, left - 10, 16,
                HorizontalAlignment.Right, VerticalAlignment.Center);
        }

        var t0 = Points[0].Date;
        var t1 = Points[^1].Date;
        var span = (t1 - t0).TotalDays;
        float X(DateTime d) => span <= 0 ? left + plotW / 2 : left + (float)((d - t0).TotalDays / span * plotW);

        var pts = Points.Select(p => new PointF(X(p.Date), Y(p.Value))).ToList();

        if (pts.Count > 1)
        {
            if (FillArea)
            {
                var area = new PathF();
                area.MoveTo(pts[0].X, top + plotH);
                foreach (var p in pts) area.LineTo(p);
                area.LineTo(pts[^1].X, top + plotH);
                area.Close();
                canvas.FillColor = accent.WithAlpha(0.12f);
                canvas.FillPath(area);
            }
            var path = new PathF();
            path.MoveTo(pts[0]);
            foreach (var p in pts.Skip(1)) path.LineTo(p);
            canvas.StrokeColor = accent;
            canvas.StrokeSize = 2.5f;
            canvas.StrokeLineJoin = LineJoin.Round;
            canvas.StrokeLineCap = LineCap.Round;
            canvas.DrawPath(path);
        }

        // son nokta
        var last = pts[^1];
        canvas.FillColor = bg;
        canvas.FillCircle(last, 8);
        canvas.FillColor = accent;
        canvas.FillCircle(last, 5.5f);

        // x ekseni etiketleri
        canvas.FontColor = faint;
        canvas.FontSize = 12;
        var labelCount = span <= 0 ? 1 : 4;
        var lastRight = float.MinValue;
        for (var i = 0; i < labelCount; i++)
        {
            var d = labelCount == 1 ? t0 : t0.AddDays(span * i / (labelCount - 1));
            var x = X(d);
            var text = d.ToString(span < 45 ? "d MMM" : "MMM", SettingsService.Tr);
            if (x - 24 < lastRight) continue;
            var align = i == 0 && labelCount > 1 ? HorizontalAlignment.Left : HorizontalAlignment.Center;
            var boxX = align == HorizontalAlignment.Left ? left : x - 30;
            canvas.DrawString(text, boxX, top + plotH + 10, 60, 16, align, VerticalAlignment.Center);
            lastRight = x + 24;
        }
    }

    static (double Lo, double Hi, double Step) NiceScale(double min, double max)
    {
        if (max - min < 1e-6)
        {
            min -= 2;
            max += 2;
        }
        var raw = (max - min) / 4;
        var pow = Math.Pow(10, Math.Floor(Math.Log10(raw)));
        var frac = raw / pow;
        var nice = frac <= 1 ? 1 : frac <= 2 ? 2 : frac <= 2.5 ? 2.5 : frac <= 5 ? 5 : 10;
        var step = nice * pow;
        var lo = Math.Floor(min / step) * step;
        var hi = Math.Ceiling(max / step) * step;
        if (hi - lo < step) hi = lo + step;
        return (lo, hi, step);
    }
}
