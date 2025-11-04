
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System.Collections.Generic;
using System;

using BusLab.Workspace;

using BusLab.Windows;

namespace BusLab.Workspace.Panels;

public class PanelBase: UserControl
{
    public TitleUpdateDelegate? TitleUpdate { get; set; }
    public ExplorerEntry? ExplorerEntry { get; set; }
    public bool IsModified { get; set; } = false;
    public string FileTypeDescription { get; set; } = "Text Files";
    public string FileExtension { get; set; } = ".txt";
    
    public virtual string EncodeFile()
    {
        return string.Empty;
    }

    public virtual void LoadFile(ExplorerEntry entry, string contents)
    {
        ExplorerEntry = entry;
    }

    public async void Save()
    {
        if (ExplorerEntry == null)
        {
            SaveAs();
        }
    }

    public async void SaveAs()
    {
        MainWindow parentWindow = (MainWindow)TopLevel.GetTopLevel(this);

        Uri.TryCreate("file://" + parentWindow.WorkspaceManager.WorkspacePath, UriKind.Absolute, out var uri);        
        IStorageFolder folder = await parentWindow.StorageProvider.TryGetFolderFromPathAsync(uri);    

        IStorageFile? file = await parentWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save File As...",
            FileTypeChoices = new List<FilePickerFileType>
            {
                new FilePickerFileType(FileTypeDescription)
                {
                    Patterns = new[] { "*" + FileExtension }
                },
                new FilePickerFileType("All Files")
                {
                    Patterns = new[] { "*" }
                }
            },
            
            SuggestedStartLocation = folder,
            SuggestedFileName = ExplorerEntry?.Name ?? "Untitled" + FileExtension
        });

        if (file is not null)
        {
            string? path = file.TryGetLocalPath();

            if (path is not null)
            {
                ExplorerEntry newEntry = new ExplorerEntry
                {
                    Name = System.IO.Path.GetFileName(path),
                    Path = path,
                };

                ExplorerEntry = newEntry;

                System.IO.File.WriteAllText(path, EncodeFile());

                parentWindow.WorkspaceManager.UpdatePath();

                foreach (ExplorerEntry entry in parentWindow.ExplorerControl.Entries)
                {
                    if (entry.Path == ExplorerEntry?.Path)
                    {
                        this.ExplorerEntry = newEntry;
                        this.IsModified = false;
                        TitleUpdate?.Invoke(entry.Name);
                    }
                }
            }
            
        }

    }
}