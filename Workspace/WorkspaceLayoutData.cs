using Dock.Model.Controls;
using Dock.Model.Avalonia;
using Dock.Model.Avalonia.Controls;
using Dock.Model.Core;
using System.Text;
using System.Collections.Generic;

namespace BusLab;

public class LayoutData
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public List<IDockableData> VisibleDockables { get; set; } = new();
}

public interface IDockableData { }

public class DockableData : IDockableData
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Title { get; set; }
    public bool IsVisible { get; set; } = true;
    public string? Orientation { get; set; }
    public List<IDockableData>? VisibleDockables { get; set; }
    public List<DocumentData>? Documents { get; set; }
    public bool? CanCreateDocument { get; set; }
}

public class DocumentData : IDockableData
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? ContentType { get; set; }
}

public class SplitterData : IDockableData
{
    public string Type { get; set; } = string.Empty;
    public double Proportion { get; set; }
}