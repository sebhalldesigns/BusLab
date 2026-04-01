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

public partial class TabArea: UserControl
{
    public List<TabGroup> TabGroups = new List<TabGroup>(); 
    public List<Tab> Tabs = new List<Tab>();
    
    private TabDragEvent currentTabDragEvent = new TabDragEvent();
    
    public TabArea()
    {
        InitializeComponent();
        Console.WriteLine("Hello from Workbench");
    

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
            TabGroup tabGroup = new TabGroup(this, DocumentGrid);
            Grid.SetColumn(tabGroup, 0);
            DocumentGrid.Children.Add(tabGroup);
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
            else if (parentGrid == DocumentGrid)
            {
                parentGrid.Children.Remove(group);
            }
        }
    }

}
