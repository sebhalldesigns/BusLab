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

public enum TabDragEventType
{
    NONE,
    SPLIT,
    DROP, 
    INSERT
}

public enum TabSplitDirection
{
    NONE,
    LEFT,
    RIGHT,
    UP,
    DOWN
}

public class TabDragEvent
{
    public TabDragEventType EventType { get; set; } = TabDragEventType.NONE;
    public TabSplitDirection SplitDirection { get; set; } = TabSplitDirection.NONE;
    public int InsertIndex { get; set; } = -1;
    public Tab? Tab { get; set; } = null;
    public TabGroup? TargetGroup { get; set; } = null;
}

public class Workbench: UserControl
{
    public List<TabGroup> TabGroups = new List<TabGroup>(); 
    public List<Tab> Tabs = new List<Tab>();
    
    private Grid rootGrid;
    private TabDragEvent currentTabDragEvent = new TabDragEvent();
    
    public Workbench()
    {
        Console.WriteLine("Hello from Workbench");

        rootGrid = new Grid();
        this.Content = rootGrid;

        for (int i = 0; i < 10; i++)
        {
            Tab tab = AddTab();
            tab.TextBlock.Text = "Tab " + (i + 1);
        }

    }

    public Tab AddTab()
    {
        Tab tab = new Tab(this);

        if (TabGroups.Count == 0)
        {
            TabGroup tabGroup = new TabGroup(this, rootGrid);
            Grid.SetColumn(tabGroup, 0);
            rootGrid.Children.Add(tabGroup);
            TabGroups.Add(tabGroup);
        }

        TabGroups[0].AddTab(tab);
        Tabs.Add(tab);

        return tab;
    }

    public void RemoveTab(Tab tab)
    {
        if (Tabs.Contains(tab))
        {
            tab.TabGroup?.RemoveTab(tab);
            Tabs.Remove(tab);
        }

        RemoveEmptyTabGroups();
    }

    public void SelectTab(Tab tab)
    {
        foreach (Tab t in Tabs)
        {
            t.IsSelected = false;
        }

        tab.IsSelected = true;
    }

    public void TabDragged(Tab tab, PointerEventArgs e)
    {

        currentTabDragEvent = new TabDragEvent { Tab = tab };
        
        foreach (TabGroup group in TabGroups)
        {
            group.TabDragged(tab, e, currentTabDragEvent);
        }
    }

    public void TabDragEnded()
    {
        foreach (TabGroup group in TabGroups)
        {
            group.TabDragEnded();
        }

        Console.WriteLine("Tab drag ended with event type: " + currentTabDragEvent.EventType);
        Console.WriteLine("Split direction: " + currentTabDragEvent.SplitDirection);
        Console.WriteLine("Insert index: " + currentTabDragEvent.InsertIndex);

        if (currentTabDragEvent.Tab == null || currentTabDragEvent.TargetGroup == null) return;

        switch (currentTabDragEvent.EventType)
        {
            case TabDragEventType.DROP:
            {
                if (currentTabDragEvent.Tab.TabGroup != currentTabDragEvent.TargetGroup)
                {   
                    /* move tab to target group */
                    currentTabDragEvent.Tab.TabGroup?.RemoveTab(currentTabDragEvent.Tab);
                    currentTabDragEvent.TargetGroup.AddTab(currentTabDragEvent.Tab);
                }
               
            } break;

            case TabDragEventType.INSERT:
            {
                /* move tab to target group at specified index */
                currentTabDragEvent.Tab.TabGroup?.RemoveTab(currentTabDragEvent.Tab);
                currentTabDragEvent.TargetGroup.AddTab(currentTabDragEvent.Tab, currentTabDragEvent.InsertIndex);
            } break;

            case TabDragEventType.SPLIT:
            {
                currentTabDragEvent.Tab.TabGroup?.RemoveTab(currentTabDragEvent.Tab);
                SplitTabGroup(currentTabDragEvent.TargetGroup, currentTabDragEvent.SplitDirection).AddTab(currentTabDragEvent.Tab);

            } break;

        }

        currentTabDragEvent = new TabDragEvent();

        RemoveEmptyTabGroups();
    }

