using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using Avalonia;

namespace BusLab;

public partial class NewWindow : Window
{
    private Button? selectedButton = null;
    private IBrush unselectedBrush = Brushes.Gray;
    private IBrush selectedBrush = Brushes.LightBlue;

    public NewWindow()
    {
        InitializeComponent();

        unselectedBrush = Brushes.Transparent;

        if (Application.Current!.TryGetResource("ThemeControlMidBrush", App.Current!.ActualThemeVariant, out var brush))
        {
            selectedBrush = brush as IBrush ?? Brushes.Gray;
        }

        selectedButton = NewNetButton;
        UpdateHighlights();
    }

    public void ClosePressed(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    public void NewButtonPressed(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;

        selectedButton = button;
        UpdateHighlights();
    }

    private void UpdateHighlights()
    {
        if (selectedButton == null)
            return;

        NewNetButton.Background = unselectedBrush;
        NewFilterButton.Background = unselectedBrush;
        NewCanDatabaseButton.Background = unselectedBrush;

        selectedButton.Background = selectedBrush;

    }
}