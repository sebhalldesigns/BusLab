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
using System.Linq;

namespace BusLab.UI.Docking;

public enum ToolTabLocation
{
    Left,
    Right,
    Bottom
}

public partial class DockArea: UserControl
{
    private const int LeftToolColumnIndex = 0;
    private const int DocumentColumnIndex = 2;
    private const int RightToolColumnIndex = 4;
    private const int DocumentRowIndex = 0;
    private const int BottomToolRowIndex = 2;

    private List<Tab> tabs = new List<Tab>();
    private Dictionary<DockItem, Tab> tabsByItem = new Dictionary<DockItem, Tab>();
    private Dictionary<string, Tab> toolTabsById = new Dictionary<string, Tab>();
    
    public List<TabGroup> DocumentTabs = new List<TabGroup>();
    public List<TabGroup>[] ToolTabs = new List<TabGroup>[]
    {
        new List<TabGroup>(),
        new List<TabGroup>(),
        new List<TabGroup>()
    };
    
    private TabDragEvent currentTabDragEvent = new TabDragEvent();
    
    private List<Grid> ToolGrids = new List<Grid>();
    private bool rootSplitSizesInitialized;

    public IReadOnlyList<DockItem> OpenItems => tabs.Select(tab => tab.Item).ToList();
    
    public DockArea()
    {
        InitializeComponent();
        Console.WriteLine("Hello from Workbench");
        Loaded += OnLoaded;

        ToolGrids.Add(LeftToolGrid);
        ToolGrids.Add(RightToolGrid);
        ToolGrids.Add(BottomToolGrid);
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        TryFreezeRootSplitSizes();

        if (!rootSplitSizesInitialized)
        {
            LayoutUpdated += OnLayoutUpdated;
        }
    }

    private void OnLayoutUpdated(object? sender, EventArgs e)
    {
        TryFreezeRootSplitSizes();

        if (rootSplitSizesInitialized)
        {
            LayoutUpdated -= OnLayoutUpdated;
        }
    }

    private void TryFreezeRootSplitSizes()
    {
        if (rootSplitSizesInitialized)
        {
            return;
        }

        if (LeftToolGrid.Bounds.Width <= 0 || RightToolGrid.Bounds.Width <= 0 || BottomToolGrid.Bounds.Height <= 0)
        {
            return;
        }

        ColumnDefinition leftColumn = RootGrid.ColumnDefinitions[LeftToolColumnIndex];
        ColumnDefinition rightColumn = RootGrid.ColumnDefinitions[RightToolColumnIndex];
        RowDefinition bottomRow = RootGrid.RowDefinitions[BottomToolRowIndex];

        leftColumn.Width = new GridLength(Math.Max(leftColumn.MinWidth, LeftToolGrid.Bounds.Width), GridUnitType.Pixel);
        RootGrid.ColumnDefinitions[DocumentColumnIndex].Width = GridLength.Star;
        rightColumn.Width = new GridLength(Math.Max(rightColumn.MinWidth, RightToolGrid.Bounds.Width), GridUnitType.Pixel);

        RootGrid.RowDefinitions[DocumentRowIndex].Height = GridLength.Star;
        bottomRow.Height = new GridLength(Math.Max(bottomRow.MinHeight, BottomToolGrid.Bounds.Height), GridUnitType.Pixel);

        rootSplitSizesInitialized = true;
    }

    public Tab OpenDocument(DocumentDockItem item)
    {
        if (DocumentTabs.Count == 0)
        {
            TabGroup tabGroup = new TabGroup(this, DocumentGrid);
            Grid.SetColumn(tabGroup, 0);
            DocumentGrid.Children.Add(tabGroup);
            DocumentTabs.Add(tabGroup);
        }

        return OpenItem(item, DocumentTabs[0]);
    }

    public Tab OpenTool(ToolDockItem item)
    {
        if (toolTabsById.TryGetValue(item.Id, out Tab? existingTab))
        {
            SelectTab(existingTab);
            return existingTab;
        }

        int toolLocationIndex = (int)item.PreferredLocation;

        if (ToolTabs[toolLocationIndex].Count == 0)
        {
            TabGroup tabGroup = new TabGroup(this, ToolGrids[toolLocationIndex], TabType.Tool, item.PreferredLocation);
            Grid.SetColumn(tabGroup, 0);
            ToolGrids[toolLocationIndex].Children.Add(tabGroup);
            ToolTabs[toolLocationIndex].Add(tabGroup);
        }

        return OpenItem(item, ToolTabs[toolLocationIndex][0]);
    }

    public bool TrySelectItem(DockItem item)
    {
        if (!tabsByItem.TryGetValue(item, out Tab? tab))
        {
            return false;
        }

        SelectTab(tab);
        return true;
    }

    public void CloseItem(DockItem item)
    {
        if (!tabsByItem.TryGetValue(item, out Tab? tab))
        {
            return;
        }

        RemoveTab(tab);
    }

    private Tab OpenItem(DockItem item, TabGroup group)
    {
        Tab tab = new Tab(this, item);

        group.AddTab(tab);
        tabs.Add(tab);
        tabsByItem[item] = tab;

        if (item is ToolDockItem toolItem)
        {
            toolTabsById[toolItem.Id] = tab;
        }

        SelectTab(tab);

        return tab;
    }

    internal void RemoveTab(Tab tab)
    {
        if (tabs.Contains(tab))
        {
            tab.TabGroup?.RemoveTab(tab);
            tab.ReleaseItemSubscriptions();
            tabs.Remove(tab);
            tabsByItem.Remove(tab.Item);

            if (tab.Item is ToolDockItem)
            {
                toolTabsById.Remove(tab.Item.Id);
            }
        }

        RemoveEmptyTabs();
    }

    public void SelectTab(Tab tab)
    {
        tab.TabGroup?.SelectTab(tab);
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

    private Control CreateDemoContent(string title, TabType tabType)
    {
        Border border = new Border
        {
            Background = new SolidColorBrush(Color.FromUInt32(tabType == TabType.Tool ? 0x10FFFFFFu : 0x08000000u)),
            Padding = new Thickness(12)
        };

        border.Child = new TextBlock
        {
            Text = $"{title} content",
            VerticalAlignment = VerticalAlignment.Center
        };

        return border;
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

        TabGroup newTabGroup = new TabGroup(this, newGrid, sourceGroup.TabType, sourceGroup.ToolLocation);
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

        if (sourceGroup.TabType == TabType.Tool && sourceGroup.ToolLocation.HasValue)
        {
            ToolTabs[(int)sourceGroup.ToolLocation.Value].Add(newTabGroup);
        }
        else
        {
            DocumentTabs.Add(newTabGroup);
        }

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
