using Avalonia.Controls;

namespace BusLab.UI.Docking;

public class ToolDockItem : DockItem
{
    public ToolDockItem(string id, string title, ToolTabLocation preferredLocation, bool canClose = false)
        : base(id, title, TabType.Tool, canClose)
    {
        PreferredLocation = preferredLocation;
    }

    public ToolTabLocation PreferredLocation { get; }
}
