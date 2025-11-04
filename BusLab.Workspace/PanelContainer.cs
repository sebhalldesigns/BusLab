using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System;

using BusLab.Workspace.Panels;

namespace BusLab.Workspace;

public delegate void TitleUpdateDelegate(string title);

public partial class PanelContainer: UserControl
{
    Dock.Model.Avalonia.Controls.Document parent;
     
    public PanelContainer(Dock.Model.Avalonia.Controls.Document parent)
    {
        InitializeComponent();

        this.parent = parent;
        SetTabTitle("BusLab");

        this.Content = new WelcomePanel();
    }

    public void SetTabTitle(string title)
    {
        parent.Title = title;
    }

    public async void OpenFilePressed()
    {
        
    }

    public async void SaveFilePressed()
    {
        
    }
}