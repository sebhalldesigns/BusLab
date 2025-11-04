using Avalonia;
using Avalonia.Styling;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
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

using BusLab.Workspace;

namespace BusLab.Windows;

public partial class MainWindow : Window
{   
    private DockControl dockControl;
    public TabFactory TabFactory { get; set; }

    private double previousLeftSidebarWidth = 300;
    private double previousRightSidebarWidth = 300;

    public WorkspaceManager WorkspaceManager { get; set; }
    public ExplorerControl ExplorerControl { get; set; }

    public MainWindow()
    {
        InitializeComponent();        


        dockControl = new DockControl();
        TabFactory = new TabFactory();

        IRootDock root = TabFactory.CreateLayout();
        TabFactory.InitLayout(root);

        dockControl.Factory = TabFactory;
        dockControl.Layout = root;
        
        MainContent.Content = dockControl;

        ExplorerControl = new ExplorerControl(this);
        LeftSidebarContent.Content = ExplorerControl;
        
        WorkspaceManager = new WorkspaceManager(this);
    }

    public void OpenPressed(object? sender, RoutedEventArgs e)
    {
        WorkspaceManager.OpenFile(); 
    }

    public async void OpenFolderPressed(object? sender, RoutedEventArgs e)
    {
        WorkspaceManager.OpenWorkspace();
    }

    public void NewPressed(object? sender, RoutedEventArgs e)
    {
        WorkspaceManager.NewFile();
    }

    public void SettingsPressed(object? sender, RoutedEventArgs e)
    {
        SettingsWindow settingsWindow = new SettingsWindow();
        settingsWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        settingsWindow.ShowDialog(this);
    }

    public void AboutPressed(object? sender, RoutedEventArgs e)
    {
        AboutWindow aboutWindow = new AboutWindow();
        aboutWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        aboutWindow.ShowDialog(this);
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
      
    }

    public void ToggleLeftSidebarPressed(object? sender, RoutedEventArgs e)
    {
        if (SidebarsGrid.ColumnDefinitions[0].Width.Value > 0)
        {
            previousLeftSidebarWidth = SidebarsGrid.ColumnDefinitions[0].Width.Value;
            SidebarsGrid.ColumnDefinitions[0].Width = new GridLength(0);
            SidebarsGrid.ColumnDefinitions[0].MinWidth = 0;
        }
        else
        {
            if (previousLeftSidebarWidth < 200)
                previousLeftSidebarWidth = 300;
                
            SidebarsGrid.ColumnDefinitions[0].Width = new GridLength(previousLeftSidebarWidth);
            SidebarsGrid.ColumnDefinitions[0].MinWidth = 200;
        }
    }

    public void ToggleRightSidebarPressed(object? sender, RoutedEventArgs e)
    {
        if (SidebarsGrid.ColumnDefinitions[4].Width.Value > 0)
        {
            previousRightSidebarWidth = SidebarsGrid.ColumnDefinitions[4].Width.Value;
            SidebarsGrid.ColumnDefinitions[4].Width = new GridLength(0);
            SidebarsGrid.ColumnDefinitions[4].MinWidth = 0;
        }
        else
        {
            if (previousRightSidebarWidth < 200)
                previousRightSidebarWidth = 300;

            SidebarsGrid.ColumnDefinitions[4].Width = new GridLength(previousRightSidebarWidth);
            SidebarsGrid.ColumnDefinitions[4].MinWidth = 200;
        }
    }


}



