using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace BusLab.Workspace.Panels;

public partial class DatabasePanel: PanelBase
{

    private Button[] tabButtons;
    private UserControl[] tabPanels;
    private UserControl[] propertiesPanels;

    public CanDatabase Database { get; set; } = new CanDatabase();

    private MessagesEditPanel messagesEditPanel = new MessagesEditPanel();
    private MessagesPropertiesPanel messagesPropertiesPanel = new MessagesPropertiesPanel();

    public DatabasePanel()
    {
        InitializeComponent();

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

    public override void OpenFilePressed()
    {
        
    }

    public override void SaveFilePressed()
    {
        
    }


}
