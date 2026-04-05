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

public partial class Properties : ToolDockItem
{

    public Properties() : base("buslab.ui.tools.propeties", "Properties", ToolTabLocation.Right)
    {
        InitializeComponent();
    }
}
