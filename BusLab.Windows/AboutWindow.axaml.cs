using Avalonia.Controls;
using Avalonia.Interactivity;

namespace BusLab;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();

    }

    public void ClosePressed(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}