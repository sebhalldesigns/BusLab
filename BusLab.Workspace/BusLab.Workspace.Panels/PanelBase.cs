
using Avalonia.Controls;
using BusLab.Workspace;

namespace BusLab.Workspace.Panels;

public class PanelBase: UserControl
{
    public TitleUpdateDelegate? TitleUpdate { get; set; }
    public ExplorerEntry? ExplorerEntry { get; set; }
    
    public virtual void SaveFilePressed()
    {
        
    }

    public virtual void OpenFilePressed()
    {
        
    }

    public virtual void LoadFile(ExplorerEntry entry, string contents)
    {
        ExplorerEntry = entry;
    }
}