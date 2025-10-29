using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BusLab;

public partial class TimeRangeControl: UserControl
{
    private bool isDraggingLeft = false;
    private bool isDraggingMiddle = false;
    private bool isDraggingRight = false;

    private Point startPoint;
    private double startLeftWidth;
    private double startMiddleWidth;
    private double startRightWidth;

    Avalonia.Controls.ColumnDefinition LeftColumn;
    Avalonia.Controls.ColumnDefinition MiddleColumn;
    Avalonia.Controls.ColumnDefinition RightColumn;

    public double SizeRatio = 0.5;
    public double OffsetRatio = 0.0;

    private double minWidth = 20.0; /* 5% minimum width for middle */
    
    public TimeRangeControl()
    {
        InitializeComponent();

        LeftColumn = new Avalonia.Controls.ColumnDefinition();
        LeftColumn.Width = new GridLength(1, GridUnitType.Star);
        MiddleColumn = new Avalonia.Controls.ColumnDefinition();
        MiddleColumn.Width = new GridLength(1, GridUnitType.Star);
        RightColumn = new Avalonia.Controls.ColumnDefinition();
        RightColumn.Width = new GridLength(0, GridUnitType.Star);

        ColumnsGrid.ColumnDefinitions.Add(LeftColumn);
        ColumnsGrid.ColumnDefinitions.Add(MiddleColumn);
        ColumnsGrid.ColumnDefinitions.Add(RightColumn);

    }

    private void OnLeftHandlePressed(object? sender, PointerPressedEventArgs e) => StartDrag(e, "Left");
    private void OnMiddlePressed(object? sender, PointerPressedEventArgs e) => StartDrag(e, "Middle");
    private void OnRightHandlePressed(object? sender, PointerPressedEventArgs e) => StartDrag(e, "Right");

    private void StartDrag(PointerPressedEventArgs e, string region)
    {

        startPoint = e.GetPosition(ColumnsGrid);

        startLeftWidth = LeftColumn.ActualWidth;
        startMiddleWidth = MiddleColumn.ActualWidth;
        startRightWidth = RightColumn.ActualWidth;

        isDraggingLeft = region == "Left";
        isDraggingMiddle = region == "Middle";
        isDraggingRight = region == "Right";

        e.Pointer.Capture(ColumnsGrid);
        ColumnsGrid.PointerMoved += OnPointerMoved;
        ColumnsGrid.PointerReleased += OnPointerReleased;
    }
    
    private void OnPointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        Point pos = e.GetPosition(ColumnsGrid);

        double dx = pos.X - startPoint.X;

        double newLeft = startLeftWidth;
        double newMiddle = startMiddleWidth;
        double newRight = startRightWidth;

        if (isDraggingLeft)
        {
            newLeft = Math.Max(0, startLeftWidth + dx);
            newMiddle = Math.Max(minWidth, startMiddleWidth - dx);
        }
        else if (isDraggingRight)
        {
            newMiddle = Math.Max(minWidth, startMiddleWidth + dx);
            newRight = Math.Max(0, startRightWidth - dx);
        }
        else if (isDraggingMiddle)
        {
            /* don't allow dragging beyond limits */
            if (newLeft + dx < 0)
            {
                newLeft = 0;
                newRight = startLeftWidth + startMiddleWidth + startRightWidth - newLeft - newMiddle;
            }
            else if (newRight - dx < 0)
            {
                newRight = 0;
                newLeft = startLeftWidth + startMiddleWidth + startRightWidth - newMiddle - newRight;
            }
            else
            {
                newLeft = Math.Max(0, startLeftWidth + dx);
                newRight = Math.Max(0, startRightWidth - dx);
            }
        }

        double total = newLeft + newMiddle + newRight;
        LeftColumn.Width = new GridLength(newLeft / total, GridUnitType.Star);
        MiddleColumn.Width = new GridLength(newMiddle / total, GridUnitType.Star);
        RightColumn.Width = new GridLength(newRight / total, GridUnitType.Star);

        SizeRatio = newMiddle / total;
        OffsetRatio = newRight / total;
    }

    private void OnPointerReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
    {
        isDraggingLeft = false;
        isDraggingMiddle = false;
        isDraggingRight = false;

        ColumnsGrid.PointerMoved -= OnPointerMoved;
        ColumnsGrid.PointerReleased -= OnPointerReleased;
        e.Pointer.Capture(null);
    }
}