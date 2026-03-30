using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace OnyxArchiver.UI.Converters;

/// <summary>
/// A specialized converter that toggles UI visibility based on whether an object is null.
/// Commonly used to hide action panels or detail views when no item is selected.
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    /// <summary>
    /// Checks for nullity and returns the appropriate WPF Visibility value.
    /// </summary>
    /// <param name="value">The object being checked for null.</param>
    /// <param name="targetType">The type of the binding target property.</param>
    /// <param name="parameter">An optional parameter (not used here).</param>
    /// <param name="culture">The culture to use in the converter.</param>
    /// <returns>Collapsed if the value is null; Visible if an instance exists.</returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Simple null-check to drive UI layout logic.
        return value == null ? Visibility.Collapsed : Visibility.Visible;
    }

    /// <summary>
    /// Backward conversion is not implemented as visibility cannot be converted back to a generic object.
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}