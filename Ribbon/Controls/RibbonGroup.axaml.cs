using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;

namespace BusLab;

public partial class RibbonGroup : UserControl
{
    public RibbonGroup()
    {
        InitializeComponent();

        DataContext = this;
    }

    public static readonly StyledProperty<Control?> GroupContentProperty =
        AvaloniaProperty.Register<RibbonGroup, Control?>(nameof(GroupContent), null);
    
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<RibbonGroup, string>(nameof(Title), "Group");

    public Control? GroupContent
    {
        get => GetValue(GroupContentProperty);
        set => SetValue(GroupContentProperty, value);
    }

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

}