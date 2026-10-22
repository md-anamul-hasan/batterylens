using System.Threading.Tasks;
using System.Windows;

namespace BatteryLens;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        Mdah.Theming.Theme.Initialize("BatteryLens");
        base.OnStartup(e);

        // Catch unhandled exceptions on the UI thread
        DispatcherUnhandledException += (s, args) =>
        {
            LogCrash(args.Exception);
            MessageBox.Show(
                "BatteryLens hit an unexpected problem but is still running.\n\n" +
                "Details were saved to %AppData%\\BatteryLens\\error.log. If it keeps happening, email contact@mdanamulhasan.com and attach that file.",
                "BatteryLens",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            args.Handled = true;
        };

        // Catch unobserved async Task exceptions (fire-and-forget tasks)
        TaskScheduler.UnobservedTaskException += (s, args) =>
        {
            args.SetObserved(); // Prevent process termination
            // Exceptions from background hardware reads are expected on desktops with no WMI battery
        };
    }

    private static void LogCrash(Exception ex)
    {
        try
        {
            string dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BatteryLens");
            System.IO.Directory.CreateDirectory(dir);
            System.IO.File.AppendAllText(System.IO.Path.Combine(dir, "error.log"), $"[{DateTime.Now:u}] {ex}\n\n");
        }
        catch { /* logging must never crash the app */ }
    }
}
