using Avalonia.Controls;
using Avalonia.Interactivity;

namespace BusLab;

public partial class ErrorWindow : Window
{
    public ErrorWindow(string message, string details)
    {
        InitializeComponent();

        ErrorMessageTextBlock.Text = message;
        ErrorDescriptionTextBlock.Text = details;

    }

    public void ClosePressed(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}