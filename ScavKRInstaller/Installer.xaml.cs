using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace ScavKRInstaller;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class Installer : Window
{
    public static string GamePath = "";
    public static string GameFolderPath = "";
    public static string[] SaveFilePaths = [];
    public static string ModZipArchivePath = "";
    public static string BepinZipArchivePath = "";
    private string providedPath = "";
    public static bool InDownloadMode = true;
    private Log logWindow = null;

    public Installer()
    {
        InitializeComponent();
        FileOperations.DiscoverFilenames();
        string[] saveFilePaths;
        if(FileOperations.CheckIfSaveFilesPresent(out saveFilePaths))
        {
            Installer.SaveFilePaths=saveFilePaths;
        }
        FileOperations.DeleteTempFiles();
        Window.GetWindow(this).Title = $"Scav Krokosha Multiplayer Installer rev. {Constants.Version}: {Constants.GetSplash()[Random.Shared.Next(0, Constants.GetSplash().Length)]}";
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute=true });
    }

    private void ButtonBrowsePath_Click(object sender, RoutedEventArgs e)
    {
        if((bool)CheckBoxDownloadGame.IsChecked)
        {
            OpenFolderDialog dialog = new OpenFolderDialog();
            dialog.Multiselect=false;
            bool? result = dialog.ShowDialog();
            if(result==null) return;
            if((bool)result)
            {
                Installer.GameFolderPath=dialog.FolderName;
                this.TextBoxGamePath.Text=dialog.FolderName;
            }
        }
        else
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter="Game executable (.exe)|CasualtiesUnknown.exe";
            dialog.Multiselect=false;
            bool? result = dialog.ShowDialog();
            if(result==null) return;
            if((bool)result)
            {
                string gameFile = dialog.FileName;
                if(FileOperations.HandleProvidedGamePath(ref gameFile))
                {
                    this.TextBoxGamePath.Text=gameFile;
                }
            }
        }
    }

    private void TextBoxGamePath_TextChanged(object sender, TextChangedEventArgs e)
    {
        this.providedPath = this.TextBoxGamePath.Text;
    }

    private void SetStatus(string status)
    {
        this.ButtonInstall.Content=status;
    }
    private async void ButtonInstall_Click(object sender, RoutedEventArgs e)
    {
        LogHandler.Instance.Write($"BEGIN: Initiating installation");
        this.CheckBoxDownloadGame.IsEnabled=false;
        this.CheckBoxSavefileDelete.IsEnabled=false;
        this.ButtonInstall.IsEnabled=false;
        this.ButtonBrowsePath.IsEnabled=false;
        this.TextBoxGamePath.IsEnabled=false;
        List<string> finalUnzipPaths=new();
        try
        {
            FileOperations.HandleProvidedGamePath(ref this.providedPath);
        }
        catch(Exception ex)
        {
            if (ex.Message=="Non-latin characters in the gamepath!") MessageBox.Show("Provided path contains non-latin characters! For the mod to function properly, path should be english-only.", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
            else MessageBox.Show("Provided path is invalid!", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
            LogHandler.Instance.Write($"CANCEL: invalid path");
            goto CancelInstallation;
        }
        if((bool)this.CheckBoxDownloadGame.IsChecked)
        {
            try
            {
                this.SetStatus("Downloading the game, please wait!");
                finalUnzipPaths.Add(await FileOperations.TryGameDownload(Constants.GameDownloadURLs));
            }
            catch(TimeoutException ex)
            {
                MessageBox.Show("Failed to download the game from multiple mirrors!\n\nTry again and consider acquiring the game manually if this fails multiple times.", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                LogHandler.Instance.Write($"CANCEL: all mirrors are bust!");
                goto CancelInstallation;
            }
        }
        if(FileOperations.CheckForMod(Installer.GameFolderPath))
        {
            MessageBoxResult msgBoxModAlreadyInstalled = MessageBox.Show("Looks like the mod is already installed!\n\nInstaller is going to download and install the latest version of the mod from github.\n\nContinue?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if(msgBoxModAlreadyInstalled == MessageBoxResult.No)
            {
                LogHandler.Instance.Write($"DONE: Did not want to update");
                goto CancelInstallation;
            }
            LogHandler.Instance.Write($"Agreed to update");
        }
        if((bool)this.CheckBoxSavefileDelete.IsChecked)
        {
            FileOperations.DeleteSavefiles(Installer.SaveFilePaths);
        }
        if(!FileOperations.CheckForBepin(Installer.GameFolderPath))
        {
            this.SetStatus("Downloading BepinEX...");
            try
            {
                Installer.BepinZipArchivePath=await FileOperations.DownloadArchive(Constants.BepinZipURL);
            }
            catch(Exception ex)
            {
                MessageBox.Show($"Error while downloading BepInEx! Caught exception:\n{ex.Message}\n{ex.StackTrace}\n\nContact the developer if issue persists!", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                LogHandler.Instance.Write($"CANCEL: Bepin fail: {ex.ToString()}");
                goto CancelInstallation;
            }
        }
        this.SetStatus("Downloading multiplayer mod...");
        try
        {
            Installer.ModZipArchivePath=await FileOperations.DownloadArchive(Constants.ModZipURL);
        }
        catch(Exception ex)
        {
            MessageBox.Show($"Error while downloading the multiplayer mod! Caught exception:\n{ex.Message}\n{ex.StackTrace}\n\nContact the developer if issue persists!", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
            LogHandler.Instance.Write($"CANCEL: Mod fail: {ex.ToString()}");
            goto CancelInstallation;
        }
        if(!String.IsNullOrEmpty(Installer.BepinZipArchivePath))
        {
            finalUnzipPaths.Add(Installer.BepinZipArchivePath);
        }
        finalUnzipPaths.Add(Installer.ModZipArchivePath);
        this.SetStatus("Extracting archives...");
        string[] unpackedDirs = [];
        try
        {
            FileOperations.UnzipFiles(finalUnzipPaths.ToArray(), out unpackedDirs);
        }
        catch(Exception ex)
        {
            MessageBox.Show($"Error while unzipping mods! Ensure that your %TEMP% folder has write permissions!\n\nCaught exception:\n{ex.Message}\n{ex.StackTrace}\n\nContact the developer if issue persists!", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
            LogHandler.Instance.Write($"CANCEL: Zip fail: {ex.ToString()}");
            goto CancelInstallation;
        }
        this.SetStatus("Moving files...");
        if(!FileOperations.HandleCopyingFiles(unpackedDirs))
        {
            MessageBox.Show("Error while copying files to the game folder! Ensure that your game folder has write permissions!", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
            LogHandler.Instance.Write($"CANCEL: Copy fail.");
            goto CancelInstallation;
        }
        this.SetStatus("Done!");
        LogHandler.Instance.Write($"DONE: Success!");
        MessageBoxResult msgBoxFinished = MessageBox.Show($"{((bool)this.CheckBoxDownloadGame.IsChecked ? "Modded game" : "Mod")} has been succesfully installed! Don't forget to delete this installer.\n\nLaunch the game?", "Message", MessageBoxButton.YesNo, MessageBoxImage.Information);
        if(msgBoxFinished == MessageBoxResult.Yes)
        {
            Process.Start(new ProcessStartInfo(Installer.GamePath));
            Environment.Exit(0);
        }
    CancelInstallation:
        {
            this.CheckBoxDownloadGame.IsEnabled=true;
            this.CheckBoxSavefileDelete.IsEnabled=true;
            this.ButtonInstall.IsEnabled=true;
            this.ButtonBrowsePath.IsEnabled=true;
            this.TextBoxGamePath.IsEnabled=true;
            this.ButtonInstall.Content="Install";
            ClearStatics();
            FileOperations.DeleteTempFiles();
            return;
        }
    }
    public static void ClearStatics()
    {
        Installer.GameFolderPath="";
        Installer.GamePath="";
        Installer.BepinZipArchivePath="";
        Installer.ModZipArchivePath="";
    }
    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if(Application.Current.Windows.OfType<Log>().Count() > 0)
        {
            Application.Current.Windows.OfType<Log>().FirstOrDefault().Close();
        }
        FileOperations.DeleteTempFiles();
    }

    private void CheckBoxDownloadGame_Click(object sender, RoutedEventArgs e)
    {
        Installer.GameFolderPath="";
        Installer.GamePath="";
        this.TextBoxGamePath.Text="";
        if((bool)this.CheckBoxDownloadGame.IsChecked)
        {
            this.TextBlockInstallPath.Text="Install path:";
            return;
        }
        this.TextBlockInstallPath.Text="Game path:";
    }

    private void CheckBoxDownloadGame_Checked(object sender, RoutedEventArgs e)
    {
        Installer.InDownloadMode = CheckBoxDownloadGame.IsChecked.Value;
    }

    private void ButtonOpenLog_Click(object sender, RoutedEventArgs e)
    {
        if(this.logWindow==null)
        {
            this.logWindow=new Log(LogHandler.Instance);
            logWindow.Show();
        }
        else
        {
            if(Application.Current.Windows.OfType<Log>().Count() > 0)
            {
                this.logWindow.Activate();
            }
            else
            {
                this.logWindow=new Log(LogHandler.Instance);
                logWindow.Show();
            }
        }
    }
}