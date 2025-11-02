using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace BusLab;

public partial class DatabaseEditPanel: UserControl
{
    private Button[] tabButtons;
    private UserControl[] tabPanels;
    private UserControl[] propertiesPanels;
    private TitleUpdate titleUpdate;

    public CanDatabase Database { get; set; } = new CanDatabase();

    private MessagesEditPanel messagesEditPanel = new MessagesEditPanel();
    private MessagesPropertiesPanel messagesPropertiesPanel = new MessagesPropertiesPanel();

    public DatabaseEditPanel(TitleUpdate titleUpdate)
    {
        InitializeComponent();

        this.titleUpdate = titleUpdate;
        this.DataContext = this;

        tabButtons = new Button[]
        {
            MessagesButton,
            SignalsButton,
            NodesButton,
            OtherButton
        };

        tabPanels = new UserControl[]
        {
            messagesEditPanel,
            new MessagesEditPanel(),
            new MessagesEditPanel(),
            new MessagesEditPanel()
        };

        propertiesPanels = new UserControl[]
        {
            messagesPropertiesPanel,
            new MessagesPropertiesPanel(),
            new MessagesPropertiesPanel(),
            new MessagesPropertiesPanel()
        };

        for (int i = 0; i < tabButtons.Length; i++)
        {
            int index = i;
            tabButtons[i].Click += (s, e) => SelectTab(index);
        }

        SelectTab(0);
    }

    private void SelectTab(int tabIndex)
    {
        for (int i = 0; i < tabButtons.Length; i++)
        {
            if (i == tabIndex)
            {
                tabButtons[i].Background = new SolidColorBrush(Avalonia.Media.Color.FromArgb(48, 128, 128, 128));
                tabButtons[i].Opacity = 1.0;
            }
            else
            {
                tabButtons[i].Background = Avalonia.Media.Brushes.Transparent;
                tabButtons[i].Opacity = 0.5;
            }
        }

        TabContentControl.Content = tabPanels[tabIndex];
        PropertiesContentControl.Content = propertiesPanels[tabIndex];
    }

    public void LoadDatabase(string filePath)
    {
        // Load the database file and update the UI accordingly
        Console.WriteLine($"Loading database from: {filePath}");

        // Update the title of the tab
        titleUpdate?.Invoke(System.IO.Path.GetFileName(filePath));

        CanDatabase? database = DatabaseReader.Read(filePath, out string error, out string detailedError);
        
        if (database != null)
        {
            Console.WriteLine($"Database loaded successfully with {database.Messages.Count} messages.");
            
            messagesEditPanel.Database = database;
            
            // Update UI with database content
        }
        else
        {
            ErrorWindow errorWindow = new ErrorWindow(error, detailedError);
            errorWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            errorWindow.ShowDialog(TopLevel.GetTopLevel(this) as Window ?? new Window());
        }
    }
   

}
