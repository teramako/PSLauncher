using System.Windows.Controls;

namespace PSLauncher.Views;

/// <summary>
/// StringResultView.xaml の相互作用ロジック
/// </summary>
public partial class StringResultView : UserControl, IResultContentControl
{
    public StringResultView()
    {
        InitializeComponent();
    }

    public void FocusToContent()
    {
        // Implementation for focusing to the content
        ResultText.Focus();
        ResultText.SelectAll();
    }
}
