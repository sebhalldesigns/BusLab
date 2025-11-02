namespace BusLab.Data.Items;

public enum NetDataSourceType
{
    None,
    Peak,
    Vector,
    Kvaser,
    Logfile
}

public class NetItem
{
    public string ID { get; set; } = "";
    public string Name { get; set; } = "";
    public string Comments { get; set; } = "";

    public NetDataSourceType DataSourceType { get; set; } = NetDataSourceType.None;
}

public class NetDataSource
{
    public bool Connect()
    {
        return true;
    }

    public bool Disconnect()
    {
        return true;
    }
}