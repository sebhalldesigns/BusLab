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


using BusLab.UI.Tools;

namespace BusLab.UI.App;

public partial class MainWindow : Window
{   

    public MainWindow()
    {
        InitializeComponent();        
        DockArea.OpenTool(new Explorer());
        DockArea.OpenTool(new Devices());
        DockArea.OpenTool(new Symbols());
        DockArea.OpenTool(new Output());
        DockArea.OpenTool(new Problems());
        DockArea.OpenTool(new Properties());
        DockArea.OpenTool(new Assistant());
    }

}



