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
        
        if (File.Exists("layout.json") && false)
        {
            string json = File.ReadAllText("layout.json");
            root = tabFactory.LoadRootFromJson(json);

            Console.WriteLine("Layout loaded from file.");
        }

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

    public async void OpenDatabasePressed(object? sender, RoutedEventArgs e)
    {

        if (tabFactory.activePanel != null && tabFactory.activePanel is BlankPanel blankPanel)
        {
            switch (tabFactory.activePanel.ActivePanel)
            {
                case DatabaseEditPanel dbPanel:
                    IReadOnlyList<IStorageFile> files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                    {
                        Title = "Open CAN Database File",
                        AllowMultiple = false,
                        FileTypeFilter = new List<FilePickerFileType>
                        {
                            new FilePickerFileType("CAN Database Files")
                            {
                                Patterns = new[] { "*.dbc" }
                            }
                        }
                    });

                    if (files.Count == 1)
                    {
                        string filePath = files[0].Path.LocalPath;
                        dbPanel.LoadDatabase(filePath);
                    }

                    break;
            }
        }

        

        
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
        string workspaceContent = tabFactory.GetJsonLayout();
        Console.WriteLine(workspaceContent);
        //File.WriteAllText("layout.json", workspaceContent);
    }

}



