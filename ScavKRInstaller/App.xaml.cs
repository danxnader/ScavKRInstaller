using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace ScavKRInstaller;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        try
        {
            Exception ex = e.Exception;
            string filename = $"INSTALLER_LOG_{DateTime.Now.Ticks.ToString()}.txt";
            string path = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)+Path.DirectorySeparatorChar+filename;
            File.WriteAllText(path, ex.ToString());
            MessageBoxResult result = MessageBox.Show($"A very bad thing has occurred! Read through this carefully:\nThe installer has run into a critical, unexpected problem.\nIf this message shows up when running the executable for the first time, ensure that you have .NET 8.0 desktop runtime installed. Press \"OK\" (or the translated equivalent) button below to open the .NET desktop runtime installer download page.\n\nIf this has happened DURING execution and if you have .NET desktop runtime installed, a log file has been created on your desktop containing the information about the error ({filename}). If you actually want your issue to be addressed, contact the developer via github or other means and forward them the log.", "Critical error!", MessageBoxButton.OKCancel, MessageBoxImage.Error, MessageBoxResult.Cancel);
            if(result == MessageBoxResult.OK) Process.Start(new ProcessStartInfo(@"https://dotnet.microsoft.com/en-us/download/dotnet/8.0") { UseShellExecute = true});
        }
        catch(Exception ex)
        {
            MessageBox.Show($"You are a very special person. This has never happened and should never happen. The installer has somehow failed to write the log data to your desktop. You should take a screenshot of this and contact the developer because i am genuinely curious. Or for some reason you're running this executable in a headless environment, but then i have no idea how you're even reading this.\n\n{ex.ToString()}", "Ultimate critical error!", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

