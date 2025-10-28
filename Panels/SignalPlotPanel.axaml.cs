using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.Interactivity;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ScottPlot;


namespace BusLab;

public partial class SignalPlotPanel: UserControl
{
    private ScottPlot.Plottables.AxisLine? PlottableBeingDragged = null;
    private ScottPlot.Plottables.AxisLine vline;
    private ScottPlot.Plottables.Scatter scatter;
    
    CircularDataBuffer buffer = new CircularDataBuffer(1000);

    ulong counter = 0;

    private ScottPlot.DataGenerators.RandomWalker Walker1 = new(0);

    private readonly DispatcherTimer timer = new DispatcherTimer
    {
        Interval = TimeSpan.FromMilliseconds(10)
    };

    public SignalPlotPanel()
    {
        InitializeComponent();

        scatter = SignalPlot.Plot.Add.Scatter(buffer.XOrdered, buffer.YOrdered);
        scatter.MarkerSize = 0;


        timer.Tick += OnTimerTick;
        timer.Start();

        vline = SignalPlot.Plot.Add.VerticalLine(23);
        vline.IsDraggable = true;


        SignalPlot.PointerMoved  += (s, e) =>
        {   
            Avalonia.Input.PointerPoint point = e.GetCurrentPoint(s as Control);

            Pixel mousePixel = new Pixel((float)point.Position.X, (float)point.Position.Y);
            Coordinates mouseCoordinates = SignalPlot.Plot.GetCoordinates(mousePixel);

            if (PlottableBeingDragged == null)
            {
                ScottPlot.Plottables.AxisLine? line = GetLineUnderMouse((float)e.GetPosition(SignalPlot).X, (float)e.GetPosition(SignalPlot).Y);
                if (line != null)
                {
                    SignalPlot.Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.SizeWestEast);
                }
                else
                {
                    SignalPlot.Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Arrow);
                }
            }
            else
            {
                PlottableBeingDragged.Position = mouseCoordinates.X;
                //DataPoint nearest = scatter.Data.GetNearestX(mouseCoordinates, SignalPlot.Plot.LastRender);
            }


            SignalPlot.Refresh();
        };

        SignalPlot.PointerPressed += (s, e) =>
        {
            ScottPlot.Plottables.AxisLine? line = GetLineUnderMouse((float)e.GetPosition(SignalPlot).X, (float)e.GetPosition(SignalPlot).Y);
            if (line != null)
            {
                PlottableBeingDragged = line;
                SignalPlot.UserInputProcessor.Disable();
            }
        };

        SignalPlot.PointerReleased += (s, e) =>
        {
            PlottableBeingDragged = null;
            SignalPlot.UserInputProcessor.Enable();
            SignalPlot.Refresh();
        };

        UpdateColors(Application.Current.RequestedThemeVariant ?? ThemeVariant.Light);

        SignalPlot.Refresh();

        
        ActualThemeVariantChanged += (sender, args) =>
        {
            UpdateColors(ActualThemeVariant);
        };
    }

    private void UpdateColors(ThemeVariant variant)
    {
        if (variant == ThemeVariant.Dark)
        {
            SignalPlot.Plot.Add.Palette = new ScottPlot.Palettes.Penumbra();

            // change figure colors
            SignalPlot.Plot.FigureBackground.Color = Color.FromHex("#181818");
            SignalPlot.Plot.DataBackground.Color = Color.FromHex("#1f1f1f");

            // change axis and grid colors
            SignalPlot.Plot.Axes.Color(Color.FromHex("#d7d7d7"));
            SignalPlot.Plot.Grid.MajorLineColor = Color.FromHex("#404040");

            // change legend colors
            SignalPlot.Plot.Legend.BackgroundColor = Color.FromHex("#404040");
            SignalPlot.Plot.Legend.FontColor = Color.FromHex("#d7d7d7");
            SignalPlot.Plot.Legend.OutlineColor = Color.FromHex("#d7d7d7");

            SignalPlot.Refresh();
        }
        else
        {
            // change figure colors
            SignalPlot.Plot.FigureBackground.Color = Color.FromHex("#ffffff");
            SignalPlot.Plot.DataBackground.Color = Color.FromHex("#ffffff");

            // change axis and grid colors
            SignalPlot.Plot.Axes.Color(Color.FromHex("#404040"));
            SignalPlot.Plot.Grid.MajorLineColor = Color.FromHex("#b0b0b0");

            // change legend colors
            SignalPlot.Plot.Legend.BackgroundColor = Color.FromHex("#ffffff");
            SignalPlot.Plot.Legend.FontColor = Color.FromHex("#ffffff");
            SignalPlot.Plot.Legend.OutlineColor = Color.FromHex("#d7d7d7");

            SignalPlot.Refresh();
        }
    }

    private ScottPlot.Plottables.AxisLine? GetLineUnderMouse(float x, float y)
    {
        CoordinateRect rect = SignalPlot.Plot.GetCoordinateRect(x, y, radius: 10);

        if (vline.IsUnderMouse(rect))
            return vline;

        return null;
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        double next = Walker1.Next();
        buffer.Add(counter, next);

        buffer.OrderData();
        
        SignalPlot.Refresh();

        counter++;
    }

}

public class CircularDataBuffer
{
    public double[] X;
    public double[] Y;
    private int NextIndex = 0;
    private bool Filled = false;

    public int Capacity => X.Length;
    public int Count => Filled ? Capacity : NextIndex;

    public double[] XOrdered;
    public double[] YOrdered;

    public CircularDataBuffer(int capacity)
    {
        X = new double[capacity];
        Y = new double[capacity];

        XOrdered = new double[capacity];
        YOrdered = new double[capacity];

        for (int i = 0; i < capacity; i++)
        {
            X[i] = double.NaN;
            Y[i] = double.NaN;

            XOrdered[i] = double.NaN;
            YOrdered[i] = double.NaN;
        }
    }

    public void Add(double x, double y)
    {
        X[NextIndex] = x;
        Y[NextIndex] = y;

        NextIndex++;
        if (NextIndex >= Capacity)
        {
            NextIndex = 0;
            Filled = true;
        }
    }

    public void OrderData()
    {
        if (Filled)
        {
            Array.Copy(X, NextIndex, XOrdered, 0, Capacity - NextIndex);
            Array.Copy(X, 0, XOrdered, Capacity - NextIndex, NextIndex);

            Array.Copy(Y, NextIndex, YOrdered, 0, Capacity - NextIndex);
            Array.Copy(Y, 0, YOrdered, Capacity - NextIndex, NextIndex);
        }
        else
        {
            Array.Copy(X, 0, XOrdered, 0, NextIndex);
            Array.Copy(Y, 0, YOrdered, 0, NextIndex);
        }

    }
}