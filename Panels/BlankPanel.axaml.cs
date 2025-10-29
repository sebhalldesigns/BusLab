using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System;

namespace BusLab;

public enum PanelType
{
    Blank,
    NetworkMessages,
    NetworkTrace,
    SignalPlot,
    DatabaseFile
}

public partial class BlankPanel: UserControl
{
    Dock.Model.Avalonia.Controls.Document parent;
     
    public BlankPanel(Dock.Model.Avalonia.Controls.Document parent)
    {
        this.parent = parent;

        InitializeComponent();
    }

    private void NetworkMessagesPressed(object? sender, RoutedEventArgs e)
    {
        LoadPanel(PanelType.NetworkMessages);
    }

    private void NetworkTracePressed(object? sender, RoutedEventArgs e)
    {
        LoadPanel(PanelType.NetworkTrace);
    }

    private void SignalPlotPressed(object? sender, RoutedEventArgs e)
    {
        LoadPanel(PanelType.SignalPlot);
    }

    private void DatabaseFilePressed(object? sender, RoutedEventArgs e)
    {
        LoadPanel(PanelType.DatabaseFile);
    }
    
    public void LoadPanel(PanelType panel)
    {
        switch (panel)
        {
            case PanelType.NetworkMessages:
                PanelContent.Content = new NetworkMessagesPanel();
                SelectPanelContent.IsVisible = false;
                break;

            case PanelType.SignalPlot:
                PanelContent.Content = new SignalPlotPanel();
                SelectPanelContent.IsVisible = false;
                break;

            case PanelType.DatabaseFile:
                PanelContent.Content = new DatabaseEditPanel();
                SelectPanelContent.IsVisible = false;
                break;  

            default:
                break;
        }

        parent.Title = panel.ToString();

        Console.WriteLine("Loading panel: " + panel.ToString());
    }
}