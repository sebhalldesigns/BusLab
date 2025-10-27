using Avalonia;
using Avalonia.Styling;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Dock.Avalonia.Themes.Simple;
using Dock.Avalonia.Controls;
using Dock.Model.Avalonia;
using Dock.Model.Avalonia.Controls;
using Dock.Model.Core;
using Dock.Settings;

namespace BusLab;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        DockControl dockControl = new DockControl();
        Factory factory = new Factory();

        var documentDock = new DocumentDock
        {
            Id = "Documents",
            IsCollapsable = false,
            CanCreateDocument = false,
            CanClose = false,
            CanFloat = false
        };

        var document = new Document
        {
            Title = "Empty Panel",
            Content = new BlankPanel(),
            CanClose = false,
            CanFloat = false
        };

        var document2 = new Document
        {
            Title = "Network Messages",
            Content = new NetworkMessagesPanel(),
            CanClose = false,
            CanFloat = false
        };

        var document3 = new Document
        {
            Title = "Signal Plot",
            Content = new SignalPlotPanel(), 
            CanClose = false,
            CanFloat = false
        };

        documentDock.VisibleDockables = factory.CreateList<IDockable>(document, document2, document3);
        documentDock.ActiveDockable = document;

        var mainLayout = new ProportionalDock
        {
            Orientation = Orientation.Horizontal,
            VisibleDockables = factory.CreateList<IDockable>(
                documentDock
            )
        };

        var root = factory.CreateRootDock();
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