using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System;

using BusLab.Windows;

namespace BusLab.Workspace.Panels;

public partial class WelcomePanel: UserControl
{
    public WelcomePanel()
    {
        InitializeComponent();
    }

    public async void NewFilePressed(object? sender, RoutedEventArgs e)
    {
        MainWindow parentWindow = (MainWindow)TopLevel.GetTopLevel(this);
        await parentWindow.WorkspaceManager.NewFile();
    }

    public async void OpenFilePressed(object? sender, RoutedEventArgs e)
    {
        MainWindow parentWindow = (MainWindow)TopLevel.GetTopLevel(this);
        parentWindow.WorkspaceManager.OpenFile();
    }

    public async void OpenFolderPressed(object? sender, RoutedEventArgs e)
    {
        MainWindow parentWindow = (MainWindow)TopLevel.GetTopLevel(this);
        parentWindow.WorkspaceManager.OpenWorkspace();
    }

    public async void HelpPressed(object? sender, RoutedEventArgs e)
    {
        MainWindow parentWindow = (MainWindow)TopLevel.GetTopLevel(this);
        parentWindow.WorkspaceManager.OpenDocs();
    }
}