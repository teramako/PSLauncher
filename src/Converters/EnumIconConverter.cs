using PSLauncher.Models;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Data;

namespace PSLauncher.Converters;

/// <summary>
/// An implementation of <seealso cref="IMultiValueConverter"/> and <seealso cref="IValueConverter"/>
/// that returns the appropriate icon resource from <seealso cref="IconAttribute"/> of Enum value
/// </summary>
/// <remarks>
/// Used in WPF binding to display different icons for each type of <seealso cref="IResultData"/>
/// ConvertBack is not implemented.
/// </remarks>
internal class EnumIconConverter : IMultiValueConverter, IValueConverter
{
    private static readonly Dictionary<Enum, string> cache = []; 

    public static string? GetResourceName(Enum value)
    {
        if (cache.TryGetValue(value, out var resourceName))
            return resourceName;
        var member = value.GetType().GetField(value.ToString());
        resourceName = member?.GetCustomAttribute<IconAttribute>()?.ResourceName;
        if (!string.IsNullOrEmpty(resourceName))
            cache[value] = resourceName;
        return resourceName;
    }

    public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values[0] is not Enum val)
            return null;

        if (values[1] is not FrameworkElement elm)
            return null;

        return elm.TryFindResource(GetResourceName(val));
    }

    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not Enum val)
            return null;

        return App.Current.TryFindResource(GetResourceName(val));
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Not implemented.
    /// </remarks>
    /// <exception cref="NotImplementedException"></exception>
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Not implemented.
    /// </remarks>
    /// <exception cref="NotImplementedException"></exception>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
