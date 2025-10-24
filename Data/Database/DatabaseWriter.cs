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


}