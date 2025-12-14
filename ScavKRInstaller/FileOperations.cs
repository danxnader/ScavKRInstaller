using System.IO;
using System.Net.Http;
using File = System.IO.File;
using System.Windows;
using System.IO.Compression;
using System.Runtime.CompilerServices;

namespace ScavKRInstaller
{
    public static class FileOperations
    {
        private const string GameName = "CasualtiesUnknown.exe";

        public static bool HandleProvidedGamePath(ref string path)
        {
            string gameName = GameName;
            FileAttributes attributes = File.GetAttributes(path);
            if((attributes & FileAttributes.Directory) == FileAttributes.Directory)
            {
                string possibleGamePath = path+"\\"+gameName;
                if(File.Exists(possibleGamePath))
                {
                    path = possibleGamePath;
                    return true;
                }
                throw new ArgumentException("Provided directory does not contain a game executable!");
            }
            if((attributes & FileAttributes.Archive) == FileAttributes.Archive)
            {
                if(Path.GetFileName(path) == gameName) return true;
                throw new ArgumentException("Provided file is not a game executable!");
            }
            return false;
        }
        public static bool CheckIfSaveFilesPresent(out string[] saveFilePaths)
        {
            List<string> resultPaths = new();
            string[] appdataPaths =
            {
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)+"Low", //Currently, the savefile is stored in LocalLow
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), //Although, it wouldn't hurt to check other folders too
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) //Just in case
            };
            string devName = "Orsoniks";
            string[] gameNames = { "CasualtiesUnknown", "CasualtiesUnknownDemo" }; //Before beta 4, game saved to CasualtiesUnknownDemo. We'll clean both in case it changes again.
            string savefileName = "save.sv";
            bool result = false;
            foreach(string appdataPath in appdataPaths)
            {
                foreach(string name in gameNames)
                {
                    string path = appdataPath+"\\"+devName+"\\"+name+"\\"+savefileName;
                    if(File.Exists(path)) resultPaths.Add(path);
                    result=true;
                }
            }
            if(result)
            {
                saveFilePaths=resultPaths.ToArray();
                return true;
            }
            saveFilePaths= [];
            return false;
        }
        public static bool DeleteSavefiles(string[] paths)
        {
            bool hasDeleted = false;
            foreach(string path in paths)
            {
                File.Delete(path);
                hasDeleted=true;
            }
            return hasDeleted;
        }
        private static string GetTempFolderPath()
        {
            string tempFolder = Path.GetTempPath()+"ScavKRInstaller";
            Directory.CreateDirectory(tempFolder);
            return tempFolder;
        }
        private static string GetZipFilename(string url)
        {
            return url.Substring(url.LastIndexOf('/') + 1, url.LastIndexOf(".zip") + 3 - url.LastIndexOf('/'));
        }
        public static bool CheckForBepin(string gameFolder)
        {
            return Directory.Exists(gameFolder+"\\BepInEx");
        }
        public static bool CheckForMod(string gameFolder)
        {
            return File.Exists(gameFolder+"\\BepInEx\\plugins\\KrokoshaCasualtiesMP.dll");
        }
        public static async Task<string> TryGameDownload(string[] urls)
        {
            foreach(string url in urls)
            {
                try
                {
                    string result = await FileOperations.DownloadArchive(url, true);
                    return result;
                }
                catch
                {
                    continue;
                }
            }
            throw new TimeoutException("All game download mirrors failed!");
        }
        public async static Task<string> DownloadArchive(string url, bool silentExceptions = false)
        {
            HttpClient client = new();
            Uri uri = new(url);
            string filename = FileOperations.GetZipFilename(url);
            string tempFolderFilePath = GetTempFolderPath()+$"\\{filename}";
            try
            {
                HttpResponseMessage response = await client.GetAsync(uri);
                using(FileStream fs = new FileStream(tempFolderFilePath, FileMode.Create))
                {
                    await response.Content.CopyToAsync(fs);
                    return tempFolderFilePath;
                }
            }
            catch(TaskCanceledException ex) when(ex.InnerException is TimeoutException)
            {
                if(!silentExceptions)MessageBox.Show($"Connection has timed while downloading {filename}! Ensure that github.com is reachable and try again.", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                throw new TimeoutException();
            }
            catch(HttpRequestException ex)
            {
                if(!silentExceptions)MessageBox.Show($"Could not connect to github while downloading {filename}! Ensure that github.com is reachable and try again.", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                throw new TimeoutException();
            }
            throw new Exception($"Something really bad has happened while downloading {filename}!");
        }
        public static bool UnzipFiles(string[] zippedPaths, out string[] unzippedPaths)
        {
            bool result = false;
            List<string> paths = new();
            foreach(string path in zippedPaths)
            {
                string directoryPath = path.Substring(0, path.LastIndexOf('.'));
                try
                {
                    ZipFile.ExtractToDirectory(path, directoryPath);
                    result = true;
                }
                catch(Exception ex) //he he i am sure i won't regret cutting corners later
                {
                    unzippedPaths = [];
                    return false;
                }
                paths.Add(directoryPath);
            }
            unzippedPaths = paths.ToArray();
            return result;
        }
        public static bool HandleCopyingFiles(string[] paths) //well, since the multiplayer mod is inside of it's own folder, i can't just generically move everything into game's directory, so they all get a special treatment.
        {
            static void CloneDirectory(string root, string dest)
            {
                foreach(var directory in Directory.GetDirectories(root))
                {
                    var newDirectory = Path.Combine(dest, Path.GetFileName(directory));
                    Directory.CreateDirectory(newDirectory);
                    CloneDirectory(directory, newDirectory);
                }

                foreach(var file in Directory.GetFiles(root))
                {
                    File.Copy(file, Path.Combine(dest, Path.GetFileName(file)), true);
                }
            }
            int copiedFolders = 0;
            foreach(string path in paths)
            {
                string targetFolder = path.Substring(path.LastIndexOf('\\') + 1);
                if(Installer.GameDownloadURLs[0].Contains(targetFolder))
                {
                    CloneDirectory(path, Installer.GameFolderPath);
                    copiedFolders++;
                    string[] dirs = Directory.GetDirectories(Installer.GameFolderPath);
                    Installer.GameFolderPath = dirs[0];
                    Installer.GamePath = dirs[0] + "\\"+GameName;
                    continue;
                }
                if(Installer.BepinZipArchivePath.Contains(targetFolder))
                {
                    CloneDirectory(path, Installer.GameFolderPath);
                    copiedFolders++;
                    continue;
                }
                if(Installer.ModZipArchivePath.Contains(targetFolder))
                {
                    string[] dirs = Directory.GetDirectories(path);
                    string finalModPath = dirs[0];
                    CloneDirectory(finalModPath, Installer.GameFolderPath);
                    copiedFolders++;
                    continue;
                }
            }
            if (copiedFolders != paths.Length)
            {
                MessageBox.Show("Files were only copied partially! This is very bad, and should never happen. If that's the case, you would want to do a complete reinstallation of the game. If the error persists on a fresh install, consider manual installation.", "Critical Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            return true;

        }
        public static void DeleteTempFiles()
        {
            if(Directory.Exists(GetTempFolderPath())) Directory.Delete(GetTempFolderPath(), true);
        }
    }
}
