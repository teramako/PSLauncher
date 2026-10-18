using PSLauncher.Models;
using System.Windows;
using System.Windows.Controls;

namespace PSLauncher.Views;

/// <summary>
/// ResultViewControl.xaml
/// </summary>
public partial class ResultViewControl : UserControl, IResultContentControl
{
    public ResultViewControl()
    {
        InitializeComponent();
    }

    public void FocusToContent()
    {
        if (DataContext is not ResultList data)
            return;

        if (data.Kind is ResultDataKind.Simple)
        {
            ResultText.Focus();
            ResultText.SelectAll();
            return;
        }

        // Implementation for focusing to the content
        if (DetailExpander.Visibility is not Visibility.Visible)
            return;

        if (!DetailExpander.IsExpanded)
        {
            DetailExpander.IsExpanded = true;
        }
        DetailDataGrid.FocusToContent();
    }
}
