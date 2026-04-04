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

public enum ToolTabLocation
{
    Left,
    Right,
    Bottom
}

public partial class DockArea: UserControl
{
    public List<Tab> Tabs = new List<Tab>();
    
    public List<TabGroup> DocumentTabs = new List<TabGroup>();
    public List<TabGroup>[] ToolTabs = new List<TabGroup>[]
    {
        new List<TabGroup>(),
        new List<TabGroup>(),
        new List<TabGroup>()
    };
    
    private TabDragEvent currentTabDragEvent = new TabDragEvent();

    private List<Grid> ToolGrids = new List<Grid>();
    
    public DockArea()
    {
        InitializeComponent();
        Console.WriteLine("Hello from Workbench");

        ToolGrids.Add(LeftToolGrid);
        ToolGrids.Add(RightToolGrid);
        ToolGrids.Add(BottomToolGrid);

        for (int i = 0; i < 10; i++)
        {
            Tab tab = AddDocumentTab();
            tab.TextBlock.Text = "Tab " + (i + 1);
        }

        for (int i = 0; i < 3; i++)
        {
            Tab tab = AddToolTab(ToolTabLocation.Left);
            tab.TextBlock.Text = "Tool " + (i + 1);
        }

        for (int i = 0; i < 3; i++)
        {
            Tab tab = AddToolTab(ToolTabLocation.Right);
            tab.TextBlock.Text = "Tool " + (i + 1);
        }

        for (int i = 0; i < 3; i++)
        {
            Tab tab = AddToolTab(ToolTabLocation.Bottom);
            tab.TextBlock.Text = "Tool " + (i + 1);
        }

    }

    public Tab AddDocumentTab()
    {
        Tab tab = new Tab(this);

        if (DocumentTabs.Count == 0)
        {
            TabGroup tabGroup = new TabGroup(this, DocumentGrid);
            Grid.SetColumn(tabGroup, 0);
            DocumentGrid.Children.Add(tabGroup);
            DocumentTabs.Add(tabGroup);
        }

        DocumentTabs[0].AddTab(tab);
        Tabs.Add(tab);

        SelectTab(tab);

        return tab;
    }

    public Tab AddToolTab(ToolTabLocation location)
    {
        Tab tab = new Tab(this, TabType.Tool);

        if (ToolTabs[(int)location].Count == 0)
        {
            TabGroup tabGroup = new TabGroup(this, ToolGrids[(int)location], TabType.Tool);
            Grid.SetColumn(tabGroup, 0);
            ToolGrids[(int)location].Children.Add(tabGroup);
            ToolTabs[(int)location].Add(tabGroup);
        }

        ToolTabs[(int)location][0].AddTab(tab);
        Tabs.Add(tab);

        SelectTab(tab);

        return tab;
    }

    public void RemoveTab(Tab tab)
    {
        if (Tabs.Contains(tab))
        {
            tab.TabGroup?.RemoveTab(tab);
            Tabs.Remove(tab);
        }

        RemoveEmptyTabs();
    }

    public void SelectTab(Tab tab)
    {
        foreach (Tab t in tab.TabGroup.Tabs)
        {
            t.IsSelected = false;
        }

        tab.IsSelected = true;
    }

    public void TabDragged(Tab tab, PointerEventArgs e)
    {

        currentTabDragEvent = new TabDragEvent { Tab = tab };

        if (tab.Type == TabType.Document)
        {
            foreach (TabGroup group in DocumentTabs)
            {
                group.TabDragged(tab, e, currentTabDragEvent);
            }
        }
        else
        {
            foreach (List<TabGroup> groups in ToolTabs)
            {
                foreach (TabGroup group in groups)
                {
                    group.TabDragged(tab, e, currentTabDragEvent);
                }
            }
        }
        
        
    }

    public void TabDragEnded()
    {
        foreach (TabGroup group in DocumentTabs)
        {
            group.TabDragEnded();
        }

        foreach (List<TabGroup> groups in ToolTabs)
        {
            foreach (TabGroup group in groups)
            {
                group.TabDragEnded();
            }
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

        RemoveEmptyTabs();
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

        TabGroup newTabGroup = new TabGroup(this, newGrid, sourceGroup.TabType);
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
        DocumentTabs.Add(newTabGroup);

        return newTabGroup;

    }

    private void RemoveEmptyTabs()
    {
        List<TabGroup> groupsToRemove = new List<TabGroup>();
        foreach (TabGroup group in DocumentTabs)
        {
            if (group.Tabs.Count == 0) 
            {
                groupsToRemove.Add(group);
            }
        }

        foreach (List<TabGroup> groups in ToolTabs)
        {
            foreach (TabGroup group in groups)
            {
                if (group.Tabs.Count == 0) 
                {
                    groupsToRemove.Add(group);
                }
            }
        }

        foreach (TabGroup group in groupsToRemove)
        {
            DocumentTabs.Remove(group);
            foreach (List<TabGroup> groups in ToolTabs)
            {
                groups.Remove(group);
            }

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
            else
            {
                foreach (Grid toolGrid in ToolGrids)
                {
                    if (parentGrid == toolGrid)
                    {
                        parentGrid.Children.Remove(group);
                        break;
                    }
                }
            }
        }
    }

   

}
