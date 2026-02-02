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

using System;
using System.Collections.Generic;


namespace BusLab.Workbench;

public class Tab: UserControl
{

    private TabArea tabArea;

    public bool IsSelected { get => GetIsSelected(); set => SetIsSelected(value); }
    private bool isSelected = false;

    private DockPanel contentDock;
    private Grid labelGrid;

    public Button Button;
    public Button CloseButton;
    public TextBlock TextBlock;

    private bool isDragging = false;
    private Point dragStartPoint;
    private Control? dragAdorner;
    private AdornerLayer? adornerLayer;

    public TabGroup? TabGroup;

    private IBrush highlightBrush = new SolidColorBrush(0x58808080);

    public Tab(TabArea tabArea, TabGroup? tabGroup = null)
    {
        this.tabArea = tabArea;
        this.TabGroup = tabGroup;
        

        /* create a new button on top of label */
        
        Button = new Button();
        Content = Button;
        Button.HorizontalContentAlignment = HorizontalAlignment.Stretch;
        Button.Padding = new Thickness(0);
        Button.CornerRadius = new CornerRadius(5);
        Button.Background = Avalonia.Media.Brushes.Transparent;
        Button.BorderThickness = new Thickness(0);
        
        contentDock = new DockPanel();
        Button.Content = contentDock;
        labelGrid = new Grid();
        CloseButton = new Button();
        CloseButton.Width = 17;
        CloseButton.Height = 17;
        CloseButton.Background = Brushes.Transparent;
        CloseButton.Margin = new Thickness(0, 0, 3, 0);
        CloseButton.CornerRadius = new CornerRadius(4);
        CloseButton.BorderThickness = new Thickness(0);
        CloseButton.Content = new MaterialIcon { Kind = Material.Icons.MaterialIconKind.Close, Width = 15, Height = 15 };
        CloseButton.HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center;
        CloseButton.VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center;

        CloseButton.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
        CloseButton.PointerPressed += (s, e) =>
        {
            Console.WriteLine("Close tab");
            tabArea.RemoveTab(this);
            e.Handled = true;
        };

        CloseButton.PointerEntered += (s, e) =>
        {
            CloseButton.Background = new SolidColorBrush(0x50808080);
        };

        CloseButton.PointerExited += (s, e) =>
        {
            CloseButton.Background = Brushes.Transparent;
        };

        CloseButton.IsVisible = false;
        DockPanel.SetDock(CloseButton, Avalonia.Controls.Dock.Right);
        contentDock.Children.Add(CloseButton);
        contentDock.Children.Add(labelGrid);
    
        TextBlock = new TextBlock { Text = "Tab" };
        TextBlock.TextAlignment = TextAlignment.Left;
        TextBlock.VerticalAlignment = VerticalAlignment.Center;
        TextBlock.Margin = new Thickness(5, 0, 0, 0);
        TextBlock.IsHitTestVisible = false;
        labelGrid.Children.Add(TextBlock);
        
        MinWidth = 100;
        MaxWidth = 200;
        
        PointerEntered += (s, e) =>
        {
            CloseButton.IsVisible = true;
            Button.Background = highlightBrush;
        };

        PointerExited += (s, e) =>
        {
            CloseButton.IsVisible = false;

            if (!isSelected)
            {
                Button.Background = Avalonia.Media.Brushes.Transparent;
            }
        };
        
        // Use AddHandler with handledEventsToo = true
        Button.AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Bubble, handledEventsToo: true);
        Button.AddHandler(PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Bubble, handledEventsToo: true);
        Button.AddHandler(PointerMovedEvent, OnPointerMoved, RoutingStrategies.Bubble, handledEventsToo: true);
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

        tabArea.TabDragEnded();

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
                tabArea.RemoveTab(this);
            }
            else
            {
                // Select tab
                tabArea.SelectTab(this);
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

        tabArea.TabDragged(this, e);
        
    }
    
    private Control CreateDragAdorner()
    {
        // Create a visual copy of the tab
        var border = new Border
        {
            Width = Bounds.Width,
            Height = Bounds.Height,
            Background = new SolidColorBrush(0x30808080), // Dark background
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
            Text = TextBlock.Text,
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

        if (!isSelected)
        {
            Button.Background = Avalonia.Media.Brushes.Transparent;
        }
        else
        {
            Button.Background = highlightBrush;
        }
    }

    private bool GetIsSelected()
    {
        return isSelected;
    }
}