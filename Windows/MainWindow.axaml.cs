using Avalonia;
using Avalonia.Styling;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Dock.Avalonia.Themes.Simple;
using Dock.Avalonia.Controls;
using Dock.Model.Avalonia;
using Dock.Model.Avalonia.Controls;
using Dock.Model.Core;
using Dock.Model.Controls;
using Dock.Settings;

namespace BusLab;

public partial class MainWindow : Window
{
    private DocumentDock documentDock;
    private DockControl dockControl;
    private Factory factory;

    public MainWindow()
    {
        InitializeComponent();

        documentDock = new DocumentDock();
        documentDock.CanCreateDocument = true;
        documentDock.IsCollapsable = false;

        factory = new Factory();
        dockControl = new DockControl();

        documentDock.DocumentFactory = () =>
        {
            int index = documentDock.VisibleDockables?.Count ?? 0;
            
            return new Document
            {
                Id = $"Doc{index + 1}",
                Title = $"Select Panel Type",
                Content = new BlankPanel()
            };
        };

        Document? startDocument = documentDock.DocumentFactory() as Document;
        
        if (startDocument != null)
        {
            documentDock.VisibleDockables = factory.CreateList<IDockable>(startDocument);
            documentDock.ActiveDockable = startDocument;
        }
        
        ProportionalDock mainLayout = new ProportionalDock
        {
            Orientation = Orientation.Horizontal,
            VisibleDockables = factory.CreateList<IDockable>(documentDock)
            ,
        };

        IRootDock root = factory.CreateRootDock();
        root.VisibleDockables = factory.CreateList<IDockable>(mainLayout);
        root.DefaultDockable = mainLayout;

        factory.InitLayout(root);
        dockControl.Factory = factory;
        dockControl.Layout = root;

        MainContent.Content = dockControl;
    }

    public void OpenPressed(object? sender, RoutedEventArgs e)
    {
        OpenWindow openWindow = new OpenWindow();
        openWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        openWindow.ShowDialog(this);
    }

    public void ToggleTheme(object? sender, RoutedEventArgs e)
    {
        if (Application.Current!.ActualThemeVariant == ThemeVariant.Light)
        {
            Application.Current!.RequestedThemeVariant = ThemeVariant.Dark;
        }
        else
        {
            Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
        }
    }
}

