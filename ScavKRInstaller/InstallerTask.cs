using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ScavKRInstaller
{
    public class InstallerTask
    {
        public static async Task Fetch(Installer installerWindow = null)
        {
            installerWindow?.DisableInputs();
            installerWindow?.SetStatus("Fetching sources...");
            try
            {
                await VersionManager.Init(Constants.SourcelistURL);
                Installer.Ready=true;
            }
            catch
            {
                LogHandler.Instance.Write("Fetch failed; no local source.json; no github access");
                if(installerWindow != null)
                {
                    MessageBox.Show("Unable to fetch sources, and no local source file was found!\n\nThe installer cannot proceed further, and this is very, very bad, meaning that github is not accessible and you will not be able to download the mods from there.\nIf you believe that this is a bug or if you want to use a separate filehost list, consider:\n\n1. Disabling your VPN/proxy/geoblock bypass software since it sometimes interferes with http requests which are used to fetch the list.\n\n2. Acquiring sources.json from the github page manually, there is a link to the secret gist in the readme. Then place the file into the same directory as the installer and relaunch.\n\nTHIS IS NOT GUARANTEED TO WORK!\n\nIF YOU BELIEVE THAT THIS IS A BUG, REPORT IT!", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                    installerWindow?.SetStatus("SOURCE ERROR!");
                }
                else
                {
                    throw new NullReferenceException("Fetch failed");
                }
                return;
            }
            installerWindow?.SetStatus("Initial setup...");
            FileOperations.DiscoverFilenames();
            string[] saveFilePaths;
            if(FileOperations.CheckIfSaveFilesPresent(out saveFilePaths))
            {
                Installer.SaveFilePaths=saveFilePaths;
            }
            FileOperations.DeleteTempFiles();
            installerWindow?.EnableInputs();
            return;
        }
        public static async Task Install(string[] path, Installer installerWindow = null)
        {
            LogHandler.Instance.Write($"BEGIN: Initiating installation");
            List<string> finalUnzipPaths = new();
            try
            {
                FileOperations.HandleProvidedGamePath(ref path[0]);
            }
            catch(Exception ex)
            {
                if(installerWindow != null)
                {
                    if(ex.Message=="Non-latin characters in the gamepath!") MessageBox.Show("Provided path contains non-latin characters! For the mod to function properly, path should be english-only.", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                    else MessageBox.Show("Provided path is invalid!", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                LogHandler.Instance.Write($"CANCEL: invalid path");
                goto CancelInstallation;
            }
            if(Installer.InDownloadMode)
            {
                try
                {
                    installerWindow?.SetStatus("Downloading the game, please wait!");
                    finalUnzipPaths.Add(await FileOperations.TryGameDownload(VersionManager.Instance.Versions["Latest"].Game.ToArray()));
                }
                catch(TimeoutException ex)
                {
                    if(installerWindow != null)
                    {
                        MessageBox.Show("Failed to download the game from multiple mirrors!\n\nTry again and consider acquiring the game manually if this fails multiple times.", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    LogHandler.Instance.Write($"CANCEL: all mirrors are bust!");
                    goto CancelInstallation;
                }
            }
            if(FileOperations.CheckForMod(Installer.GameFolderPath))
            {
                if(installerWindow != null)
                {
                    MessageBoxResult msgBoxModAlreadyInstalled = MessageBox.Show("Looks like the mod is already installed!\n\nInstaller is going to download and install the latest version of the mod from github.\n\nContinue?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if(msgBoxModAlreadyInstalled == MessageBoxResult.No)
                    {
                        LogHandler.Instance.Write($"DONE: Did not want to update");
                        goto CancelInstallation;
                    }
                    LogHandler.Instance.Write($"Agreed to update");
                }
                LogHandler.Instance.Write("Mod already detected, updating");
            }
            //changeskin out until we get a consistent mod file structure
            //if(this.CheckBoxChangeSkinInstall.IsChecked.Value)
            //{
            //    try
            //    {
            //        this.SetStatus("Downloading ChangeSkin...");
            //        Installer.ChangeSkinArchivePath=await FileOperations.DownloadArchive(VersionManager.Instance.Versions["Latest"].Ch.ToArray()));
            //    }
            //    catch(Exception ex)
            //    {
            //        MessageBox.Show($"Error while downloading ChangeSkin mod!\nContact the developer if the issue persists!", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
            //        LogHandler.Instance.Write($"SKIP: Bepin fail: {ex.ToString()}");
            //    }
            //}
            if(Installer.DeleteSavefile)
            {
                FileOperations.DeleteSavefiles(Installer.SaveFilePaths);
            }
            if(!FileOperations.CheckForBepin(Installer.GameFolderPath))
            {
                installerWindow?.SetStatus("Downloading BepinEX...");
                try
                {
                    Installer.BepinArchivePath=await FileOperations.DownloadArchive(VersionManager.Instance.Versions["Latest"].Bepin.ToArray()[0]);
                }
                catch(Exception ex)
                {
                    if(installerWindow != null)
                    {
                        MessageBox.Show($"Error while downloading BepInEx!\nContact the developer if the issue persists!", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    LogHandler.Instance.Write($"CANCEL: Bepin fail: {ex.ToString()}");
                    goto CancelInstallation;
                }
            }
            installerWindow?.SetStatus("Downloading multiplayer mod...");
            try
            {
                Installer.ModArchivePath=await FileOperations.DownloadArchive(VersionManager.Instance.Versions["Latest"].MultiplayerMod.ToArray()[0]);
            }
            catch(Exception ex)
            {
                if(installerWindow != null)
                {
                    MessageBox.Show($"Error while downloading the multiplayer mod! Caught exception:\n{ex.Message}\n{ex.StackTrace}\n\nContact the developer if issue persists!", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                LogHandler.Instance.Write($"CANCEL: Mod fail: {ex.ToString()}");
                goto CancelInstallation;
            }
            if(!String.IsNullOrEmpty(Installer.BepinArchivePath))
            {
                finalUnzipPaths.Add(Installer.BepinArchivePath);
            }
            if(!String.IsNullOrEmpty(Installer.ChangeSkinArchivePath))
            {
                finalUnzipPaths.Add(Installer.ChangeSkinArchivePath);
            }
            finalUnzipPaths.Add(Installer.ModArchivePath);
            installerWindow?.SetStatus("Extracting archives...");
            string[] unpackedDirs = [];
            try
            {
                FileOperations.UnzipFiles(finalUnzipPaths.ToArray(), out unpackedDirs);
            }
            catch(Exception ex)
            {
                if(installerWindow != null)
                {
                    MessageBox.Show($"Error while unzipping mods! Ensure that your %TEMP% folder has write permissions!\n\nCaught exception:\n{ex.Message}\n{ex.StackTrace}\n\nContact the developer if issue persists!", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                LogHandler.Instance.Write($"CANCEL: Zip fail: {ex.ToString()}");
                goto CancelInstallation;
            }
            installerWindow?.SetStatus("Moving files...");
            if(!FileOperations.HandleCopyingFiles(unpackedDirs))
            {
                if(installerWindow != null)
                {
                    MessageBox.Show("Error while copying files to the game folder! Ensure that your game folder has write permissions!", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                LogHandler.Instance.Write($"CANCEL: Copy fail.");
                goto CancelInstallation;
            }
            installerWindow?.SetStatus("Done!");
            LogHandler.Instance.Write($"DONE: Success!");
            if(installerWindow != null)
            {
                MessageBoxResult msgBoxFinished = MessageBox.Show($"{(Installer.InDownloadMode ? "Modded game" : "Mod")} has been succesfully installed! Don't forget to delete this installer.\n\nLaunch the game?", "Message", MessageBoxButton.YesNo, MessageBoxImage.Information);
                if(msgBoxFinished == MessageBoxResult.Yes)
                {
                    Process.Start(new ProcessStartInfo(Installer.GamePath));
                    Environment.Exit(0);
                }
            }
        CancelInstallation:
            {
                installerWindow?.EnableInputs();
                Installer.ClearStatics();
                FileOperations.DeleteTempFiles();
                return;
            }
        }
    }
}
