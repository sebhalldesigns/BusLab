using Avalonia.Controls;

namespace BusLab.UI.Docking;

public class DocumentDockItem : DockItem
{
    public DocumentDockItem(string id, string title, bool canClose = true)
        : base(id, title, TabType.Document, canClose)
    {
    }
}
