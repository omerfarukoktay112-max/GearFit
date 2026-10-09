namespace GearFit.Controls;

/// <summary>Uygulama genelinde kullanılan küçük geçiş animasyonları.</summary>
public static class Motion
{
    /// <summary>Sayfa içeriğini hafifçe yukarı kayarak belirginleştirir.</summary>
    public static async Task PageInAsync(ContentPage page, uint ms = 260)
    {
        if (page.Content is not { } content) return;
        content.Opacity = 0;
        content.TranslationY = 16;
        await Task.WhenAll(
            content.FadeToAsync(1, ms, Easing.CubicOut),
            content.TranslateToAsync(0, 0, ms, Easing.CubicOut));
    }

    /// <summary>Dokunma geri bildirimi: kısa küçülüp yaylanarak geri döner.</summary>
    public static async Task TapAsync(VisualElement view, double to = 0.92)
    {
        await view.ScaleToAsync(to, 70, Easing.CubicOut);
        await view.ScaleToAsync(1, 160, Easing.SpringOut);
    }

    /// <summary>Soldan/sağdan kayarak girer.</summary>
    public static async Task SlideInAsync(VisualElement view, double fromX = 36, uint ms = 280)
    {
        view.Opacity = 0;
        view.TranslationX = fromX;
        await Task.WhenAll(
            view.FadeToAsync(1, ms, Easing.CubicOut),
            view.TranslateToAsync(0, 0, ms, Easing.CubicOut));
    }

    /// <summary>Alttan yukarı kayarak girer (dinlenme paneli, alt sayfa).</summary>
    public static async Task RiseAsync(VisualElement view, double fromY = 140, uint ms = 300)
    {
        view.TranslationY = fromY;
        view.Opacity = 0;
        await Task.WhenAll(
            view.FadeToAsync(1, ms, Easing.CubicOut),
            view.TranslateToAsync(0, 0, ms, Easing.CubicOut));
    }

    /// <summary>Üstten yumuşakça düşerek girer (PR bandı).</summary>
    public static async Task DropInAsync(VisualElement view, uint ms = 380)
    {
        view.TranslationY = -24;
        view.Scale = 0.96;
        view.Opacity = 0;
        await Task.WhenAll(
            view.FadeToAsync(1, ms, Easing.CubicOut),
            view.TranslateToAsync(0, 0, ms, Easing.SpringOut),
            view.ScaleToAsync(1, ms, Easing.SpringOut));
    }
}
