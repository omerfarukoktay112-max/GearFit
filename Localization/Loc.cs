namespace GearFit.Localization;

public static class Loc
{
    public static string Group(string canonical) => LocalizationService.Instance.Group(canonical);

    public static string T(string key, params object[] args) => LocalizationService.T(key, args);
}
