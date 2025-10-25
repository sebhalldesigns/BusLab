using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BusLab;

public static class DatabaseWriter
{
    public static string? Write(string filePath, CanDatabase database, out string error, out string detailedError)
    {
        string outPath = filePath;
        error = "";
        detailedError = "";

        List<string> lines = new List<string>();

        WriteVersion(lines, database);
        WriteNamespaceSymbols(lines, database);
        WriteBusSpeed(lines, database);
        WriteNodes(lines, database);
        WriteMessages(lines, database);
        WriteComments(lines, database);

        WriteSignalValueTypes(lines, database);

        lines.Add("");

        try
        {
            File.WriteAllLines(filePath, lines);
        }
        catch (Exception e)
        {
            error = "Failed to write file!";
            detailedError = e.ToString();
            return null;
        }

        return outPath;
    }

    private static void WriteVersion(List<string> lines, CanDatabase database)
    {
        lines.Add($"VERSION \"{database.Version}\"");

        /* For some reason we add two lines here */
        lines.Add("");
        lines.Add("");
    }

    private static void WriteNamespaceSymbols(List<string> lines, CanDatabase database)
    {
        /*
        ** I don't like this space inconsistency between NS_ : and BS_:
        ** But keep to what most examples seem to use. 
        */
        lines.Add($"NS_ :");

        foreach (string symbol in database.NamespaceSymbols)
        {
            lines.Add($"\t{symbol}");
        }

        lines.Add("");

    }

    private static void WriteBusSpeed(List<string> lines, CanDatabase database)
    {
        lines.Add($"BS_: {database.BusSpeed}");
        lines.Add("");
    }

    private static void WriteNodes(List<string> lines, CanDatabase database)
    {
        string line = "BU_:";

        foreach (CanDatabaseNode node in database.Nodes)
        {
            line += $" {node.Name}";
        }

        lines.Add(line);

        /* For some reason we add two lines here */
        lines.Add("");
        lines.Add("");
    }
    
    private static void WriteMessages(List<string> lines, CanDatabase database)
    {
        foreach (CanDatabaseMessage message in database.Messages)
        {

            /* default sender is Vector__XXX if none specified */
            string sender = message.SenderNode;

            if (string.IsNullOrEmpty(sender))
            {
                sender = "Vector__XXX";
            }

            string messageLine = $"BO_ {message.ID} {message.Name}: {message.Length} {sender}";
            lines.Add(messageLine);

            foreach (CanDatabaseSignal signal in message.Signals)
            {
                string byteOrder = signal.ByteOrder == CanDatabaseSignalByteOrder.LITTLE_ENDIAN ? "1" : "0";

                /* N.B. Signals that aren't UNSIGNED are signed - including floats and doubles */
                string signalType = signal.SignalType == CanDatabaseSignalType.UNSIGNED ? "+" : "-";
                
                string receiver = signal.ReceiverNode;
                if (string.IsNullOrEmpty(receiver))
                {
                    receiver = "Vector__XXX";
                }

                string signalLine = $" SG_ {signal.Name} : {signal.StartBit}|{signal.BitLength}@{byteOrder}{signalType} ({signal.Scale},{signal.Offset}) [{signal.Minimum}|{signal.Maximum}] \"{signal.Unit}\" {receiver}";

                lines.Add(signalLine);
            }

            lines.Add("");
        }
    }

    private static void WriteComments(List<string> lines, CanDatabase database)
    {
        foreach (string comment in database.GlobalComments)
        {
            lines.Add($"CM_ \"{comment}\";");
        }

        foreach (CanDatabaseNode node in database.Nodes)
        {
            if (!string.IsNullOrEmpty(node.Comment))
            {
                lines.Add($"CM_ BU_ {node.Name} \"{node.Comment}\";");
            }
        }  

        foreach (CanDatabaseMessage message in database.Messages)
        {
            if (!string.IsNullOrEmpty(message.Comment))
            {
                lines.Add($"CM_ BO_ {message.ID} \"{message.Comment}\";");
            }
        }

        foreach (CanDatabaseMessage message in database.Messages)
        {
            foreach (CanDatabaseSignal signal in message.Signals)
            {
                if (!string.IsNullOrEmpty(signal.Comment))
                {
                    lines.Add($"CM_ SG_ {message.ID} {signal.Name} \"{signal.Comment}\";");
                }
            }
        }
    }

    private static void WriteSignalValueTypes(List<string> lines, CanDatabase database)
    {
        foreach (CanDatabaseMessage message in database.Messages)
        {
            foreach (CanDatabaseSignal signal in message.Signals)
            {
                if (signal.SignalType == CanDatabaseSignalType.FLOAT || signal.SignalType == CanDatabaseSignalType.DOUBLE)
                {
                    string valueType = signal.SignalType == CanDatabaseSignalType.FLOAT ? "1" : "2";
                    string signalValueTypeLine = $"SIG_VALTYPE_ {message.ID} {signal.Name} : {valueType};";
                    lines.Add(signalValueTypeLine);
                }
            }
        }

    }

}