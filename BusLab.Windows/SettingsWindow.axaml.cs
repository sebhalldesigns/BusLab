using Avalonia.Controls;
using Avalonia.Interactivity;
using System;


namespace BusLab;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        TabContent.Content = new GeneralSettingsControl();
    }

    private void TabSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (TabListBox == null || TabContent == null)
        {
            return;
        }

        switch (TabListBox.SelectedIndex)
        {
            case 1:
                TabContent.Content = new AppearanceSettingsControl();
                break;
            case 2:
                TabContent.Content = new LanguageSettingsControl();
                break;
            default:
                TabContent.Content = new GeneralSettingsControl();
                break;
        }
    }

    public void ClosePressed(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}