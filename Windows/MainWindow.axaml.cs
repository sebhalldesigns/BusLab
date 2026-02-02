using Avalonia;
using Avalonia.Styling;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using System.Text;


namespace BusLab;

public partial class MainWindow : Window
{   

    private double previousLeftSidebarWidth = 300;
    private double previousRightSidebarWidth = 300;

    public MainWindow()
    {
        InitializeComponent();        

      
    }

    public void OpenPressed(object? sender, RoutedEventArgs e)
    {
        
    }

    public void SettingsPressed(object? sender, RoutedEventArgs e)
    {
        
    }

    public async void OpenDatabasePressed(object? sender, RoutedEventArgs e)
    {
        
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
        
    }

    public void ToggleRightSidebarPressed(object? sender, RoutedEventArgs e)
    {
        
    }


}



