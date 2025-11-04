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
using Dock.Model.Core.Events;
using Dock.Model;
using Dock.Avalonia.Controls;

using System.Linq;

namespace BusLab.Workspace;

public class TabFactory: Factory
{
    IRootDock? root = null;

    public PanelContainer? ActivePanel { get; private set; }
    public DocumentDock? ActiveDocumentDock { get; private set; }

    public event EventHandler<PanelContainer?>? ActivePanelChanged;

    public List<PanelContainer> ActivePanelContainers { get; private set; }

    public TabFactory()
    {

        ActivePanelContainers = new List<PanelContainer>();

        ActiveDockableChanged   += OnActiveDockableChanged;
        DockableClosed      += (_, __) => ResetIfEmpty();
        DockableMoved       += (_, __) => ResetIfEmpty();
        DockableRemoved     += (_, e) =>
        {
            if (e.Dockable is Document doc && doc.Content is PanelContainer panel)
            {
                ActivePanelContainers.Remove(panel);
            }
            
            ResetIfEmpty();
        };

        DockableInit       += (_, e) =>
        {
            if (e.Dockable is DocumentDock documentDock)
            {
                documentDock.CanCreateDocument = false;
                documentDock.DocumentFactory = null;
                documentDock.IsCollapsable = true;
                documentDock.CanFloat = false;
                documentDock.MinHeight = 200;
                documentDock.MinWidth = 200;
            }
            
            if (e.Dockable is Document doc && doc.Content is PanelContainer panel && !ActivePanelContainers.Contains(panel))
            {
                ActivePanelContainers.Add(panel);
            }
        };

    }

    #region Layout Creation

    public override IRootDock CreateLayout()
    {

        Document startDoc = CreateDocument("BusLab");

        DocumentDock docDock = new DocumentDock
        {
            VisibleDockables = CreateList<IDockable>(startDoc),
            ActiveDockable = startDoc,
            CanCreateDocument = false,
            IsCollapsable = true,
            CanFloat = false,
            MinHeight = 200,
            MinWidth = 200
        };

        ProportionalDock main = new ProportionalDock
        {
            Orientation = Orientation.Horizontal,
            VisibleDockables = CreateList<IDockable>(docDock)
        }; 

        root = CreateRootDock();
        root.VisibleDockables = CreateList<IDockable>(main);
        root.DefaultDockable = main;

        return root;
    }

    public override void InitLayout(IDockable? layout)
    {
        base.InitLayout(layout);
        if (layout is IRootDock rootDock)
        {
            root = rootDock;
        }

        UpdateActiveDocumentDock();
    }

    #endregion

    #region Public API

    public PanelContainer? AddNewPanel(string title = "BusLab")
    {
        if (root?.ActiveDockable is not ProportionalDock prop) return null;

        DocumentDock targetDock = ActiveDocumentDock ?? FindFirstDocumentDock(prop) ?? CreateNewDocumentDock(prop);

        Document newDoc = CreateDocument(title);

        targetDock.VisibleDockables ??= new List<IDockable>();
        targetDock.VisibleDockables.Add(newDoc);
        targetDock.ActiveDockable = newDoc;

        ActiveDocumentDock = targetDock;

        return newDoc.Content as PanelContainer;
    }

    public void ActivateDocument(Document doc)
    {
        void SearchAndActivate(IDockable node)
        {
            switch (node)
            {
                case DocumentDock dd:
                    if (dd.VisibleDockables?.Contains(doc) == true)
                    {
                        dd.ActiveDockable = doc;
                        if (dd.Owner is IDock ownerDock) ownerDock.ActiveDockable = dd;
                        ActiveDocumentDock = dd;
                        return;
                    }
                    break;

                case ProportionalDock pd:
                    foreach (var child in pd.VisibleDockables ?? Enumerable.Empty<IDockable>())
                        SearchAndActivate(child);
                    break;

                case IRootDock rd:
                    foreach (var child in rd.VisibleDockables ?? Enumerable.Empty<IDockable>())
                        SearchAndActivate(child);
                    break;
            }
        }

        if (root != null)
            SearchAndActivate(root);

        ActivePanel = doc.Content as PanelContainer;
        ActivePanelChanged?.Invoke(this, ActivePanel);
    }

