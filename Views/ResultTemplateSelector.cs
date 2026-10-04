using PSLauncher.Models;
using System.Windows;
using System.Windows.Controls;

namespace PSLauncher.Views;

public class ResultTemplateSelector : DataTemplateSelector
{
    public DataTemplate? StringTemplate { get; set; }
    public DataTemplate? ObjectTemplate { get; set; }
    public DataTemplate? FilesTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object? item, DependencyObject container)
    {
        if (item is not ResultList results)
        {
            return null;
        }
        return results.Kind switch
        {
            ResultDataKind.Null => null,
            ResultDataKind.Simple => StringTemplate,
            ResultDataKind.Files => FilesTemplate,
            _ => ObjectTemplate,
        };
    }
}
