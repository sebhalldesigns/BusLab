using System;

using Avalonia.Controls;

namespace BusLab.UI.Docking;

public abstract class DockItem : UserControl
{
    private string title;

    protected DockItem(string id, string title, TabType tabType, bool canClose)
    {
        Id = id;
        this.title = title;
        TabType = tabType;
        CanClose = canClose;
    }

    public string Id { get; }

    public string Title
    {
        get => title;
        set
        {
            if (title == value)
            {
                return;
            }

            title = value;
            TitleChanged?.Invoke(this);
        }
    }
    public TabType TabType { get; }

    public bool CanClose { get; }

    public event Action<DockItem>? TitleChanged;
}
