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
        return new Document
        {
            Title = $"Select Panel Type",
            Content = new BlankPanel(),
            CanFloat = false
        };
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
        File.WriteAllText("layout.json", json);
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
                        ? 1.0  // CLAMP Infinity to 1.0
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

        Console.WriteLine("Built documents. " + JsonSerializer.Serialize(result));

        return result;
    }
}