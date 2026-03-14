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
    public static string ModArchivePath = "";
    public static string BepinArchivePath = "";
    public static string ChangeSkinArchivePath = "";
    private string providedPath = "";
    public static bool InDownloadMode = true;
    public static bool DeleteSavefile = true;
    public static bool Ready = false;
    private Log logWindow = null;


    public Installer()
    {
        InitializeComponent();
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

    public void SetStatus(string status)
    {
        this.ButtonInstall.Content=status;
    }
    private async void ButtonInstall_Click(object sender, RoutedEventArgs e)
    {
        if(!Installer.Ready)
        {
            LogHandler.Instance.Write("wtf how did you do that, don't do that, installer is not ready");
            return;
        }
        this.DisableInputs();
        await InstallerTask.Install([this.providedPath], this);
    }

    public void EnableInputs()
    {
        this.CheckBoxDownloadGame.IsEnabled=true;
        this.CheckBoxSavefileDelete.IsEnabled=true;
        this.ButtonInstall.IsEnabled=true;
        this.ButtonBrowsePath.IsEnabled=true;
        this.TextBoxGamePath.IsEnabled=true;
        this.ButtonInstall.Content="Install";
    }

    public void DisableInputs()
    {
        this.CheckBoxDownloadGame.IsEnabled=false;
        this.CheckBoxSavefileDelete.IsEnabled=false;
        this.CheckBoxChangeSkinInstall.IsEnabled=false;
        this.ButtonInstall.IsEnabled=false;
        this.ButtonBrowsePath.IsEnabled=false;
        this.TextBoxGamePath.IsEnabled=false;
    }

    public static void ClearStatics()
    {
        Installer.GameFolderPath="";
        Installer.GamePath="";
        Installer.BepinArchivePath="";
        Installer.ModArchivePath="";
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


    private async void ButtonOpenLog_Click(object sender, RoutedEventArgs e)
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

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await InstallerTask.Fetch(this);
        InstallerTask.PreliminarySetup(this);
    }

    private void CheckBoxDownloadGame_Checked(object sender, RoutedEventArgs e)
    {
        Installer.InDownloadMode=true;
    }
    private void CheckBoxSavefileDelete_Checked(object sender, RoutedEventArgs e)
    {
        Installer.DeleteSavefile=true;
    }
    private void CheckBoxDownloadGame_Unchecked(object sender, RoutedEventArgs e)
    {
        Installer.InDownloadMode=false;
    }

    private void CheckBoxSavefileDelete_Unchecked(object sender, RoutedEventArgs e)
    {
        Installer.DeleteSavefile=false;
    }
}