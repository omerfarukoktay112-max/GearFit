namespace GearFit.Localization;

/// <summary>XAML: Text="{loc:T anahtar}" — dil değişince otomatik güncellenir.</summary>
[ContentProperty(nameof(Key))]
public class TExtension : IMarkupExtension<BindingBase>
{
    public string Key { get; set; } = "";

    public BindingBase ProvideValue(IServiceProvider serviceProvider) =>
        new Binding($"[{Key}]", BindingMode.OneWay, source: LocalizationService.Instance);

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => ProvideValue(serviceProvider);
}
