using System.IO;
using System.Windows;

namespace ScavKRInstaller;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        int exitCode = 1;
        try
        {
            exitCode = await AutoInstaller.RunAsync();
        }
        catch(Exception ex)
        {
            LogHandler.Instance.Write($"#!STARTUP FAILURE!# {ex}");
            WriteCrashLog(ex);
        }

        Shutdown(exitCode);
    }

    private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        LogHandler.Instance.Write("#!UNHANDLED EXCEPTION CRASH!#");
        WriteCrashLog(e.Exception);
        e.Handled = true;
        Shutdown(1);
    }

    private static void WriteCrashLog(Exception ex)
    {
        try
        {
            string filename = $"INSTALLER_LOG_{DateTime.Now.Ticks}.txt";
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), filename);
            File.WriteAllText(path, ex + $"\n\nLOG START\n\n{LogHandler.Instance.GetWholeLog()}");
        }
        catch
        {
            // Last-resort path: no UI is shown in automatic/headless mode.
        }
    }
}
