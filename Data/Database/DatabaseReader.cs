using System;
using System.IO;

namespace BusLab;



public static class DatabaseReader
{
    private enum ParseState
    {
        NONE,
        NAMESPACES,
        NODES
    }

    private static ParseState parseState = ParseState.NONE;

    private static string parseError = "";

    public static CanDatabase? Read(string filePath, out string error, out string detailedError)
    {
        CanDatabase database = new CanDatabase();
        error = "";
        detailedError = "";

        parseState = ParseState.NONE;
        parseError = "";

        string[] fileContents = { };

        try
        {
            fileContents = File.ReadAllLines(filePath);
        }
        catch (Exception e)
        {
            error = $"Failed to open file!";
            detailedError = e.ToString();
            return null;
        }

        for (int lineIndex = 0; lineIndex < fileContents.Length; lineIndex++)
        {
            string line = fileContents[lineIndex];

            ParseLine(line, database);

            if (parseError != "")
            {
                error = $"Error parsing file {filePath} at line {lineIndex}";
                detailedError = parseError;
            }

        }

        return database;
    }

    private static void ParseLine(string line, CanDatabase database)
    {
        string[] tokens = line.Trim().Split(' ');

        bool parsed = false;

        if (tokens.Length > 0)
        {
            switch (tokens[0])
            {
                case "VERSION":
                {
                    ParseVersion(tokens, database);
                    parsed = true;
                } break;

                case "NS_":
                case "NS_:":
                {
                    ParseNamespaces(tokens, database);
                    parsed = true;
                } break;

                case "BS_":
                case "BS_:":
                {
                    ParseSpeed(tokens, database);
                    parsed = true;
                } break;

                case "BU_":
                case "BU_:":
                {
                    ParseNodes(tokens, database);
                    parsed = true;
                } break;

                default:
                {
                    parsed = false;
                } break;
            }
        }

        if (!parsed)
        {
            /*
            ** We use states to entries that aren't contained in a single line.
            */
            switch (parseState)
            {
                case ParseState.NAMESPACES:
                {
                    ParseNamespaces(tokens, database);
                } break;

                case ParseState.NODES:
                {
                    ParseNodes(tokens, database);
                } break;

                default:
                {
                    /* empty line */
                } break;
            }
        }

        Console.WriteLine(line);
        Console.WriteLine(parseState);
    }

    private static void ParseVersion(string[] tokens, CanDatabase database)
    {
        if (tokens.Length > 1)
        {
            string versionString = tokens[1];

            if (versionString.StartsWith("\"") && versionString.EndsWith("\""))
            {
                versionString = versionString.TrimStart('\"');
                versionString = versionString.TrimEnd('\"');

                database.Version = versionString;
            }
        }

        parseState = ParseState.NONE;
        parseError = "";
    }

    private static void ParseNamespaces(string[] tokens, CanDatabase database)
    {
        if (tokens.Length > 0)
        {
            /*
            ** This handles the possible inconsistency between NS_ : and NS_:
            ** All examples seem to have a space after NS but I don't want to assume it's standard.
            **
            ** All tokens after this on this line will be added after nodes, until we get a line with no tokens.
            */

            int tokenPos = 0;

            if (tokens.Length >= 2 && tokens[0] == "NS_" && tokens[1] == ":")
            {
                tokenPos = 2;
                parseState = ParseState.NAMESPACES;
            }

            if (tokens.Length >= 1 && tokens[0] == "NS_:")
            {
                tokenPos = 1;
                parseState = ParseState.NAMESPACES;
            }

            if (parseState == ParseState.NAMESPACES)
            {
                for (int i = tokenPos; i < tokens.Length; i++)
                {
                    string token = tokens[i];

                    if (token.Trim() != "")
                    {
                        database.NamespaceSymbols.Add(token);
                    }
                }
            }

            if (tokens[0].Trim() == "")
            {
                /*
                ** We have reached a blank line, so escape this state. 
                */

                parseState = ParseState.NONE;
            }
        }
        else
        {
            /*
            ** We have reached a blank line, so escape this state. 
            */

            parseState = ParseState.NONE;
        }
    }

    private static void ParseSpeed(string[] tokens, CanDatabase database)
    {
        /* This is apparently a legacy function but we'll attempt to support it anyway */

        int tokenPos = 0;

        if (tokens.Length >= 2 && tokens[0] == "BS_" && tokens[1] == ":")
        {
            tokenPos = 2;
        }

        if (tokens.Length >= 1 && tokens[0] == "BS_:")
        {
            tokenPos = 1;
        }

        /* 
        ** Add all tokens after BS_( ): as a space separated string
        */
        if (tokenPos > 0)
        {
            string combined = "";

            for (int i = tokenPos; i < tokens.Length; i++)
            {
                if (i > tokenPos)
                {
                    combined += " ";
                }

                combined += tokens[i];
            }

            database.BusSpeed = combined;
        }
    }
    
    private static void ParseNodes(string[] tokens, CanDatabase database)
    {
        /*
        ** This functions the same as the namespaces, which may be overkill,
        ** allowing BU_: BU_ :, and allowing any layout after BU_:
        ** (i.e nodes on the same line or line after).
        */

        if (tokens.Length > 0)
        {

            int tokenPos = 0;

            if (tokens.Length >= 2 && tokens[0] == "BU_" && tokens[1] == ":")
            {
                tokenPos = 2;
                parseState = ParseState.NODES;
            }

            if (tokens.Length >= 1 && tokens[0] == "BU_:")
            {
                tokenPos = 1;
                parseState = ParseState.NODES;
            }

            if (parseState == ParseState.NODES)
            {
                for (int i = tokenPos; i < tokens.Length; i++)
                {
                    string token = tokens[i];

                    if (token.Trim() != "")
                    {
                        CanDatabaseNode node = new CanDatabaseNode();
                        node.Name = token;
                        database.Nodes.Add(node);
                    }
                }
            }

            if (tokens[0].Trim() == "")
            {
                /*
                ** We have reached a blank line, so escape this state. 
                */

                parseState = ParseState.NONE;
            }
        }
        else
        {
            /*
            ** We have reached a blank line, so escape this state. 
            */

            parseState = ParseState.NONE;
        }


    }


}