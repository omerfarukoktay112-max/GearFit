using System.ComponentModel;
using System.Globalization;

namespace GearFit.Localization;

/// <summary>Çalışma anında dil değiştirilebilen çeviri servisi. XAML'de {loc:T anahtar} ile bağlanır.</summary>
public class LocalizationService : INotifyPropertyChanged
{
    public static LocalizationService Instance { get; } = new();

    int _index;

    LocalizationService()
    {
        var saved = Preferences.Get("lang", "");
        var code = Array.IndexOf(Strings.Languages, saved) >= 0 ? saved : "en"; // ilk açılış her zaman İngilizce
        _index = Array.IndexOf(Strings.Languages, code);
        ApplyCulture();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Language => Strings.Languages[_index];
    public int LanguageIndex => _index;

    public string this[string key] => Get(key);

    public static string T(string key, params object[] args)
    {
        var text = Instance.Get(key);
        return args.Length == 0 ? text : string.Format(Instance.Culture, text, args);
    }

    public CultureInfo Culture { get; private set; } = CultureInfo.InvariantCulture;

    public void SetLanguage(int index)
    {
        index = Math.Clamp(index, 0, Strings.Languages.Length - 1);
        if (index == _index) return;
        _index = index;
        Preferences.Set("lang", Language);
        ApplyCulture();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Language)));
    }

    /// <summary>Kas grubu adını (DB'de Türkçe saklanır) geçerli dile çevirir.</summary>
    public string Group(string canonical) =>
        Strings.All.TryGetValue("g_" + canonical, out var v) ? v[_index] : canonical;

    string Get(string key) =>
        Strings.All.TryGetValue(key, out var v) ? v[_index] : key;

    void ApplyCulture()
    {
        Culture = new CultureInfo(Language switch { "tr" => "tr-TR", "de" => "de-DE", _ => "en-US" });
        CultureInfo.DefaultThreadCurrentCulture = Culture;
        CultureInfo.DefaultThreadCurrentUICulture = Culture;
        CultureInfo.CurrentCulture = Culture;
        CultureInfo.CurrentUICulture = Culture;
    }

    static string DetectSystemLanguage()
    {
        var two = CultureInfo.InstalledUICulture.TwoLetterISOLanguageName;
        return Array.IndexOf(Strings.Languages, two) >= 0 ? two : "en";
    }
}
