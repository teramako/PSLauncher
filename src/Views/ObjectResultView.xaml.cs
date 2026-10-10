using System.Windows.Controls;

namespace PSLauncher.Views;

public partial class ObjectResultView : UserControl, IResultContentControl
{
    public ObjectResultView()
    {
        InitializeComponent();
    }

    public void FocusToContent()
    {
        // Implementation for focusing to the content
        if (!DetailExpander.IsExpanded)
        {
            DetailExpander.IsExpanded = true;
        }
        ResultGrid.FocusToContent();
    }
}
