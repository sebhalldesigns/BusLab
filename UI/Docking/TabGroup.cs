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

namespace BusLab.UI.Docking;

public class TabGroup: UserControl
{

    public List<Tab> Tabs = new List<Tab>();
    
    public ContentControl ContentControl;

    private List<Panel> inBetweenPanels = new List<Panel>();
    private TabArea tabArea;
    private Grid contentGrid;
    private Panel overlayPanel;
    private StackPanel tabPanel;

    public Grid Grid;

    public TabGroup(TabArea tabArea, Grid parentGrid)
    {
        this.tabArea = tabArea;
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