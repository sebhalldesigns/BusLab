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


namespace BusLab.UI.Docking;

public enum TabType
{
    Document,
    Tool
}

[PseudoClasses(":selected", ":tool")]
public partial class Tab: UserControl
{

    private DockArea dockArea;

    public DockItem Item { get; }

    public TabType Type { get; private set; }

    public bool IsSelected { get => GetIsSelected(); set => SetIsSelected(value); }
    private bool isSelected = false;

    private bool isDragging = false;
    private Point dragStartPoint;
    private Control? dragAdorner;
    private AdornerLayer? adornerLayer;

    public TabGroup? TabGroup;

    public Tab(DockArea dockArea, DockItem item, TabGroup? tabGroup = null)
    {

        InitializeComponent();

        this.dockArea = dockArea;
        this.Item = item;
        this.TabGroup = tabGroup;
        this.Type = item.TabType;
        TextBlock.Text = item.Title;
        CloseButton.IsVisible = item.CanClose;
        item.TitleChanged += OnItemTitleChanged;

        if (item.TabType == TabType.Tool)
        {
            PseudoClasses.Set(":tool", true);
        }
        
        CloseButton.PointerPressed += (s, e) =>
        {
            Console.WriteLine("Close tab");
            dockArea.RemoveTab(this);
            e.Handled = true;
        };

        CloseButton.PointerEntered += (s, e) =>
        {
            CloseButton.Background = new SolidColorBrush(Color.FromUInt32(0x50808080u));
        };

        CloseButton.PointerExited += (s, e) =>
        {
            CloseButton.Background = Brushes.Transparent;
        };

        PointerEntered += (s, e) =>
        {
            CloseButton.Opacity = 1.0;
            CloseButton.IsHitTestVisible = true;
        };

        PointerExited += (s, e) =>
        {
            CloseButton.Opacity = 0.0;
            CloseButton.IsHitTestVisible = false;
        };
        
        // Use AddHandler with handledEventsToo = true
        Button.AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Bubble, handledEventsToo: true);
        Button.AddHandler(PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Bubble, handledEventsToo: true);
        Button.AddHandler(PointerMovedEvent, OnPointerMoved, RoutingStrategies.Bubble, handledEventsToo: true);
    }

    private void OnItemTitleChanged(DockItem item)
    {
        TextBlock.Text = item.Title;
    }

    public void ReleaseItemSubscriptions()
    {
        Item.TitleChanged -= OnItemTitleChanged;
    }
    
    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        isDragging = true;
        dragStartPoint = e.GetPosition(this);
        
        Console.WriteLine("Started dragging tab at " + dragStartPoint);
    }
    
    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!isDragging) return;
        
        isDragging = false;
        
        // Remove adorner
        if (dragAdorner != null && adornerLayer != null)
        {
            adornerLayer.Children.Remove(dragAdorner);
            dragAdorner = null;
        }
        
        // Make original tab visible again
        Opacity = 1.0;

        dockArea.TabDragEnded();

        Console.WriteLine("Stopped dragging tab.");

        Visual? parent = this.GetVisualParent();
        if (parent == null) return;

        Point position = e.GetPosition(parent);

        if (this.Bounds.Contains(position))
        {
            if (CloseButton.Bounds.Contains(e.GetPosition(contentDock)))
            {
                // Clicked close button
                Console.WriteLine("Close button clicked on tab.");
                dockArea.CloseItem(Item);
            }
            else
            {
                // Select tab
                dockArea.SelectTab(this);
                Console.WriteLine("Tab selected.");
            }
        }
        

    }
    
    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!isDragging) return;
        
        Point currentPoint = e.GetPosition(this);
        Vector delta = currentPoint - dragStartPoint;
        
        // Only start showing adorner if we've moved a bit (prevents accidental drags)
        if (Math.Abs(delta.X) < 5 && Math.Abs(delta.Y) < 5)
            return;
        
        // Get adorner layer on first significant move
        if (adornerLayer == null)
        {
            adornerLayer = AdornerLayer.GetAdornerLayer(this);
            if (adornerLayer == null)
            {
                Console.WriteLine("Warning: No AdornerLayer found");
                return;
            }
        }
        
        // Create adorner if needed
        if (dragAdorner == null)
        {
            dragAdorner = CreateDragAdorner();
            adornerLayer.Children.Add(dragAdorner);
            
            // Make original tab semi-transparent while dragging
            Opacity = 0.5;
        }
        
        // Get position relative to adorner layer
        Point adornerPosition = e.GetPosition(adornerLayer);
        
        // Position the adorner (offset by where user grabbed it)
        Canvas.SetLeft(dragAdorner, adornerPosition.X - dragStartPoint.X);
        Canvas.SetTop(dragAdorner, adornerPosition.Y - dragStartPoint.Y);

        dockArea.TabDragged(this, e);
        
    }
    
    private Control CreateDragAdorner()
    {
        // Create a visual copy of the tab
        var border = new Border
        {
            Width = Bounds.Width,
            Height = Bounds.Height,
            Background = new SolidColorBrush(Color.FromUInt32(0x30808080u)), // Dark background
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(4),
            BoxShadow = new BoxShadows(new BoxShadow
            {
                Blur = 10,
                Color = Colors.Black,
                OffsetX = 0,
                OffsetY = 4
            }),
            Opacity = 0.9
        };
        
        // Copy the text content
        var textBlock = new TextBlock
        {
            Text = Item.Title,
            TextAlignment = TextAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(5, 0, 0, 0)
        };
        
        border.Child = textBlock;
        
        return border;
    }

    private void SetIsSelected(bool value)
    {
        isSelected = value;

        PseudoClasses.Set(":selected", value);
    }

    private bool GetIsSelected()
    {
        return isSelected;
    }
}