    private TabGroup SplitTabGroup(TabGroup sourceGroup, TabSplitDirection direction)
    {
        Grid parentGrid = sourceGroup.Grid;

        /* swap in the new grid */
        Grid newGrid = new Grid();  
        Grid.SetRow(newGrid, Grid.GetRow(sourceGroup));
        Grid.SetColumn(newGrid, Grid.GetColumn(sourceGroup));
        parentGrid.Children.Remove(sourceGroup);
        parentGrid.Children.Add(newGrid);

        if (direction == TabSplitDirection.LEFT || direction == TabSplitDirection.RIGHT)
        {
            newGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            newGrid.ColumnDefinitions.Add(new ColumnDefinition(2.0, GridUnitType.Pixel));
            newGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

            if (direction == TabSplitDirection.LEFT)
            {
                Grid.SetColumn(sourceGroup, 2);
            }
            else
            {
                Grid.SetColumn(sourceGroup, 0);
            }

            GridSplitter splitter = new GridSplitter
            {
                Width = 2.0,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            Grid.SetColumn(splitter, 1);
            newGrid.Children.Add(splitter);
        }
        else
        {
            newGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
            newGrid.RowDefinitions.Add(new RowDefinition(2.0, GridUnitType.Pixel));
            newGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));

            if (direction == TabSplitDirection.UP)
            {
                Grid.SetRow(sourceGroup, 2);
            }
            else
            {
                Grid.SetRow(sourceGroup, 0);
            }

            GridSplitter splitter = new GridSplitter
            {
                Height = 2.0,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center
            };

            Grid.SetRow(splitter, 1);
            newGrid.Children.Add(splitter);
        }
        
        newGrid.Children.Add(sourceGroup);
        sourceGroup.Grid = newGrid;

        TabGroup newTabGroup = new TabGroup(this, newGrid);
        if (direction == TabSplitDirection.LEFT || direction == TabSplitDirection.RIGHT)
        {
            if (direction == TabSplitDirection.LEFT)
            {
                Grid.SetColumn(newTabGroup, 0);
            }
            else
            {
                Grid.SetColumn(newTabGroup, 2);
            }
        }
        else
        {
            if (direction == TabSplitDirection.UP)
            {
                Grid.SetRow(newTabGroup, 0);
            }
            else
            {
                Grid.SetRow(newTabGroup, 2);
            }
        }

        newGrid.Children.Add(newTabGroup);
        TabGroups.Add(newTabGroup);

        return newTabGroup;

    }

    private void RemoveEmptyTabGroups()
    {
        List<TabGroup> groupsToRemove = new List<TabGroup>();
        foreach (TabGroup group in TabGroups)
        {
            if (group.Tabs.Count == 0) 
            {
                groupsToRemove.Add(group);
            }
        }

        foreach (TabGroup group in groupsToRemove)
        {
            TabGroups.Remove(group);
            Grid parentGrid = group.Grid;

            /* If this group is inside a split (3 children: Group, Splitter, Other) */
            if (parentGrid.Children.Count == 3)
            {
                Control? remainingChild = null;
                foreach (var child in parentGrid.Children)
                {
                    /* Find the child that ISN'T the empty group and ISN'T the splitter */
                    if (child != group && !(child is GridSplitter))
                    {
                        remainingChild = child as Control;
                        break;
                    }
                }

                Grid? higherLevelGrid = parentGrid.Parent as Grid;
                if (higherLevelGrid != null && remainingChild != null)
                {
                    int row = Grid.GetRow(parentGrid);
                    int col = Grid.GetColumn(parentGrid);

                    /* Detach remaining child from the split-grid */
                    parentGrid.Children.Remove(remainingChild);
                    
                    /* Replace the split-grid with the remaining child in the higher grid */
                    higherLevelGrid.Children.Remove(parentGrid);
                    higherLevelGrid.Children.Add(remainingChild);
                    Grid.SetRow(remainingChild, row);
                    Grid.SetColumn(remainingChild, col);

                    /* Update the internal Grid reference if the remaining child is a TabGroup */
                    if (remainingChild is TabGroup remainingGroup)
                    {
                        remainingGroup.Grid = higherLevelGrid;
                    }
                }
            }
            else if (parentGrid == rootGrid)
            {
                parentGrid.Children.Remove(group);
            }
        }
    }

}

