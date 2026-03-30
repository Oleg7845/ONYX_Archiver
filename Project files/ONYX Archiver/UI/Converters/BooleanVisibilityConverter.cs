using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace OnyxArchiver.UI.Converters;

/// <summary>
/// Converts boolean or null values to WPF Visibility enumeration.
/// Useful for toggling UI elements based on flags or object presence.
/// </summary>
public class BooleanVisibilityConverter : IValueConverter
{
    /// <summary>
    /// Transforms a bool/object value into Visibility.
    /// </summary>
    /// <param name="value">The source value (expected bool or object).</param>
    /// <param name="targetType">The type of the binding target property.</param>
    /// <param name="parameter">An optional parameter to use (not used here).</param>
    /// <param name="culture">The culture to use in the converter.</param>
    /// <returns>Visible if true/not-null; Collapsed otherwise.</returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // If the value is a boolean, return Visible for true and Collapsed for false
        if (value is bool b)
        {
            return b ? Visibility.Visible : Visibility.Collapsed;
        }

        // For non-boolean objects, treat null as Collapsed and any instance as Visible
        return value == null ? Visibility.Collapsed : Visibility.Visible;
    }

    /// <summary>
    /// Backward conversion is not required for standard visibility toggling.
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}