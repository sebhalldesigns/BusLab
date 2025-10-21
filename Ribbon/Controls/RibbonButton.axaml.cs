using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace BusLab;

public partial class RibbonButton : UserControl
{
    public RibbonButton()
    {
        InitializeComponent();

        DataContext = this;
    }

    public static readonly StyledProperty<string> CaptionProperty =
        AvaloniaProperty.Register<RibbonButton, string>(nameof(Caption), "Button");
    
    public static readonly StyledProperty<string> ToolTipProperty =
        AvaloniaProperty.Register<RibbonButton, string>(nameof(ToolTip), "ToolTip");
    
    public static readonly StyledProperty<IImage?> IconProperty =
        AvaloniaProperty.Register<RibbonButton, IImage?>(nameof(Icon), null);

    public string Caption
    {
        get => GetValue(CaptionProperty);
        set => SetValue(CaptionProperty, value);
    }

    public string ToolTip
    {
        get => GetValue(ToolTipProperty);
        set => SetValue(ToolTipProperty, value);
    }

    public IImage? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }



   

}