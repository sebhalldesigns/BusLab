using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
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
            Id = "MsgWindow",
            Title = "Message Window",
            Content = new TextBox { Text = "Document 1", AcceptsReturn = true },
            CanClose = false,
            CanFloat = false
        };

        var document2 = new Document
        {
            Id = "TxWindow",
            Title = "Transmit Window",
            Content = null, //new TextBox { Text = "Document 2", AcceptsReturn = true },
            CanClose = false,
            CanFloat = false
        };

        var document3 = new Document
        {
            Id = "TxWindow2",
            Title = "Transmit Window 2",
            Content = null, //new TextBox { Text = "Document 3", AcceptsReturn = true },
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

}