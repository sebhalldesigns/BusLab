using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

using Material.Icons.Avalonia;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avalonia.Styling;
using Avalonia.Controls.Metadata;

using System;
using System.Collections.Generic;

using BusLab.UI.Docking;

namespace BusLab.UI.Tools;

public partial class Assistant : ToolDockItem
{

    public Assistant() : base("buslab.ui.tools.assistant", "Assistant", ToolTabLocation.Right)
    {
        InitializeComponent();
    }
}
