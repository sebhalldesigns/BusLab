using Avalonia;
using System;

namespace BusLab;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        Console.WriteLine("STARTING");

        CanDatabase? dbc = DatabaseReader.Read("sample4.dbc", out string error, out string detailedError);

        if (dbc == null)
        {
            Console.WriteLine(error);
            Console.WriteLine(detailedError);
        }
        else
        {
            Console.WriteLine("Opened DBC successfully");

            string? outPath = DatabaseWriter.Write("out.dbc", dbc, out string error2, out string detailedError2);

            if (outPath == null)
            {
                Console.WriteLine(error2);
                Console.WriteLine(detailedError2);
            }
        }



        //BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }
    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
