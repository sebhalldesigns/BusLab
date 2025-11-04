using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;

using BusLab.Workspace;

namespace BusLab.Windows;

public partial class NewPopupWindow : Window
{
    public WorkspaceItem? NewItem = null;
    
    public NewPopupWindow(Window owner)
    {
        InitializeComponent();

        Owner = owner;

        PixelPoint parentPos = owner.Position;
        Rect parentBounds = owner.Bounds;

        WindowStartupLocation = Avalonia.Controls.WindowStartupLocation.Manual;
        Topmost = true;

        this.Position = new Avalonia.PixelPoint(
            (int)(parentPos.X + (parentBounds.Width - this.Width) / 2),
            (int)(parentPos.Y + 20)
        );

        TypeListBox.ItemsSource = Enum.GetNames(typeof(WorkspaceItem));
    }

    private void CancelPressed(object? sender, RoutedEventArgs e)
    {
        this.Close();
    }
    
    private void TypeSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (TypeListBox.SelectedItem != null)
        {
            string selected = TypeListBox.SelectedItem.ToString();
            if (Enum.TryParse<WorkspaceItem>(selected, out WorkspaceItem item))
            {
                NewItem = item;
                this.Close();
            }
        }
    }
    
}