using System.Globalization;
using System.Management.Automation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace PSLauncher.Converters;

/// <summary>
/// An implementation of `IMultiValueConverter` that returns the appropriate icon resource
/// based on the value of `CompletionResultType`.
/// </summary>
/// <remarks>
/// Used in WPF binding to display different icons for each type of CompletionResult.
/// ConvertBack is not implemented.
/// </remarks>
internal class CompletionResultToIconConverter : IMultiValueConverter
{
    public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values[0] is not CompletionResult cr)
            return null;
        if (values[1] is not FrameworkElement win)
            return null;

        return cr.ResultType switch
        {
            CompletionResultType.Text => win.TryFindResource("TextIcon"),
            CompletionResultType.History => win.TryFindResource("HistoryIcon"),
            CompletionResultType.Command => win.TryFindResource("CommandIcon"),
            CompletionResultType.ProviderItem => win.TryFindResource("ProviderItemIcon"),
            CompletionResultType.ProviderContainer => win.TryFindResource("ProviderContainerIcon"),
            CompletionResultType.Property => win.TryFindResource("PropertyIcon"),
            CompletionResultType.Method => win.TryFindResource("MethodIcon"),
            CompletionResultType.ParameterName => win.TryFindResource("ParameterNameIcon"),
            CompletionResultType.ParameterValue => win.TryFindResource("ParameterValueIcon"),
            CompletionResultType.Variable => win.TryFindResource("VariableIcon"),
            CompletionResultType.Namespace => win.TryFindResource("NamespaceIcon"),
            CompletionResultType.Type => win.TryFindResource("TypeIcon"),
            CompletionResultType.Keyword => win.TryFindResource("KeywordIcon"),
            CompletionResultType.DynamicKeyword => win.TryFindResource("DynamicKeywordIcon"),
            _ => null
        };
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
}
