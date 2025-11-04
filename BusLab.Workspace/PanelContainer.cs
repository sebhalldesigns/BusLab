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
    public Dock.Model.Avalonia.Controls.Document Parent;
    public PanelBase? Panel { get; private set; }
     
    public PanelContainer(Dock.Model.Avalonia.Controls.Document parent)
    {
        InitializeComponent();

        this.Parent = parent;
        SetTabTitle("BusLab");

        this.Content = new WelcomePanel();
    }

    public void SetTabTitle(string title)
    {
        Parent.Title = title;
    }

    public void SetPanel(PanelBase panel)
    {
        Panel = panel;
        this.Content = panel;
        panel.TitleUpdate = SetTabTitle;
    }

    public async void OpenFilePressed()
    {
        
    }

    public async void SaveFilePressed()
    {
        
    }
}