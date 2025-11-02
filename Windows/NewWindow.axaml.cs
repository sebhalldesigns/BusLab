using Avalonia.Controls;
using Avalonia.Interactivity;
using System;


namespace BusLab;

public partial class NewWindow : Window
{
    public NewWindow()
    {
        InitializeComponent();
        TabContent.Content = new NewNetControl();
    }

    private void TabSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (TabListBox == null || TabContent == null)
        {
            return;
        }

        switch (TabListBox.SelectedIndex)
        {
            case 0:
                TabContent.Content = new NewNetControl();
                break;
            case 1:
                TabContent.Content = new NewCanDatabaseControl();
                break;
            default:
                break;
        }
    }

    public void ClosePressed(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}