    #endregion

    #region Helper Methods

    private void OnActiveDockableChanged(object? s, ActiveDockableChangedEventArgs e)
    {
        if (e.Dockable is Document doc && doc.Content is PanelContainer panel)
        {
            ActivePanel = panel;
            ActivePanelChanged?.Invoke(this, panel);
        }

        if (e.Dockable is DocumentDock dd)
        {
            ActiveDocumentDock = dd;
        }
    }

    private Document CreateDocument(string title)
    {
        Document doc = new Document
        {
            Title = title,
            CanFloat = false
        };

        PanelContainer panel = new PanelContainer(doc);
        doc.Content = panel;
        ActivePanel = panel;

        return doc;
    }

    private DocumentDock? FindFirstDocumentDock(ProportionalDock parent)
    {
        foreach (var d in parent.VisibleDockables ?? Enumerable.Empty<IDockable>())
        {
            if (d is DocumentDock dd) return dd;
            if (d is ProportionalDock child) 
            {
                var found = FindFirstDocumentDock(child);
                if (found != null) return found;
            }
        }
        return null;
    }

    private DocumentDock CreateNewDocumentDock(ProportionalDock parent)
    {
        var emptyDoc = CreateDocument("BusLab");
        var newDock = new DocumentDock
        {
            VisibleDockables  = CreateList<IDockable>(emptyDoc),
            ActiveDockable    = emptyDoc,
            CanCreateDocument = false,
            IsCollapsable     = true,
            CanFloat          = false,
            MinWidth = 200,
            MinHeight = 200
        };

        parent.VisibleDockables ??= new List<IDockable>();
        parent.VisibleDockables.Add(newDock);
        parent.ActiveDockable = newDock;
        return newDock;
    }

    private void UpdateActiveDocumentDock()
    {
        ActiveDocumentDock = null;
        if (root?.ActiveDockable is ProportionalDock p)
            ActiveDocumentDock = FindFirstDocumentDock(p);
    }

    private void ResetIfEmpty()
    {
        if (root == null) return;

        var docs = CollectAllDocuments(root);
        if (docs.Any()) return;

        // recreate the minimal start-up layout
        var start = CreateDocument("BusLab");
        var dock = new DocumentDock
        {
            VisibleDockables  = CreateList<IDockable>(start),
            ActiveDockable    = start,
            CanCreateDocument = false,
            IsCollapsable     = true,
            CanFloat          = false,
            MinWidth = 200,
            MinHeight = 200
        };

        var main = new ProportionalDock
        {
            Orientation      = Orientation.Horizontal,
            VisibleDockables = CreateList<IDockable>(dock)
        };

        root.VisibleDockables = CreateList<IDockable>(main);
        root.DefaultDockable   = main;
        root.ActiveDockable    = main;
        InitLayout(root);

    }

    private List<Document> CollectAllDocuments(IDockable root)
    {
        var list = new List<Document>();
        void Walk(IDockable d)
        {
            switch (d)
            {
                case Document doc: list.Add(doc); break;
                case IRootDock r:   foreach (var c in r.VisibleDockables ?? Enumerable.Empty<IDockable>()) Walk(c); break;
                case ProportionalDock p: foreach (var c in p.VisibleDockables ?? Enumerable.Empty<IDockable>()) Walk(c); break;
                case DocumentDock dd: foreach (var c in dd.VisibleDockables ?? Enumerable.Empty<IDockable>()) if (c is Document doc2) list.Add(doc2); break;
            }
        }
        Walk(root);
        return list;
    }

    #endregion

    
}