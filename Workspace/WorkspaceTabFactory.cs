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

        var layout = new LayoutData
        {
            Id = root.Id,
            Type = "RootDock",
            VisibleDockables = BuildDockables(root.VisibleDockables)
        };

        string json = JsonSerializer.Serialize(layout);
        File.WriteAllText("layout.json", json);
        Console.WriteLine("Manual layout saved");
        return json;
    }

    private List<IDockableData> BuildDockables(IList<IDockable>? dockables)
    {
        var result = new List<IDockableData>();
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
                    CanCreateDocument = docDock.CanCreateDocument
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
}