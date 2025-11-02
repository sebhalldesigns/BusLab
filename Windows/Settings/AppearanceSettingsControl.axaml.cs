using Avalonia.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BusLab;

public partial class AppearanceSettingsControl: UserControl
{
    public AppearanceSettingsControl()
    {
        InitializeComponent();

    }

    public void ColorThemeSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ColorThemeComboBox == null)
        {
            return;
        }

        switch (ColorThemeComboBox.SelectedIndex)
        {
            case 1:
                App.Current!.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Light;
                break;
            case 2:
                App.Current!.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;
                break;
            default:
                App.Current!.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Default;
                break;
        }
    }
}