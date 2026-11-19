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

    public ObjectsViewWindow(IObjectList dataContext) : this()
    {
        DataContext = dataContext;
        if (!string.IsNullOrEmpty(dataContext.Description))
            Title = $"Objects View - {dataContext.Description}";
    }
}
