using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System;

using BusLab.Windows;

namespace BusLab.Workspace.Panels;

public partial class TextPanel: PanelBase
{

    public TextPanel()
    {
        InitializeComponent();
    }

    public override void LoadFile(ExplorerEntry entry, string contents)
    {
        base.LoadFile(entry, contents);
        TextEditor.Text = contents;
    }

}