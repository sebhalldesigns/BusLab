using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using System.Text;

using Avalonia;
using Avalonia.Styling;
using Avalonia.Controls;
using Dock.Model.Controls;
using Dock.Model.Avalonia;
using Dock.Model.Avalonia.Controls;
using Dock.Model.Core;
using Dock.Avalonia.Controls;


namespace BusLab;

public class TabFactory: Factory
{
    IRootDock? root = null;

    public BlankPanel? activePanel = null;

    public TabFactory()
    {
        DockableAdded += (_, args) =>
        {
            if (args.Dockable is DocumentDock)
            {
                DocumentDock dock = (DocumentDock)args.Dockable;
                
                dock.DocumentFactory = CreateTab;
                dock.IsCollapsable = true;
                dock.CanCreateDocument = true;
                dock.CanFloat = false;
                dock.MinHeight = 100;
                dock.MinWidth = 100;
            }
        };

        DockableActivated += (_, args) =>
        {
            if (args.Dockable is Document doc && doc.Content is BlankPanel panel)
            {
                activePanel = panel;
            }
        };
    }

    public override IRootDock CreateLayout()
    {
        root = CreateRootDock();

        DocumentDock dock = new DocumentDock();
        dock.DocumentFactory = CreateTab;
        dock.CanCreateDocument = true;
        dock.IsCollapsable = true;
        dock.CanFloat = false;
        dock.MinHeight = 100;
        dock.MinWidth = 100;

        Document? startDocument = dock.DocumentFactory() as Document;
        
        if (startDocument != null)
        {
            dock.VisibleDockables = CreateList<IDockable>(startDocument);
            dock.ActiveDockable = startDocument;
        }

        ProportionalDock mainLayout = new ProportionalDock
        {
            Orientation = Orientation.Horizontal,
            VisibleDockables = CreateList<IDockable>(dock)
        };

        root.VisibleDockables = CreateList<IDockable>(mainLayout);
        root.DefaultDockable = mainLayout;

        return root;
    }

    private Document CreateTab()
    {
        Document doc = new Document
        {
            Title = $"Select Panel Type",
            CanFloat = false
        };

        BlankPanel panel = new BlankPanel(doc);
        doc.Content = panel;
        activePanel = panel;

        return doc;
    }

    public string GetJsonLayout()
    {
        if (root == null) return string.Empty;

        foreach (IDockable dockable in root.VisibleDockables ?? Array.Empty<IDockable>())
        {
            if (dockable is ProportionalDock)
            {
                ProportionalDock propDock = (ProportionalDock)dockable;
                foreach (IDockable childDockable in propDock.VisibleDockables ?? Array.Empty<IDockable>())
                {
                    if (childDockable is DocumentDock)
                    {
                        DocumentDock docDock = (DocumentDock)childDockable;
                        foreach (IDockable document in docDock.VisibleDockables ?? Array.Empty<IDockable>())
                        {
                            if (document is Document)
                            {
                                Document doc = (Document)document;
                                Console.WriteLine($"Document Title: {doc.Title}");
                            }
                        }
                    }
                }
            }
        }

        Console.WriteLine("=== ALL TABS IN LAYOUT ===");
        PrintTabsRecursive(root.VisibleDockables);
        Console.WriteLine("=== END ===");

        var layout = new LayoutData
        {
            Id = root.Id,
            Type = "RootDock",
            VisibleDockables = BuildDockables(root.VisibleDockables)
        };

        JsonSerializerOptions options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        string json = JsonSerializer.Serialize(layout, options);
        Console.WriteLine("Manual layout saved");
        return json;
    }

    private List<DockableDataBase> BuildDockables(IList<IDockable>? dockables)
    {
        Console.WriteLine("Building dockables...");

        var result = new List<DockableDataBase>();
        if (dockables == null) return result;

        foreach (var dockable in dockables)
        {
            if (dockable is DocumentDock docDock)
            {
                var dockData = new DockableData
                {
                    Id = docDock.Id,
                    Type = "DocumentDock",
                    Title = docDock.Title,
                    CanCreateDocument = docDock.CanCreateDocument,
                    Documents = BuildDocuments(docDock.VisibleDockables)
                };
                result.Add(dockData);
            }
            else if (dockable is ProportionalDock propDock)
            {
                var dockData = new DockableData
                {
                    Id = propDock.Id,
                    Type = "ProportionalDock",
                    Orientation = propDock.Orientation.ToString(),
                    VisibleDockables = BuildDockables(propDock.VisibleDockables)
                };
                result.Add(dockData);
            }
            else if (dockable is ProportionalDockSplitter splitter)
            {
                var splitterData = new SplitterData
                {
                    Type = "Splitter",
                    Proportion = double.IsInfinity(splitter.Proportion) || double.IsNaN(splitter.Proportion) 
                        ? 0.5  // CLAMP Infinity to 1.0
                        : splitter.Proportion
                };
                result.Add(splitterData);
            }
        }

        return result;
    }

