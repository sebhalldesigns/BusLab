using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

using Material.Icons.Avalonia;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avalonia.Styling;
using Avalonia.Controls.Metadata;

using System;
using System.Collections.Generic;
using System.IO;

using BusLab.UI.Docking;

namespace BusLab.UI.Documents;

public partial class DbcDocument : DocumentDockItem
{
    public DbcDocument(string path) : base($"buslab.ui.documents.dbcdocument:{path}", Path.GetFileName(path))
    {
        InitializeComponent();
    }
}
