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
using System.IO;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;

using BusLab.Windows;

namespace BusLab.Workspace;

public enum WorkspaceItem
{
    Database,
    Net,
    Filter,
    Plot,
    Canvas
}

public class ExplorerEntry
{
    public string Name { get; set; } = "New Entry";
    public string Path { get; set; } = "";
    public IImage? Icon { get; set; } = null;
    public bool IconVisible { get; set; } = false;
    public Thickness LabelMargin { get; set; } = new Thickness(0, 0, 0, 0);
    public ObservableCollection<ExplorerEntry> Children { get; set; } = new ObservableCollection<ExplorerEntry>();
}

public class WorkspaceManager
{
    public string WorkspacePath { get; set; }
    private MainWindow parentWindow;

    public WorkspaceManager(MainWindow parentWindow, string path = "")
    {
        this.parentWindow = parentWindow;
        WorkspacePath = path;

        if (path == "")
        {
            /* create blank workspace */
            string docsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            path = Path.Combine(docsPath, "BusLab");

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            WorkspacePath = path;
        }

        UpdatePath();
    }

    public async Task NewFile()
    {   
        NewPopupWindow newWindow = new NewPopupWindow((Window)parentWindow);
        parentWindow.IsEnabled = false;
        await newWindow.ShowDialog((Window)parentWindow);
        parentWindow.IsEnabled = true;
    }

    public async void OpenFile()
    {
        IReadOnlyList<IStorageFile> files = await parentWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open File",
            AllowMultiple = false
        });

        if (files.Count == 1)
        {
            Console.WriteLine("Selected file: " + files[0].Path);
        }
    }

    public async void OpenWorkspace()
    {
        IReadOnlyList<IStorageFolder> folders = await parentWindow.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Open Folder",
            AllowMultiple = false
        });

        if (folders.Count == 1)
        {
            string? localPath = folders[0].TryGetLocalPath();
            if (localPath != null)
            {
                Console.WriteLine("Selected folder: " + localPath);
                WorkspacePath = localPath;
                UpdatePath();    
            }
        }
    }

    public async void OpenDocs()
    {
        string docsUrl = "https://buslab.net";
        await parentWindow.Launcher.LaunchUriAsync(new Uri(docsUrl));
    }

    private void UpdatePath()
    {
        parentWindow.ExplorerControl.SetWorkspaceTitle(Path.GetFileName(WorkspacePath));
        parentWindow.PathTextBlock.Text = WorkspacePath;

        /* update explorer entries */

        ObservableCollection<ExplorerEntry> Entries = new ObservableCollection<ExplorerEntry>();    


        List<string> list = new List<string>();
        try
        {
            SearchDirectory(list, WorkspacePath);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return;
        }

        foreach (string file in list)
        {
            string extension = Path.GetExtension(file).ToLower();

            string fileName = Path.GetFileName(file);
            ExplorerEntry entry = new ExplorerEntry
            {
                Name = fileName,
                Path = file,
                LabelMargin = new Thickness(20, 0, 0, 0),
                IconVisible = true
            };

            switch (extension)
            {
                case ".net":
                    entry.Icon = new Bitmap(AssetLoader.Open(new Uri("avares://BusLab/Assets/Icons/icons8-lan-16.png")));
                    break;
                case ".filter":
                    entry.Icon = new Bitmap(AssetLoader.Open(new Uri("avares://BusLab/Assets/Icons/icons8-filter-16.png")));
                    break;
                case ".dbc":
                    entry.Icon = new Bitmap(AssetLoader.Open(new Uri("avares://BusLab/Assets/Icons/icons8-database-16.png")));
                    break;
                case ".plot":
                    entry.Icon = new Bitmap(AssetLoader.Open(new Uri("avares://BusLab/Assets/Icons/icons8-line-chart-16.png")));
                    break;
                case ".canvas":
                    entry.Icon = new Bitmap(AssetLoader.Open(new Uri("avares://BusLab/Assets/Icons/icons8-vertical-timeline-16.png")));
                    break;
                default:
                    entry.Icon = new Bitmap(AssetLoader.Open(new Uri("avares://BusLab/Assets/Icons/icons8-file-16.png")));
                    break;
            }

            Entries.Add(entry);
        }


        // Sample data
        /*Entries.Add(new ExplorerEntry
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
        });*/

        parentWindow.ExplorerControl.UpdateEntries(Entries);
    }

    public static void SearchDirectory(List<string> files, string startDirectory)
    {
        
        // Get files in this directory, sort by file name (case-insensitive), then add
        string[] filePaths = Directory.GetFiles(startDirectory, "*.*");
        Array.Sort(filePaths, (a, b) => StringComparer.OrdinalIgnoreCase.Compare(Path.GetFileName(a), Path.GetFileName(b)));
        files.AddRange(filePaths);

        // Get subdirectories, sort by directory name (case-insensitive), then recurse
        string[] directories = Directory.GetDirectories(startDirectory);
        Array.Sort(directories, (a, b) => StringComparer.OrdinalIgnoreCase.Compare(Path.GetFileName(a), Path.GetFileName(b)));
        foreach (string directory in directories)
        {
            SearchDirectory(files, directory);
        }
 
    }
}
