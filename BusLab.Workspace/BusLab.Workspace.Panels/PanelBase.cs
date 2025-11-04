
using Avalonia.Controls;

namespace BusLab.Workspace.Panels;

public abstract class PanelBase: UserControl
{
    public abstract void SaveFilePressed(); 
    public abstract void OpenFilePressed();

    public abstract void LoadFileContents(string contents);

}