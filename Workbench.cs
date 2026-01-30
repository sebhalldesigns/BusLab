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

using System;
using System.Collections.Generic;

namespace BusLab;

public class Workbench: UserControl
{
    public List<TabGroup> TabGroups = new List<TabGroup>(); 

    public Workbench()
    {
        Console.WriteLine("Hello from Workbench");

        Content = new TabGroup(this);
        TabGroups.Add((TabGroup)Content);
    }

    public void TabDragged(Tab tab, PointerEventArgs e)
    {
        Console.WriteLine("Tab dragged: " + tab.TextBlock.Text + " at " + e.GetPosition(this));
        
        foreach (TabGroup group in TabGroups)
        {
            if (group.TabDragged(tab, e))
            {
                return;
            }
        }
    }
}

public class TabGroup: UserControl
{

    public List<Tab> Tabs = new List<Tab>();
    public ContentControl ContentControl;

    private Workbench workbench;
    private Grid contentGrid;
    private Panel overlayPanel;

    public TabGroup(Workbench workbench)
    {
        this.workbench = workbench;

        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition(25.0f, GridUnitType.Pixel));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Star));

        ScrollViewer scrollViewer = new ScrollViewer();
        Grid.SetRow(scrollViewer, 0);
        scrollViewer.Background = new Avalonia.Media.SolidColorBrush(0x30808080);

        contentGrid = new Grid();
        contentGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
        contentGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
        contentGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        contentGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

        Grid.SetRow(contentGrid, 1);
    
        ContentControl = new ContentControl();
        ContentControl.Content = new TextBlock { Text = "Content goes here." };
        Grid.SetRow(ContentControl, 0);
        Grid.SetRowSpan(ContentControl, 2);
        Grid.SetColumn(ContentControl, 0);
        Grid.SetColumnSpan(ContentControl, 2);
        contentGrid.Children.Add(ContentControl);

        grid.Children.Add(contentGrid);
        grid.Children.Add(scrollViewer);
    
        Content = grid;

        overlayPanel = new Panel();
        overlayPanel.Background = new SolidColorBrush(0x804787d1);

        StackPanel tabPanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal };
        scrollViewer.Content = tabPanel;

        for (int i = 0; i < 5; i++)
        {
            Tab tab = new Tab(workbench);
            tabPanel.Children.Add(tab);
            Tabs.Add(tab);
            tab.TextBlock.Text = "Tab " + (i + 1);
            tab.Button.Click += (s, e) => SelectTab(tab);
        }

        SelectTab(Tabs[0]);

    }

    public void SelectTab(Tab tab)
    {
        foreach (Tab child in Tabs)
        {
            if (child is Tab t)
            {
                t.IsSelected = (t == tab);
            }
        }
    }

    public bool TabDragged(Tab tab, PointerEventArgs e)
    {
        Point position = e.GetPosition(this);
        
        if (!this.Bounds.Contains(position))
        {
            if (overlayPanel.Parent != null)
            {
                contentGrid.Children.Remove(overlayPanel);
            }   

            return false;
        }

        if (position.Y < 25)
        {
            /* handle tab bar area */
            if (overlayPanel.Parent != null)
            {
                contentGrid.Children.Remove(overlayPanel);
            }   
        }
        else
        {
            /* handle content area */
            
            double xProportion = position.X / this.Bounds.Width;
            double yProportion = (position.Y - 25) / (this.Bounds.Height - 25);

            Grid.SetRow(overlayPanel, 0);
            Grid.SetRowSpan(overlayPanel, 2);

            if (xProportion < 0.2)
            {
                Grid.SetColumn(overlayPanel, 0);
                Grid.SetColumnSpan(overlayPanel, 1);
            }
            else if (xProportion > 0.8)
            {
                Grid.SetColumn(overlayPanel, 1);
                Grid.SetColumnSpan(overlayPanel, 1);
            }
            else
            {
                Grid.SetColumn(overlayPanel, 0);
                Grid.SetColumnSpan(overlayPanel, 2);

                if (yProportion < 0.2)
                {
                    Grid.SetRow(overlayPanel, 0);
                    Grid.SetRowSpan(overlayPanel, 1);
                }
                else if (yProportion > 0.8)
                {
                    Grid.SetRow(overlayPanel, 1);
                    Grid.SetRowSpan(overlayPanel, 1);
                }
            }

           
            if (overlayPanel.Parent == null)
            {
                contentGrid.Children.Add(overlayPanel);
            }   
            
        }


        return true;
    }
}

public class Tab: UserControl
{

    private Workbench workbench;

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

    public Tab(Workbench workbench)
    {
        this.workbench = workbench;

        /* create a new button on top of label */
        
        Button = new Button();
        Content = Button;
        Button.HorizontalContentAlignment = HorizontalAlignment.Stretch;
        Button.Padding = new Thickness(0);
        
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
        };
        PointerExited += (s, e) =>
        {
            CloseButton.IsVisible = false;
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
        
        Console.WriteLine("Stopped dragging tab.");
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

        workbench.TabDragged(this, e);
        
        Console.WriteLine($"Dragging tab by {delta}");
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

        if (isSelected)
        {
            //Button.ClearValue(BackgroundProperty);
            Button.Background = new Avalonia.Media.SolidColorBrush(0x804787d1);
            Button.BorderBrush = Avalonia.Media.Brushes.Transparent;
        }
        else
        {
            Button.Background = Avalonia.Media.Brushes.Transparent;
            Button.BorderBrush = Avalonia.Media.Brushes.Transparent;
        }
    }

    private bool GetIsSelected()
    {
        return isSelected;
    }
}