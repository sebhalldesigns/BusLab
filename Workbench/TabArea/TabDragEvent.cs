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

using System;
using System.Collections.Generic;

namespace BusLab.Workbench;

public enum TabDragEventType
{
    NONE,
    SPLIT,
    DROP, 
    INSERT
}

public enum TabSplitDirection
{
    NONE,
    LEFT,
    RIGHT,
    UP,
    DOWN
}

public class TabDragEvent
{
    public TabDragEventType EventType { get; set; } = TabDragEventType.NONE;
    public TabSplitDirection SplitDirection { get; set; } = TabSplitDirection.NONE;
    public int InsertIndex { get; set; } = -1;
    public Tab? Tab { get; set; } = null;
    public TabGroup? TargetGroup { get; set; } = null;
}