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
    public DocumentDock? activeDocumentDock = null;

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
                dock.MinHeight = 200;
                dock.MinWidth = 200;
            }
        };

        DockableActivated += (_, args) =>
        {
            if (args.Dockable is Document doc && doc.Content is BlankPanel panel)
            {
                activePanel = panel;
            }

        };

        DockableClosed += (_, args) =>
        {
            if (args.Dockable is Document doc)
            {
                ResetLayoutIfEmpty();   
            }

        };

        DockableMoved += (_, args) =>
        {
            if (args.Dockable is Document doc)
            {
                ResetLayoutIfEmpty();   
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
        dock.MinHeight = 200;
        dock.MinWidth = 200;

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
            Title = $"Create New...",
            CanFloat = false
        };

        BlankPanel panel = new BlankPanel(doc);
        doc.Content = panel;
        activePanel = panel;

        return doc;
    }

    private void ResetLayoutIfEmpty()
    {
        if (root == null) return;

        /* check if there are any open documents left */
        int rootDockables = root.VisibleDockables.Count;
        
        if (rootDockables == 0)
        {
            /* create new layout */
            DocumentDock dock = new DocumentDock();
            dock.DocumentFactory = CreateTab;
            dock.CanCreateDocument = true;
            dock.IsCollapsable = true;
            dock.CanFloat = false;
            dock.MinHeight = 200;
            dock.MinWidth = 200;

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
            
            InitLayout(root);
            return;
        }
    }

    public void AddNewTab()
    {
        if (root == null) return;

        if (root.ActiveDockable is ProportionalDock propDock)
        {
            Console.WriteLine("Adding new tab to active DocumentDock.");

            DocumentDock? docDock = null;
            
            docDock = activeDocumentDock;

            if (docDock == null)
            {
                foreach (IDockable dockable in propDock.VisibleDockables)
                {
                    if (dockable is DocumentDock && docDock == null)
                    {
                        docDock = dockable as DocumentDock;
                        break;
                    }
                }
            }

            if (propDock != null && docDock != null)
            {
                Document? newDoc = docDock.DocumentFactory() as Document;
                if (newDoc != null)
                {
                    List<IDockable> dockables = new List<IDockable>(docDock.VisibleDockables);
                    dockables.Add(newDoc);
                    docDock.VisibleDockables = dockables;
                    docDock.ActiveDockable = newDoc;
                }
            }   
            else
            {
                Console.WriteLine($"No active DocumentDock found. {propDock.ActiveDockable?.GetType().Name}");
            }
        }
        else 
        {
            Console.WriteLine($"No active DocumentDock found. {root.ActiveDockable?.GetType().Name}");
        }
    }
}