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

public partial class TabGroup: UserControl
{

    public List<Tab> Tabs = new List<Tab>();
    
    private List<Panel> inBetweenPanels = new List<Panel>();
    private DockArea dockArea;
    private Panel overlayPanel;

    public Grid Grid;

    public TabType TabType { get; private set; }

    public TabGroup(DockArea dockArea, Grid parentGrid, TabType tabType = TabType.Document)
    {
        this.dockArea = dockArea;
        this.Grid = parentGrid;
        this.TabType = tabType;

        InitializeComponent();

        if (tabType == TabType.Tool)
        {
            TabBackground.IsVisible = true;
            DocumentHighlight.IsVisible = false;
        }
        else
        {
            
        }

        overlayPanel = new Panel();
        overlayPanel.Background = new SolidColorBrush(0x804787d1);

        Panel inBetween2 = new Panel();
        inBetween2.Width = 4;
        inBetween2.Margin = new Thickness(-2, 0, -2, 0);
        inBetween2.Background = Brushes.Transparent;
        TabStackPanel.Children.Add(inBetween2);
        inBetweenPanels.Add(inBetween2);

    }

    public void RemoveTab(Tab tab)
    {
        if (Tabs.Contains(tab))
        {
            
            /* remove an inBetweenPanel */
            int tabIndex = Tabs.IndexOf(tab);
            Panel inBetweenToRemove = inBetweenPanels[tabIndex + 1];
            TabStackPanel.Children.Remove(inBetweenToRemove);
            inBetweenPanels.Remove(inBetweenToRemove);

            TabStackPanel.Children.Remove(tab);
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
            TabStackPanel.Children.Add(tab);
            Tabs.Add(tab);
            TabStackPanel.Children.Add(inBetween);
            inBetweenPanels.Add(inBetween);
        }
        else
        {
            TabStackPanel.Children.Insert(index * 2 + 1, inBetween);
            TabStackPanel.Children.Insert(index * 2 + 1, tab);
            Tabs.Insert(index, tab);
            inBetweenPanels.Insert(index + 1, inBetween);
        }

        tab.TabGroup = this;

        foreach (Tab t in Tabs)
        {
            t.IsSelected = false;
        }

        tab.IsSelected = true;
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
                ContentGrid.Children.Remove(overlayPanel);
            }   

            return false;
        }

        dragEvent.TargetGroup = this;

        if (position.Y < 25)
        {
            /* handle tab bar area */
            if (overlayPanel.Parent != null)
            {
                ContentGrid.Children.Remove(overlayPanel);
            }   

            for (int i = 0; i < Tabs.Count; i++)
            {
                if (Tabs[i].Bounds.Contains(e.GetPosition(TabStackPanel)))
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
                ContentGrid.Children.Add(overlayPanel);
            }   
            
        }


        return true;
    }

    public void TabDragEnded()
    {   
        if (overlayPanel.Parent != null)
        {
            ContentGrid.Children.Remove(overlayPanel);
        }   

        foreach (Panel inBetween in inBetweenPanels)
        {
            inBetween.Background = Brushes.Transparent;
        }
    }

    private void DebugPrintStructure()
    {
        Console.WriteLine("=== Tab Panel Structure ===");
        for (int i = 0; i < TabStackPanel.Children.Count; i++)
        {
            var child = TabStackPanel.Children[i];
            if (child is Panel)
                Console.WriteLine($"{i}: InBetween Panel");
            else if (child is Tab tab)
                Console.WriteLine($"{i}: Tab '{tab.TextBlock.Text}'");
        }
        Console.WriteLine($"Tabs.Count: {Tabs.Count}");
        Console.WriteLine($"InBetweenPanels.Count: {inBetweenPanels.Count}");
    }
}
