using GearFit.Models;

namespace GearFit.Services;

/// <summary>Egzersiz kütüphanesi ile onu açan sayfa arasındaki seçim köprüsü.</summary>
public class SelectionService
{
    /// <summary>Kütüphane "Ekle" dediğinde seçilen egzersizlerle çağrılır.</summary>
    public Func<IReadOnlyList<Exercise>, Task>? OnPicked { get; set; }

    /// <summary>Özel egzersiz oluşturulunca kütüphane bunu otomatik seçer.</summary>
    public Exercise? PendingNew { get; set; }

    /// <summary>Zaten eklenmiş egzersizler; kütüphanede seçili gelmez ama bilgi olarak kullanılır.</summary>
    public HashSet<int> Exclude { get; } = new();
}
