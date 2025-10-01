using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
namespace BusLab;

public partial class MainWindow : Window
{

    bool isPointerPressed = false;
    Point originalPosition;


    public MainWindow()
    {
        InitializeComponent();

        TabButton.AddHandler(PointerPressedEvent, PointerPressed, RoutingStrategies.Tunnel);
        TabButton.AddHandler(PointerMovedEvent, PointerMoved, RoutingStrategies.Tunnel);
        TabButton.AddHandler(PointerReleasedEvent, PointerReleased, RoutingStrategies.Tunnel);
    }

    private void PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        Console.WriteLine("Pointer pressed");
        isPointerPressed = true;
        originalPosition = e.GetPosition(this);
    }

    private void PointerMoved(object? sender, PointerEventArgs e)
    {
        Console.WriteLine("Pointer moved");
        if (isPointerPressed)
        {
            var currentPosition = e.GetPosition(this);
            var deltaX = currentPosition.X - originalPosition.X;
            var deltaY = currentPosition.Y - originalPosition.Y;

            if (Math.Abs(deltaX) < 50 && Math.Abs(deltaY) < 50)
            {
                // Ignore small movements to prevent jitter
                return;
            }

            TabButton.RenderTransform = new Avalonia.Media.TranslateTransform(deltaX, deltaY);
        }
    }

    private void PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        Console.WriteLine("Pointer released");
        isPointerPressed = false;

        TabButton.RenderTransform = new Avalonia.Media.TranslateTransform(0, 0);
    }
    


    
}