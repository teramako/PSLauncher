using PSLauncher.Models;
using System.Windows;
using System.Windows.Input;

namespace PSLauncher.Views;

public partial class ObjectsViewWindow : Window
{
    public ObjectsViewWindow()
    {
        InitializeComponent();
    }

    public ObjectsViewWindow(IResultList resultList) : this()
    {
        DataContext = resultList;
        if (!string.IsNullOrEmpty(resultList.Description))
            Title = $"Objects View - {resultList.Description}";
    }
}