public class TabGroup: UserControl
{

    public List<Tab> Tabs = new List<Tab>();
    
    public ContentControl ContentControl;

    private List<Panel> inBetweenPanels = new List<Panel>();
    private Workbench workbench;
    private Grid contentGrid;
    private Panel overlayPanel;
    private StackPanel tabPanel;

    public Grid Grid;

    public TabGroup(Workbench workbench, Grid parentGrid)
    {
        this.workbench = workbench;
        this.Grid = parentGrid;

        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition(25.0f, GridUnitType.Pixel));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Star));

        ScrollViewer scrollViewer = new ScrollViewer();
        Grid.SetRow(scrollViewer, 0);

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

        tabPanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal };
        scrollViewer.Content = tabPanel;

        Panel inBetween2 = new Panel();
        inBetween2.Width = 4;
        inBetween2.Margin = new Thickness(-2, 0, -2, 0);
        inBetween2.Background = Brushes.Transparent;
        tabPanel.Children.Add(inBetween2);
        inBetweenPanels.Add(inBetween2);

    }

    public void RemoveTab(Tab tab)
    {
        if (Tabs.Contains(tab))
        {
            
            /* remove an inBetweenPanel */
            int tabIndex = Tabs.IndexOf(tab);
            Panel inBetweenToRemove = inBetweenPanels[tabIndex + 1];
            tabPanel.Children.Remove(inBetweenToRemove);
            inBetweenPanels.Remove(inBetweenToRemove);

            tabPanel.Children.Remove(tab);
            Tabs.Remove(tab);
        }
    }

    public void AddTab(Tab tab, int index = -1)
    {
        Panel inBetween = new Panel();
        inBetween.Width = 4;
        inBetween.Margin = new Thickness(-2, 0, -2, 0);
        inBetween.Background = Brushes.Transparent;

        if (index < 0 || index > Tabs.Count)
        {
            tabPanel.Children.Add(tab);
            Tabs.Add(tab);
            tabPanel.Children.Add(inBetween);
            inBetweenPanels.Add(inBetween);
        }
        else
        {
            tabPanel.Children.Insert(index * 2 + 1, inBetween);
            tabPanel.Children.Insert(index * 2 + 1, tab);
            Tabs.Insert(index, tab);
            inBetweenPanels.Insert(index + 1, inBetween);
        }

        tab.TabGroup = this;
    }

    public bool TabDragged(Tab tab, PointerEventArgs e, TabDragEvent dragEvent)
    {

        Visual? parent = this.GetVisualParent();
        if (parent == null) return false;

        Point position = e.GetPosition(parent);

        foreach (Panel inBetween in inBetweenPanels)
        {
            inBetween.Background = Brushes.Transparent;
        }
        
        if (!this.Bounds.Contains(position))
        {
            if (overlayPanel.Parent != null)
            {
                contentGrid.Children.Remove(overlayPanel);
            }   

            return false;
        }

        dragEvent.TargetGroup = this;

        if (position.Y < 25)
        {
            /* handle tab bar area */
            if (overlayPanel.Parent != null)
            {
                contentGrid.Children.Remove(overlayPanel);
            }   

            for (int i = 0; i < Tabs.Count; i++)
            {
                if (Tabs[i].Bounds.Contains(e.GetPosition(tabPanel)))
                {
                    if (e.GetPosition(Tabs[i]).X < Tabs[i].Bounds.Width / 2)
                    {
                        /* left side */
                        Panel leftInBetween = inBetweenPanels[i];
                        leftInBetween.Background = new SolidColorBrush(0x804787d1);
                        dragEvent.EventType = TabDragEventType.INSERT;
                        dragEvent.InsertIndex = i;
                    }
                    else
                    {
                        /* right side */
                        Panel rightInBetween = inBetweenPanels[i + 1];
                        rightInBetween.Background = new SolidColorBrush(0x804787d1);
                        dragEvent.EventType = TabDragEventType.INSERT;
                        dragEvent.InsertIndex = i + 1;
                    }

                    return true;
                }
            }

            Panel endInBetween = inBetweenPanels[inBetweenPanels.Count - 1];
            endInBetween.Background = new SolidColorBrush(0x804787d1);

            dragEvent.EventType = TabDragEventType.INSERT;
            dragEvent.InsertIndex = -1; /* append to end */
            return true;
        }
        else
        {
            /* handle content area */
            
            double xProportion = (position.X - this.Bounds.X) / this.Bounds.Width;
            double yProportion = (position.Y - 25 - this.Bounds.Y) / (this.Bounds.Height - 25);

            Grid.SetRow(overlayPanel, 0);
            Grid.SetRowSpan(overlayPanel, 2);

            if (xProportion < 0.2)
            {
                Grid.SetColumn(overlayPanel, 0);
                Grid.SetColumnSpan(overlayPanel, 1);
                dragEvent.EventType = TabDragEventType.SPLIT;
                dragEvent.SplitDirection = TabSplitDirection.LEFT;
            }
            else if (xProportion > 0.8)
            {
                Grid.SetColumn(overlayPanel, 1);
                Grid.SetColumnSpan(overlayPanel, 1);
                dragEvent.EventType = TabDragEventType.SPLIT;
                dragEvent.SplitDirection = TabSplitDirection.RIGHT;
            }
            else
            {
                Grid.SetColumn(overlayPanel, 0);
                Grid.SetColumnSpan(overlayPanel, 2);

                if (yProportion < 0.2)
                {
                    Grid.SetRow(overlayPanel, 0);
                    Grid.SetRowSpan(overlayPanel, 1);
                    dragEvent.EventType = TabDragEventType.SPLIT;
                    dragEvent.SplitDirection = TabSplitDirection.UP;

                }
                else if (yProportion > 0.8)
                {
                    Grid.SetRow(overlayPanel, 1);
                    Grid.SetRowSpan(overlayPanel, 1);
                    dragEvent.EventType = TabDragEventType.SPLIT;
                    dragEvent.SplitDirection = TabSplitDirection.DOWN;
                }
                else
                {
                    dragEvent.EventType = TabDragEventType.DROP;
                }
            }

            if (overlayPanel.Parent == null)
            {
                contentGrid.Children.Add(overlayPanel);
            }   
            
        }


        return true;
    }

    public void TabDragEnded()
    {   
        if (overlayPanel.Parent != null)
        {
            contentGrid.Children.Remove(overlayPanel);
        }   

        foreach (Panel inBetween in inBetweenPanels)
        {
            inBetween.Background = Brushes.Transparent;
        }
    }

    private void DebugPrintStructure()
    {
        Console.WriteLine("=== Tab Panel Structure ===");
        for (int i = 0; i < tabPanel.Children.Count; i++)
        {
            var child = tabPanel.Children[i];
            if (child is Panel)
                Console.WriteLine($"{i}: InBetween Panel");
            else if (child is Tab tab)
                Console.WriteLine($"{i}: Tab '{tab.TextBlock.Text}'");
        }
        Console.WriteLine($"Tabs.Count: {Tabs.Count}");
        Console.WriteLine($"InBetweenPanels.Count: {inBetweenPanels.Count}");
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

    public TabGroup? TabGroup;

    public Tab(Workbench workbench, TabGroup? tabGroup = null)
    {
        this.workbench = workbench;
        this.TabGroup = tabGroup;

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
            workbench.RemoveTab(this);
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

        workbench.TabDragEnded();

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
                workbench.RemoveTab(this);
            }
            else
            {
                // Select tab
                workbench.SelectTab(this);
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

        workbench.TabDragged(this, e);
        
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