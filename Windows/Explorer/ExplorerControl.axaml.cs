using Avalonia.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Material.Icons.Avalonia;
using Material.Icons;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;

namespace BusLab;

public class ExplorerEntry
{
    public string Name { get; set; } = "New Entry";
    public string Path { get; set; } = "C:\\Path\\To\\Entry";
    public IImage? Icon { get; set; } = null;
    public bool IconVisible { get; set; } = false;
    public Thickness LabelMargin { get; set; } = new Thickness(0, 0, 0, 0);
    public ObservableCollection<ExplorerEntry> Children { get; set; } = new ObservableCollection<ExplorerEntry>();
}

public partial class ExplorerControl: UserControl
{
    public ObservableCollection<ExplorerEntry> Entries { get; set; } = new ObservableCollection<ExplorerEntry>();

    public ExplorerControl()
    {
        InitializeComponent();

        // Sample data
        Entries.Add(new ExplorerEntry
        {
            Name = "Documents",
            Path = "C:\\Users\\User\\Documents",
            Children = new ObservableCollection<ExplorerEntry>
            {
                new ExplorerEntry {
                    Name = "MyNet.net", 
                    Path = "C:\\Users\\User\\Documents\\Resume.dbc",
                    LabelMargin = new Thickness(20, 0, 0, 0),
                    Icon = new Bitmap(AssetLoader.Open(new Uri("avares://BusLab/Assets/Icons/icons8-lan-16.png"))),
                    IconVisible = true
                },
                new ExplorerEntry {
                    Name = "ExampleFilter.filter", 
                    Path = "C:\\Users\\User\\Documents\\Resume.dbc",
                    LabelMargin = new Thickness(20, 0, 0, 0),
                    Icon = new Bitmap(AssetLoader.Open(new Uri("avares://BusLab/Assets/Icons/icons8-filter-16.png"))),
                    IconVisible = true
                },
                new ExplorerEntry {
                    Name = "Network.dbc", 
                    Path = "C:\\Users\\User\\Documents\\Resume.dbc",
                    LabelMargin = new Thickness(20, 0, 0, 0),
                    Icon = new Bitmap(AssetLoader.Open(new Uri("avares://BusLab/Assets/Icons/icons8-database-16.png"))),
                    IconVisible = true
                },
                new ExplorerEntry {
                    Name = "ExamplePlot.plot", 
                    Path = "C:\\Users\\User\\Documents\\Resume.dbc",
                    LabelMargin = new Thickness(20, 0, 0, 0),
                    Icon = new Bitmap(AssetLoader.Open(new Uri("avares://BusLab/Assets/Icons/icons8-line-chart-16.png"))),
                    IconVisible = true
                },
                new ExplorerEntry {
                    Name = "ExampleCanvas.canvas", 
                    Path = "C:\\Users\\User\\Documents\\Resume.canvas",
                    LabelMargin = new Thickness(20, 0, 0, 0),
                    Icon = new Bitmap(AssetLoader.Open(new Uri("avares://BusLab/Assets/Icons/icons8-vertical-timeline-16.png"))),
                    IconVisible = true
                },
            }
        });

        this.DataContext = this;

    }

    public void NewPressed(object? sender, RoutedEventArgs e)
    {
        NewWindow newWindow = new NewWindow();
        newWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        newWindow.ShowDialog(this.GetVisualRoot() as Window);
    }
}