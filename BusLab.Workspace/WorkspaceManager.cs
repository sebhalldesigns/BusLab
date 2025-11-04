using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System.Collections.Generic;
using System.Threading.Tasks;

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

public class WorkspaceManager
{
    public string WorkspacePath { get; set; }
    private Window parentWindow;

    public WorkspaceManager(Window parentWindow, string path = "")
    {
        this.parentWindow = parentWindow;
        WorkspacePath = path;

        if (path == "")
        {
            /* create blank workspace */
            string docsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            path = Path.Combine(docsPath, "BusLab");
        }
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
            Console.WriteLine("Selected folder: " + folders[0].Path);
        }
    }

    public async void OpenDocs()
    {
        string docsUrl = "https://buslab.net";
        await parentWindow.Launcher.LaunchUriAsync(new Uri(docsUrl));
    }
}
