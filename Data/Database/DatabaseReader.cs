using System;
using System.IO;
using System.Text.RegularExpressions;

namespace BusLab;



public static class DatabaseReader
{
    private enum ParseState
    {
        NONE,
        NAMESPACES,
        NODES,
        MESSAGE,
        GLOBAL_COMMENT,
        NODE_COMMENT,
        MESSAGE_COMMENT,
        SIGNAL_COMMENT
    }

    /* parsing states */
    private static ParseState parseState = ParseState.NONE;
    private static string parseError = "";

    /* track for comments */
    private static CanDatabaseNode? currentNode = null;
    private static CanDatabaseMessage? currentMessage = null;
    private static CanDatabaseSignal? currentSignal = null;

    public static CanDatabase? Read(string filePath, out string error, out string detailedError)
    {
        CanDatabase database = new CanDatabase();
        error = "";
        detailedError = "";

        parseState = ParseState.NONE;
        parseError = "";
        currentNode = null;
        currentMessage = null;
        currentSignal = null;

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
                return null;
            }

        }

        return database;
    }

    private static void ParseLine(string line, CanDatabase database)
    {
        string[] tokens = line.Trim().Split(' ');

        bool parsed = false;

        if (tokens.Length > 0 && parseState == ParseState.NONE)
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

                case "BO_":
                {
                    ParseMessageSignal(line, tokens, database);
                    parsed = true;
                } break;

                case "CM_":
                {
                    ParseComment(line, tokens, database);
                    parsed = true;
                } break;

                case "SIG_VALTYPE_":
                {
                    ParseSignalValueType(line, database);
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

                case ParseState.MESSAGE:
                {
                    ParseMessageSignal(line, tokens, database);
                } break;

                case ParseState.GLOBAL_COMMENT:
                case ParseState.NODE_COMMENT:
                case ParseState.MESSAGE_COMMENT:
                case ParseState.SIGNAL_COMMENT:
                {
                    ParseComment(line, tokens, database);
                } break;

                default:
                {
                    /* empty line */
                } break;
            }
        }
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

    private static void ParseMessageSignal(string line, string[] tokens, CanDatabase database)
    {
        if (tokens.Length > 0 && tokens[0] == "BO_")
        {
            ParseMessage(line, database);
            parseState = ParseState.MESSAGE;
        }
        else if (tokens.Length > 0 && tokens[0] == "SG_" && parseState == ParseState.MESSAGE)
        {
            ParseSignal(line, database);
        }
        else
        {
            /* reached end of message */
            parseState = ParseState.NONE;
        }
    }

    private static void ParseMessage(string line, CanDatabase database)
    {

        CanDatabaseMessage message = new CanDatabaseMessage();

        Match match = Regex.Match(line, @"^\s*BO_\s+(\d+)\s+(.+?):\s+(\d+)\s+(\S+)\s*$");

        if (!match.Success)
        {
            parseError = $"Unable to parse message line: {line}";
        }
        else
        {
            /* ID field */
            string idStr = match.Groups[1].Value;

            if (!uint.TryParse(idStr, out uint id))
            {
                parseError = $"Invalid message ID '{idStr}' in line: {line}";
            }
            else
            {
                message.ID = id;
            }

            /* message name */
            message.Name = match.Groups[2].Value.Trim();

            /* DLC */
            string dlcStr = match.Groups[3].Value;
            if (!byte.TryParse(dlcStr, out byte dlc))
            {
                parseError = $"Invalid DLC '{dlcStr}' in line: {line}";
            }
            else
            {
                message.Length = dlc;
            }

            /* sender node */
            message.SenderNode = match.Groups[4].Value;

            /* Handle Vector__XXX as no sender */
            if (message.SenderNode == "Vector__XXX")
            {
                message.SenderNode = ""; 
            }
        }

        database.Messages.Add(message);
    }
    
    private static void ParseSignal(string line, CanDatabase database)
    {
        CanDatabaseSignal signal = new CanDatabaseSignal();

        Match match = Regex.Match(line, @"^\s*SG_\s+(\w+)\s*:\s*(\d+)\|(\d+)@(\d)([+-])\s+\(([^,]+),([^)]+)\)\s+\[([^\|]+)\|([^\]]+)\]\s+""([^""]*)""\s+(\S+)\s*$");

        if (!match.Success)
        {
            parseError = $"Unable to parse signal line: {line}";
        }
        else
        {
            /* signal name */
            signal.Name = match.Groups[1].Value.Trim();

            /* start bit */
            string startBitStr = match.Groups[2].Value;
            if (!uint.TryParse(startBitStr, out uint startBit))
            {
                parseError = $"Invalid start bit '{startBitStr}' in line: {line}";
            }
            else
            {
                signal.StartBit = startBit;
            }

            /* signal length */
            string lengthStr = match.Groups[3].Value;
            if (!uint.TryParse(lengthStr, out uint length))
            {
                parseError = $"Invalid signal length '{lengthStr}' in line: {line}";
            }
            else
            {
                signal.BitLength = length;
            }

            /* byte order */
            string byteOrderStr = match.Groups[4].Value;
            if (byteOrderStr == "0")
            {
                signal.ByteOrder = CanDatabaseSignalByteOrder.BIG_ENDIAN;
            }
            else if (byteOrderStr == "1")
            {
                signal.ByteOrder = CanDatabaseSignalByteOrder.LITTLE_ENDIAN;
            }
            else
            {
                parseError = $"Invalid byte order '{byteOrderStr}' in line: {line}";
            }

            /* signal type */
            string typeStr = match.Groups[5].Value;
            if (typeStr == "+")
            {
                signal.SignalType = CanDatabaseSignalType.UNSIGNED;
            }
            else if (typeStr == "-")
            {
                signal.SignalType = CanDatabaseSignalType.SIGNED;
            }
            else
            {
                parseError = $"Invalid signal type '{typeStr}' in line: {line}";
            }

            /* scale */
            string scaleStr = match.Groups[6].Value;
            signal.Scale = scaleStr;

            /* offset */
            string offsetStr = match.Groups[7].Value;
            signal.Offset = offsetStr;

            /* min */
            string minStr = match.Groups[8].Value;
            signal.Minimum = minStr;

            /* max */
            string maxStr = match.Groups[9].Value;
            signal.Maximum = maxStr;

            /* unit */
            string unitStr = match.Groups[10].Value;
            signal.Unit = unitStr;

            /* receiver node */
            string receiverStr = match.Groups[11].Value;
            signal.ReceiverNode = receiverStr;

            /* again, handle Vector__XXX as no receiver */
            if (signal.ReceiverNode == "Vector__XXX")
            {
                signal.ReceiverNode = "";
            }
        }

        /* add signal to last message */
        if (database.Messages.Count > 0)
        {
            CanDatabaseMessage lastMessage = database.Messages[database.Messages.Count - 1];
            lastMessage.Signals.Add(signal);
        }
        else
        {
            parseError = $"Signal defined before any message in line: {line}";
        }
    }

    private static void ParseComment(string line, string[] tokens, CanDatabase database)
    {
        if (parseState == ParseState.NONE)
        {
            bool isEndOfComment = line.Trim().EndsWith(";");
            Match match = Regex.Match(line, @"^\s*CM_\s+(?:(BU_|BO_|SG_)\s+(?:(\d+)\s+)?(\w+)?)?\s*""(.*?)""?\s*;?\s*$");
            

            if (!match.Success)
            {
                parseError = $"Unable to parse CM_ line: {line}";

                return;
            }
            else
            {
                string comment = match.Groups[4].Value;

                if (!match.Groups[1].Success)
                {
                    /* global comment */
                    database.GlobalComments.Add(comment);

                    if (!isEndOfComment)
                    {
                        parseState = ParseState.GLOBAL_COMMENT;
                    }
                    return;
                }
                else if (match.Groups[1].Value == "BU_")
                {
                    /* Node comment */
                    string nodeName = match.Groups[3].Value;

                    foreach (CanDatabaseNode node in database.Nodes)
                    {
                        if (node.Name == nodeName)
                        {
                            node.Comment = comment;

                            if (!isEndOfComment)
                            {
                                currentNode = node;
                                parseState = ParseState.NODE_COMMENT;
                            }

                            return;
                        }
                    }

                    parseError = $"Could not find node '{nodeName}' for CM_ line: {line}";
                }
                else if (match.Groups[1].Value == "BO_")
                {
                    /* Message comment */
                    string messageIdStr = match.Groups[2].Value;
                    
                    if (!uint.TryParse(messageIdStr, out uint messageId))
                    {
                        parseError = $"Invalid message ID '{messageIdStr}' in line: {line}";
                        return;
                    }

                    foreach (CanDatabaseMessage message in database.Messages)
                    {
                        if (message.ID == messageId)
                        {
                            message.Comment = comment;

                            if (!isEndOfComment)
                            {
                                currentMessage = message;
                                parseState = ParseState.MESSAGE_COMMENT;
                            }

                            return;
                        }
                    }

                    parseError = $"Could not find message ID {messageId} for CM_ line: {line}";
                    
                }
                else if (match.Groups[1].Value == "SG_")
                {
                    /* Signal comment */

                    string messageIdStr = match.Groups[2].Value;
                    
                    string signalName = match.Groups[3].Value;
                    if (!uint.TryParse(messageIdStr, out uint messageId))
                    {
                        parseError = $"Invalid message ID '{messageIdStr}' in line: {line}";
                        return;
                    }

                    foreach (CanDatabaseMessage message in database.Messages)
                    {
                        if (message.ID == messageId)
                        {
                            foreach (CanDatabaseSignal signal in message.Signals)
                            {
                                if (signal.Name == signalName)
                                {
                                    signal.Comment = comment;

                                    if (!isEndOfComment)
                                    {
                                        currentSignal = signal;
                                        parseState = ParseState.SIGNAL_COMMENT;
                                    }

                                    return;
                                }
                            }
                        }
                    }

                    parseError = $"Could not find signal '{signalName}' in message ID {messageId} for CM_ line: {line}";
                }
                else 
                {
                    parseError = $"Unable to parse CM_ line: {line}";
                }
            }
        }
        else
        {
            bool isEndOfComment = line.Trim().EndsWith(";");

            Match match = Regex.Match(line, @"^\s*([^""]*)");
            if (match.Success)
            {
                string commentPart = "\n" + match.Groups[1].Value;

                if (parseState == ParseState.NODE_COMMENT && currentNode != null)
                {
                    currentNode.Comment += commentPart;
                }
                else if (parseState == ParseState.MESSAGE_COMMENT && currentMessage != null)
                {
                    currentMessage.Comment += commentPart;
                }
                else if (parseState == ParseState.SIGNAL_COMMENT && currentSignal != null)
                {
                    currentSignal.Comment += commentPart;
                }
                else if (parseState == ParseState.GLOBAL_COMMENT && database.GlobalComments.Count > 0)
                {
                    database.GlobalComments[database.GlobalComments.Count - 1] += commentPart;
                }
            }

            if (isEndOfComment)
            {
                parseState = ParseState.NONE;
                currentNode = null;
                currentMessage = null;
                currentSignal = null;
            }
        }
        
    }


    private static void ParseSignalValueType(string line, CanDatabase database)
    {
        Match match = Regex.Match(line, @"^\s*SIG_VALTYPE_\s+(\d+)\s+(\w+)\s+:\s+(\d+)\s*;\s*$");

        if (!match.Success)
        {
            parseError = $"Unable to parse SIG_VALTYPE_ line: {line}";
        }
        else
        {
            /* message ID */
            string messageIdStr = match.Groups[1].Value;
            if (!uint.TryParse(messageIdStr, out uint messageId))
            {
                parseError = $"Invalid message ID '{messageIdStr}' in line: {line}";
                return;
            }

            /* signal name */
            string signalName = match.Groups[2].Value.Trim();

            /* value type */
            string valueTypeStr = match.Groups[3].Value;
            CanDatabaseSignalType signalType = CanDatabaseSignalType.UNSIGNED;
            bool overrideFound = false;

            switch (valueTypeStr)
            {
                case "0":
                {
                    /* ignore, default signal type */
                } break;

                case "1":
                {
                    signalType = CanDatabaseSignalType.FLOAT;
                    overrideFound = true;
                } break;

                case "2":
                {
                    signalType = CanDatabaseSignalType.DOUBLE;
                    overrideFound = true;
                } break;

                default:
                {
                    parseError = $"Invalid value type '{valueTypeStr}' in line: {line}";
                } break;
            }

            if (!overrideFound)
            {
                return;
            }

            /* find the message and signal to apply this to */
            foreach (CanDatabaseMessage message in database.Messages)
            {
                if (message.ID == messageId)
                {
                    foreach (CanDatabaseSignal signal in message.Signals)
                    {
                        if (signal.Name == signalName)
                        {
                            signal.SignalType = signalType;
                            return;
                        }
                    }
                }
            }

            parseError = $"Could not find signal '{signalName}' in message ID {messageId} for SIG_VALTYPE_ line: {line}";
        }
    }
}