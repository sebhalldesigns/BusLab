using Avalonia.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Material.Icons.Avalonia;
using Material.Icons;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;

using BusLab.Windows;
using BusLab.Workspace;

namespace BusLab;

public partial class ExplorerControl: UserControl
{
    public ObservableCollection<ExplorerEntry> Entries { get; set; } = new ObservableCollection<ExplorerEntry>();

    private MainWindow mainWindow;

    public ExplorerControl(MainWindow mainWindow)
    {
        InitializeComponent();

        this.mainWindow = mainWindow;

        this.DataContext = this;
    }

    public void UpdateEntries(ObservableCollection<ExplorerEntry> newEntries)
    {
        Entries = newEntries;
        this.DataContext = null;
        this.DataContext = this;
    }

    public void SetWorkspaceTitle(string title)
    {
        WorkspaceTitle.Text = title;
    }

    private void NewPressed(object? sender, RoutedEventArgs e)
    {
        mainWindow.NewPressed(sender, e);
    }

    private void NewFolderPressed(object? sender, RoutedEventArgs e)
    {
        
    }

    private void OpenFolderPressed(object? sender, RoutedEventArgs e)
    {
        mainWindow.OpenFolderPressed(sender, e);
    }

    private void TreeViewSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0)
        {
            ExplorerEntry selectedEntry = e.AddedItems[0] as ExplorerEntry;
            mainWindow.WorkspaceManager.ExplorerEntrySelected(selectedEntry);
        }
    }

}