using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.Interactivity;
using Avalonia.Media;
using System.ComponentModel;


namespace BusLab;

public partial class MessagesEditPanel: UserControl, INotifyPropertyChanged
{

    public CanDatabase database { get; set; }

    public CanDatabaseMessage? selectedMessage { get; set; }

    public CanDatabase Database
    {
        get => database;
        set
        {
            if (database != value)
            {
                database = value;
                OnPropertyChanged(nameof(Database));
            }
        }
    }

    public CanDatabaseMessage? SelectedMessage
    {
        get => selectedMessage;
        set
        {
            if (selectedMessage != value)
            {
                selectedMessage = value;
                OnPropertyChanged(nameof(SelectedMessage));
            }
        }
    }


    public MessagesEditPanel()
    {
        InitializeComponent();

        Database = new CanDatabase();
        
        this.DataContext = this;
    }

    public void Update()
    {
        this.DataContext = null;
        this.DataContext = this;

    }

    private void MessageSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (MessagesDataGrid.SelectedItem is CanDatabaseMessage message)
        {
            SelectedMessage = message;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));


}
