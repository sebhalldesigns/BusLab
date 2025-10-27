using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Interactivity;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ScottPlot;


namespace BusLab;

public partial class SignalPlotPanel: UserControl
{
    public SignalPlotPanel()
    {
        InitializeComponent();

        double[] dataX = { 1, 2, 3, 4, 5 };
        double[] dataY = { 1, 4, 9, 16, 25 };

        ScottPlot.Plottables.Scatter scatter = SignalPlot.Plot.Add.Scatter(dataX, dataY);

        ScottPlot.Plottables.Crosshair crosshair = SignalPlot.Plot.Add.Crosshair(0, 0);

        SignalPlot.PointerMoved  += (s, e) =>
        {   
            Avalonia.Input.PointerPoint point = e.GetCurrentPoint(s as Control);

            Pixel mousePixel = new Pixel((float)point.Position.X, (float)point.Position.Y);
            Coordinates mouseCoordinates = SignalPlot.Plot.GetCoordinates(mousePixel);

            DataPoint nearest = scatter.Data.GetNearestX(mouseCoordinates, SignalPlot.Plot.LastRender);

            crosshair.Position = nearest.Coordinates;
            crosshair.VerticalLine.Text = $"{mouseCoordinates.X:N3}";
            crosshair.HorizontalLine.Text = $"{mouseCoordinates.Y:N3}";

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

}