    private List<DocumentData> BuildDocuments(IList<IDockable>? documents)
    {
        var result = new List<DocumentData>();
        if (documents == null) return result;

        foreach (var doc in documents)
        {
            if (doc is Document document)
            {
                result.Add(new DocumentData
                {
                    Id = document.Id,
                    Title = document.Title,
                    IsActive = ReferenceEquals(document, (document.Parent as DocumentDock)?.ActiveDockable),
                    Content = document.Content?.GetType().Name
                });
            }
        }

        Console.WriteLine("Built documents. " + JsonSerializer.Serialize(result) + " of " + documents.Count);

        return result;
    }

    
    public IRootDock LoadRootFromJson(string json)
    {
        var layoutData = JsonSerializer.Deserialize<LayoutData>(json);
        if (layoutData == null)
            throw new InvalidOperationException("Failed to deserialize layout data.");

        var rootDock = new RootDock
        {
            Id = layoutData.Id
        };

        if (layoutData.VisibleDockables.Count > 0)
        {
            rootDock.VisibleDockables = RebuildDockables(layoutData.VisibleDockables);
            rootDock.DefaultDockable = rootDock.VisibleDockables[0];
        }

        return rootDock;
    }

    private List<IDockable> RebuildDockables(List<DockableDataBase> dockableDataList)
    {
        var result = new List<IDockable>();

        foreach (var data in dockableDataList)
        {
            var dockable = RebuildFromData(data);
            result.Add(dockable);
        }

        return result;
    }

    private IDockable RebuildFromData(DockableDataBase data)
    {
        return data switch
        {
            DockableData { Type: "DocumentDock" } dd => BuildDocumentDock(dd),
            DockableData { Type: "ProportionalDock" } pd => BuildProportionalDock(pd),
            SplitterData sd => new ProportionalDockSplitter { Proportion = sd.Proportion },
            _ => throw new InvalidOperationException($"Unknown type: {data.Type}")
        };
    }

    private DocumentDock BuildDocumentDock(DockableData dd)
    {
        var dock = new DocumentDock 
        { 
            Id = dd.Id,
            Title = dd.Title,
            CanCreateDocument = dd.CanCreateDocument ?? true,
            DocumentFactory = CreateTab 
        };

        if (dd.Documents.Count > 0)
        {
            foreach (var docData in dd.Documents)
            {
                var doc = new Document
                {
                    Id = docData.Id,
                    Title = docData.Title
                };

                doc.Content = docData.Content switch
                {
                    "BlankPanel" => new BlankPanel(doc),
                    "NetworkMessagesPanel" => new NetworkMessagesPanel(),
                    "SignalPlotPanel" => new SignalPlotPanel(),
                    _ => new BlankPanel(doc)
                };
                
                dock.VisibleDockables.Add(doc);
            }
            
            for (int i = 0; i < dock.VisibleDockables.Count; i++)
            {
                if (dock.VisibleDockables[i] is Document doc && doc.Id == dd.Documents.Find(d => d.IsActive)?.Id)
                {
                    dock.ActiveDockable = dock.VisibleDockables[i];
                    break;
                }
            }
        }

        return dock;
    }

    private ProportionalDock BuildProportionalDock(DockableData pd)
    {
        var dock = new ProportionalDock
        {
            Id = pd.Id,
            Orientation = Enum.TryParse<Orientation>(pd.Orientation, out var ori) 
                        ? ori 
                        : Orientation.Horizontal
        };

        if (pd.VisibleDockables.Count > 0)
        {
            foreach (var child in pd.VisibleDockables)
                dock.VisibleDockables.Add(RebuildFromData(child));
        }

        return dock;
    }

    private void PrintTabsRecursive(IList<IDockable>? dockables, int indent = 0)
    {
        if (dockables == null) return;
        string prefix = new string(' ', indent * 2);

        foreach (var d in dockables)
        {
            switch (d)
            {
                case DocumentDock docDock:
                    Console.WriteLine($"{prefix}DocumentDock:");
                    foreach (Document doc in docDock.VisibleDockables ?? [])
                    {
                        string active = doc == docDock.ActiveDockable ? " [Active]" : "";
                        Console.WriteLine($"{prefix}  → \"{doc.Title}\"{active} ({doc.Content?.GetType().Name})");
                    }
                    break;

                case ProportionalDock propDock:
                    Console.WriteLine($"{prefix}ProportionalDock ({propDock.Orientation})");
                    PrintTabsRecursive(propDock.VisibleDockables, indent + 1);
                    break;

                case ProportionalDockSplitter splitter:
                    Console.WriteLine($"{prefix}Splitter ({splitter.Proportion})");
                    break;
            }
        }
    }
}