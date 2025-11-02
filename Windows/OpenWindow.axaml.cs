using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Interactivity;
using System.Collections.Generic;

namespace BusLab;

public partial class OpenWindow : Window
{
    public OpenWindow()
    {
        InitializeComponent();
    
    }

    public async void ListBoxSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (TabListBox == null)
            return;

        switch (TabListBox.SelectedIndex)
        {
            case 0:
                break;
            case 1:
                this.IsVisible = false;
                OpenFile();
                break;
            case 2:
                this.IsVisible = false;
                OpenFolder();
                break;
            default:
                break;
        }
    }

    private async void OpenFile()
    {
        IReadOnlyList<IStorageFile> files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open File",
            AllowMultiple = false,
            FileTypeFilter = new List<FilePickerFileType>
            {
                new FilePickerFileType("CAN Database Files")
                {
                    Patterns = new[] { "*.dbc" }
                },
                new FilePickerFileType("All Files")
                {
                    Patterns = new[] { "*.*" }
                }
            }
        });

        Close();

    }

    private async void OpenFolder()
    {
        IReadOnlyList<IStorageFolder> folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Open Folder",
            AllowMultiple = false
        });

        Close();
    }
}