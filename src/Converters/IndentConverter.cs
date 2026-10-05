using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PSLauncher.Converters;

/// <summary>
/// <seealso cref="IValueConverter"/> that converts the indentation level of <see cref="int"/> to that of <see cref="Thickness"/>.
/// </summary>
/// <remarks>
/// The indentation level is multiplied by 16 to convert it to pixels.
/// For example, an indentation level of 1 corresponds to a left margin of 16 pixels,
/// while an indentation level of 2 corresponds to a left margin of 32 pixels, and so on.
/// </remarks>
public class IndentConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        int level = (int)value;
        return new Thickness(level * 16, 0, 0, 0);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
