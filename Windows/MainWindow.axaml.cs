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
using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using System.Text;


namespace BusLab;

public partial class MainWindow : Window
{   
    private DockControl dockControl;
    private TabFactory tabFactory;

    public MainWindow()
    {
        InitializeComponent();        

        dockControl = new DockControl();
        tabFactory = new TabFactory();

        IRootDock root = tabFactory.CreateLayout();
        tabFactory.InitLayout(root);
        dockControl.Factory = tabFactory;
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

    public void SaveWorkspacePressed(object? sender, RoutedEventArgs e)
    {
        string workspaceContent = tabFactory.GetJsonLayout();
        Console.WriteLine(workspaceContent);
    }

}



