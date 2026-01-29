using Avalonia;
using Avalonia.Controls;
using System;
using Material.Icons.Avalonia;

namespace BusLab;

public class Workbench: UserControl
{
    public Workbench()
    {
        Console.WriteLine("Hello from Workbench");

        Content = new TabGroup();
    }
}

public class TabGroup: UserControl
{
    public TabGroup()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition(25.0f, GridUnitType.Pixel));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Star));

        ScrollViewer scrollViewer = new ScrollViewer();
        Grid.SetRow(scrollViewer, 0);
        scrollViewer.Background = new Avalonia.Media.SolidColorBrush(0x30808080);

        ContentControl contentControl = new ContentControl();
        contentControl.Content = new TextBlock { Text = "Content goes here." };
        Grid.SetRow(contentControl, 1);

        grid.Children.Add(scrollViewer);
        grid.Children.Add(contentControl);

        Content = grid;

        StackPanel tabPanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal };
        scrollViewer.Content = tabPanel;

        for (int i = 0; i < 5; i++)
        {
            Tab tab = new Tab();
            tabPanel.Children.Add(tab);
            tab.HandleButton.Click += (s, e) => SelectTab(tab);
        }

    }

    public void SelectTab(Tab tab)
    {
        foreach (var child in ((StackPanel)((ScrollViewer)((Grid)Content).Children[0]).Content).Children)
        {
            if (child is Tab t)
            {
                t.IsSelected = (t == tab);
            }
        }
    }
}

public class Tab: UserControl
{

    public bool IsSelected { get => GetIsSelected(); set => SetIsSelected(value); }
    private bool isSelected = false;

    private DockPanel contentDock;
    private Grid labelGrid;

    public Button Button;
    public Button CloseButton;
    public TextBlock TextBlock;
    public Button HandleButton;

    public Tab()
    {

        /* create a new button on top of label */
        
        Button = new Button();
        Content = Button;
        Button.HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
        Button.Padding = new Thickness(0);
        
        contentDock = new DockPanel();
        Button.Content = contentDock;

        labelGrid = new Grid();

        CloseButton = new Button();
        CloseButton.Width = 17;
        CloseButton.Height = 17;
        CloseButton.Background = Avalonia.Media.Brushes.Transparent;
        CloseButton.Margin = new Thickness(0, 0, 3, 0);
        CloseButton.CornerRadius = new CornerRadius(4);
        CloseButton.BorderThickness = new Thickness(0);
        CloseButton.Content = new MaterialIcon { Kind = Material.Icons.MaterialIconKind.Close, Width = 15, Height = 15 };
        CloseButton.HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center;
        CloseButton.VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center;

        CloseButton.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
        CloseButton.PointerPressed += (s, e) =>
        {
            e.Handled = true;
        };

        CloseButton.PointerEntered += (s, e) =>
        {
            CloseButton.Background = new Avalonia.Media.SolidColorBrush(0x50808080);
        };

        CloseButton.PointerExited += (s, e) =>
        {
            CloseButton.Background = Avalonia.Media.Brushes.Transparent;
        };

        CloseButton.IsVisible = false;

        DockPanel.SetDock(CloseButton, Avalonia.Controls.Dock.Right);
        contentDock.Children.Add(CloseButton);
        contentDock.Children.Add(labelGrid);
    
        TextBlock = new TextBlock { Text = "Tab" };
        TextBlock.TextAlignment = Avalonia.Media.TextAlignment.Left;
        TextBlock.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
        TextBlock.Margin = new Thickness(5, 0, 0, 0);
        labelGrid.Children.Add(TextBlock);

        HandleButton = new Button();
        HandleButton.Background = Avalonia.Media.Brushes.Transparent;
        HandleButton.BorderThickness = new Thickness(0);
        labelGrid.Children.Add(HandleButton);
        
        MinWidth = 100;
        MaxWidth = 200;

        IsSelected = false;

        PointerEntered += (s, e) =>
        {
            CloseButton.IsVisible = true;
        };

        PointerExited += (s, e) =>
        {
            CloseButton.IsVisible = false;
        };
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