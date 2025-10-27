using Avalonia.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BusLab;


public class NetworkListEntry
{
    public uint ID { get; set; } = 0;
    public string RxTx { get; set; } = "";
    public byte DLC { get; set; } = 0;
    public string Message { get; set; } = "";
    public string Data { get; set; } = "";
    public double CycleTime { get; set; } = 0.0;

}

public partial class NetworkMessagesPanel: UserControl
{

    public ObservableCollection<NetworkListEntry> NetworkList { get; }

    public NetworkMessagesPanel()
    {
        InitializeComponent();

        var network = new List<NetworkListEntry> 
        {
            new NetworkListEntry(),
            new NetworkListEntry()
        };

        NetworkList = new ObservableCollection<NetworkListEntry>(network);
        
        this.DataContext = this;

    }
}