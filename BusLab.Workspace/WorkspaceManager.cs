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
using BusLab.Workspace.Panels;

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

        if (newWindow.NewItem != null)
        {
            PanelContainer? panelContainer = null;

            switch (newWindow.NewItem)
            {
                case WorkspaceItem.Database:
                    panelContainer = parentWindow.TabFactory.AddNewPanel();
                    if (panelContainer == null) return;

                    DatabasePanel databasePanel = new DatabasePanel();
                    databasePanel.IsModified = true;
                    panelContainer.SetPanel(databasePanel);
                    panelContainer.SetTabTitle("Untitled.dbc*");
                    break;
            }
        }        
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

            ErrorWindow errorWindow = new ErrorWindow("Not implemented!", "Opening files from outside the workspace is not yet implemented.");
            errorWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            errorWindow.ShowDialog((Window)parentWindow);
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

    public async void SaveFile()
    {
        PanelContainer? activePanelContainer = parentWindow.TabFactory.ActivePanel;

        if (activePanelContainer != null && activePanelContainer.Panel != null)
        {
            activePanelContainer.Panel.Save();
        }
    }

    public async void SaveFileAs()
    {
        PanelContainer? activePanelContainer = parentWindow.TabFactory.ActivePanel;

        if (activePanelContainer != null && activePanelContainer.Panel != null)
        {
            activePanelContainer.Panel.SaveAs();
        }
    }

    public void ExplorerEntrySelected(ExplorerEntry entry)
    {

        if (System.IO.File.Exists(entry.Path))
        {

            foreach (PanelContainer panel in parentWindow.TabFactory.ActivePanelContainers)
            {
                if (panel.Panel?.ExplorerEntry != null && panel.Panel.ExplorerEntry.Path == entry.Path)
                {
                    parentWindow.TabFactory.ActivateDocument(panel.Parent);
                    return;
                }
            }

            PanelContainer? panelContainer = null;

            string extension = Path.GetExtension(entry.Path).ToLower();

            switch (extension)
            {

                case ".dbc":
                    panelContainer = parentWindow.TabFactory.AddNewPanel();
                    if (panelContainer == null) return;

                    DatabasePanel databasePanel = new DatabasePanel();
                    databasePanel.LoadFile(entry, File.ReadAllText(entry.Path));
                    panelContainer.SetPanel(databasePanel);
                    panelContainer.SetTabTitle(entry.Name);
                    break;

                case ".net":
                    panelContainer = parentWindow.TabFactory.AddNewPanel();
                    if (panelContainer == null) return;

                    NetPanel netPanel = new NetPanel();
                    netPanel.LoadFile(entry, File.ReadAllText(entry.Path));
                    panelContainer.SetPanel(netPanel);
                    panelContainer.SetTabTitle(entry.Name);
                    break;

                case ".filter":
                    panelContainer = parentWindow.TabFactory.AddNewPanel();
                    if (panelContainer == null) return;
                    
                    FilterPanel filterPanel = new FilterPanel();
                    filterPanel.LoadFile(entry, File.ReadAllText(entry.Path));
                    panelContainer.SetPanel(filterPanel);
                    panelContainer.SetTabTitle(entry.Name);
                    break;

                case ".plot":
                    panelContainer = parentWindow.TabFactory.AddNewPanel();
                    if (panelContainer == null) return;

                    PlotPanel plotPanel = new PlotPanel();
                    plotPanel.LoadFile(entry, File.ReadAllText(entry.Path));
                    panelContainer.SetPanel(plotPanel);
                    panelContainer.SetTabTitle(entry.Name);
                    break;

                case ".canvas":
                    panelContainer = parentWindow.TabFactory.AddNewPanel();
                    if (panelContainer == null) return;

                    CanvasPanel canvasPanel = new CanvasPanel();
                    canvasPanel.LoadFile(entry, File.ReadAllText(entry.Path));
                    panelContainer.SetPanel(canvasPanel);
                    panelContainer.SetTabTitle(entry.Name);
                    break;
                
                default:
                    long fileSize = new FileInfo(entry.Path).Length;

                    if (fileSize > 1*1024*1024)
                    {
                        ErrorWindow errorWindow = new ErrorWindow("File too large!", $"The selected file size {(double)fileSize / (1024.0*1024.0):F3}MB is >1MB and cannot be opened.");
                        errorWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        errorWindow.ShowDialog((Window)parentWindow);
                        return;
                    }
                    
                    panelContainer = parentWindow.TabFactory.AddNewPanel();
                    if (panelContainer == null) return;

                    TextPanel textPanel = new TextPanel();

                    string fileContents = System.IO.File.ReadAllText(entry.Path);

                    textPanel.LoadFile(entry, File.ReadAllText(entry.Path));
                    panelContainer.SetPanel(textPanel);
                    panelContainer.SetTabTitle(entry.Name);
                    break;
            }
        }
    }

    public void UpdatePath()
    {
        parentWindow.ExplorerControl.SetWorkspaceTitle(Path.GetFileName(WorkspacePath));
        parentWindow.PathTextBlock.Text = WorkspacePath;

        /* update explorer entries */

        ObservableCollection<ExplorerEntry> Entries = new ObservableCollection<ExplorerEntry>();    

        try
        {
            SearchDirectory(Entries, null, WorkspacePath);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            ErrorWindow errorWindow = new ErrorWindow("Could not open folder!", e.Message);
            errorWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            errorWindow.ShowDialog((Window)parentWindow);
            return;
        }

        parentWindow.ExplorerControl.UpdateEntries(Entries);
    }

    public static void SearchDirectory(ObservableCollection<ExplorerEntry> entries, ExplorerEntry? activeFolder, string startDirectory)
    {
        
        // Get files in this directory, sort by file name (case-insensitive), then add
        string[] filePaths = Directory.GetFiles(startDirectory, "*.*");
        Array.Sort(filePaths, (a, b) => StringComparer.OrdinalIgnoreCase.Compare(Path.GetFileName(a), Path.GetFileName(b)));

        foreach (string file in filePaths)
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
            
            if (activeFolder != null)
            {
                activeFolder.Children.Add(entry);
            }
            else
            {
                entries.Add(entry);    
            }
        }

        // Get subdirectories, sort by directory name (case-insensitive), then recurse
        string[] directories = Directory.GetDirectories(startDirectory);
        Array.Sort(directories, (a, b) => StringComparer.OrdinalIgnoreCase.Compare(Path.GetFileName(a), Path.GetFileName(b)));

        foreach (string directory in directories)
        {
            /* skip hidden/system directories */
            if (Path.GetFileName(directory).StartsWith("."))
                continue;

            string dirName = Path.GetFileName(directory);
            ExplorerEntry entry = new ExplorerEntry
            {
                Name = dirName,
                Path = directory,
                LabelMargin = new Thickness(0, 0, 0, 0),
                IconVisible = true
            };

            if (activeFolder != null)
            {
                activeFolder.Children.Add(entry);
            }
            else
            {
                entries.Add(entry);    
            }

            SearchDirectory(entries, entry, directory);
        }
 
    }
}
