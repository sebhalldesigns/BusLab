using System.Collections.Generic;

namespace BusLab;

public enum CanDatabaseAttributeType
{
    INT,
    FLOAT,
    STRING,
    ENUM
}

public enum CanDatabaseAttributeTarget
{
    NONE,
    SIGNAL,
    MESSAGE
}

public enum CanDatabaseSignalByteOrder
{
    BIG_ENDIAN,
    LITTLE_ENDIAN
}

public enum CanDatabaseSignalType
{
    UNSIGNED,
    SIGNED,
    FLOAT,
    DOUBLE
}

public class CanDatabaseNode
{
    public string Name { get; set; } = "";
    public string Comment { get; set; } = "";
}

public class CanDatabaseAttribute
{
    public string Name { get; set; } = "";
    public CanDatabaseAttributeType Type { get; set; }
    public CanDatabaseAttributeTarget Target { get; set; }

    public string Min { get; set; } = "";
    public string Max { get; set; } = "";

    public string StringValue { get; set; } = "";

    public List<string> EnumValues { get; set; } = new List<string>();
}

public class CanDatabaseAttributeValue
{
    public string Attribute { get; set; } = "";
    
    public double FloatValue { get; set; }
    
    public int IntValue { get; set; }
    
    public string StringValue { get; set; } = "";
    
    public string EnumValue { get; set; } = "";
}

public class CanDatabaseValueTable
{
    public Dictionary<double, string> Values { get; set; } = new Dictionary<double, string>();
}

public class CanDatabaseSignal
{
    public string Name { get; set; } = "";

    public uint StartBit { get; set; }
    public uint BitLength { get; set; }
    public CanDatabaseSignalByteOrder ByteOrder { get; set; }

    public CanDatabaseSignalType SignalType { get; set; }

    public bool IsMultiplexor { get; set; }

    public bool IsMultiplexed { get; set; }
    public string MultiplexValue { get; set; } = "";
    public string MultiplexorSignal { get; set; } = "";

    public string Scale { get; set; } = "";
    public string Offset { get; set; } = "";
    public string Minimum { get; set; } = "";
    public string Maximum { get; set; } = "";
    public string Unit { get; set; } = "";
    public string ReceiverNode { get; set; } = "";

    public string Comment { get; set; } = "";

    public List<CanDatabaseAttributeValue> AttributeValues { get; set; } = new List<CanDatabaseAttributeValue>();
    public List<CanDatabaseValueTable> ValueTable { get; set; } = new List<CanDatabaseValueTable>();
}

public class CanDatabaseMessage
{
    public string Name { get; set; } = "";
    public uint ID { get; set; }
    public bool Extended { get; set; }
    public byte Length { get; set; }
    public string SenderNode { get; set; } = "";

    public List<CanDatabaseSignal> Signals { get; set; } = new List<CanDatabaseSignal>();

    public string Comment { get; set; } = "";

    public List<CanDatabaseAttributeValue> AttributeValues { get; set; } = new List<CanDatabaseAttributeValue>();
}

public class CanDatabaseEnvironmentVariable
{
    public string Name { get; set; } = "";

    public CanDatabaseAttributeType Type { get; set; }

    public string Min { get; set; } = "";
    public string Max { get; set; } = "";

    public string Unit { get; set; } = "";
    public string InitialValue { get; set; } = "";
    public string AccessType { get; set; } = "";
    public string Node { get; set; } = "";
}

public class CanDatabase
{
    public string Version { get; set; } = "";
    public List<string> NamespaceSymbols { get; set; } = new List<string>();
    public string BusSpeed { get; set; } = "";
    public List<CanDatabaseNode> Nodes { get; set; } = new List<CanDatabaseNode>();
    public List<CanDatabaseMessage> Messages { get; set; } = new List<CanDatabaseMessage>();
    public List<CanDatabaseAttribute> Attributes { get; set; } = new List<CanDatabaseAttribute>();
    public List<CanDatabaseEnvironmentVariable> EnvironmentVariables = new List<CanDatabaseEnvironmentVariable>();
    public List<string> GlobalComments { get; set; } = new List<string>();
}