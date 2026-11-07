using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PcPulse.Controls;

/// <summary>Binds a radio button to one enum value: <c>ConverterParameter=TopLeft</c>.</summary>
public sealed class EnumToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value?.ToString() == parameter?.ToString();

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true && parameter is string name ? Enum.Parse(targetType, name) : Binding.DoNothing;
}
