using Dock.Model.Controls;
using Dock.Model.Avalonia;
using Dock.Model.Avalonia.Controls;
using Dock.Model.Core;
using System.Text;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System;

namespace BusLab;

public class LayoutData
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public List<DockableDataBase> VisibleDockables { get; set; } = new();
}

[JsonDerivedType(typeof(DockableData), typeDiscriminator: "dockable")]
[JsonDerivedType(typeof(SplitterData), typeDiscriminator: "splitter")]
public class DockableDataBase
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}

public class DockableData : DockableDataBase
{
    public string Id { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Orientation { get; set; }
    public List<DockableDataBase>? VisibleDockables { get; set; }
    public List<DocumentData>? Documents { get; set; }
    public bool? CanCreateDocument { get; set; }
}

public class SplitterData : DockableDataBase
{
    public double Proportion { get; set; }
}

public class DocumentData
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? Content { get; set; }
}