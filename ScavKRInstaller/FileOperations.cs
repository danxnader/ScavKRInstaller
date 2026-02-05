using System.IO;
using System.Net.Http;
using File = System.IO.File;
using System.Windows;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Windows.Documents.DocumentStructures;
using System.Security.Cryptography;
using System.Data;
using System.Text;

namespace ScavKRInstaller
{
    public static class FileOperations
    {
        private const string GameName = "CasualtiesUnknown.exe";
        private const string DevName = "Orsoniks";
        private const string SavefileName = "save.sv";
        public static bool HandleProvidedGamePath(ref string path)
        {
            string gameName = GameName;
            FileAttributes attributes = File.GetAttributes(path);
            if((attributes & FileAttributes.Directory) == FileAttributes.Directory)
            {
                string possibleGamePath = path+Path.DirectorySeparatorChar+gameName;
                if(File.Exists(possibleGamePath))
                {
                    path = possibleGamePath;
                    LogHandler.Instance.Write($"{path} is valid!");
                    return true;
                }
                LogHandler.Instance.Write($"Bad game folder path!");
                throw new ArgumentException("Provided directory does not contain a game executable!");
            }
            if((attributes & FileAttributes.Archive) == FileAttributes.Archive)
            {
                if(Path.GetFileName(path) == gameName) return true;
                LogHandler.Instance.Write($"Bad game file path!");
                throw new ArgumentException("Provided file is not a game executable!");
            }
            LogHandler.Instance.Write($"{path} is invalid!");
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
            string devName = DevName;
            string[] gameNames = GetGameNames(); //Before beta 4, game saved to CasualtiesUnknownDemo. We'll clean both in case it changes again.
            string savefileName = SavefileName;
            bool result = false;
            foreach(string appdataPath in appdataPaths)
            {
                foreach(string name in gameNames)
                {
                    string path = appdataPath+Path.DirectorySeparatorChar+devName+Path.DirectorySeparatorChar+name+Path.DirectorySeparatorChar+savefileName;
                    if(File.Exists(path))
                    {
                        resultPaths.Add(path);
                        result=true;
                    }
                }
            }
            if(result)
            {
                saveFilePaths=resultPaths.ToArray();
                StringBuilder sb=new();
                foreach(string path in saveFilePaths)
                {
                    sb.AppendJoin(' ', path);
                }
                LogHandler.Instance.Write($"Found savefiles: {sb.ToString()}");
                return true;
            }
            saveFilePaths=[];
            LogHandler.Instance.Write("No savefiles found");
            return false;
        }

        private static string[] GetGameNames()
        {
            return new string[] { "CasualtiesUnknownDemo", "CasualtiesUnknown"};
        }
        private static Dictionary<string, byte[]> GetArchiveChecksums() //wanted to make this smarter where you automatically get checksum by current filename but realized it would take too much effort since i don't really separate those outside of functions and these change at runtime sooooooooo (i should (go rewrite this whole thing you fucking idiot))
        {
            return new Dictionary<string, byte[]>
            {
                { "game", [188, 68, 84, 231, 77, 176, 224, 123, 248, 183, 80, 143, 194, 185, 241, 15, 99, 69, 224, 134, 196, 62, 159, 68, 134, 250, 253, 92, 246, 180, 100, 110, ]},
                { "bepin", [248, 129, 32, 27, 121, 218, 3, 229, 19, 191, 151, 205, 243, 150, 7, 255, 167, 249, 224, 211, 26, 81, 155, 26, 238, 202, 142, 182, 15, 131, 9, 231, ]},
                { "mod", [167, 64, 77, 178, 185, 240, 17, 161, 171, 117, 125, 251, 238, 108, 91, 203, 227, 161, 72, 89, 11, 170, 181, 27, 15, 170, 18, 189, 111, 10, 252, 160, ]}
            };
        }

        public static bool DeleteSavefiles(string[] paths)
        {
            bool hasDeleted = false;
            foreach(string path in paths)
            {
                File.Delete(path);
                hasDeleted=true;
            }
            if(hasDeleted)
            {
                StringBuilder sb = new();
                foreach(string path in paths)
                {
                    sb.AppendJoin(' ', path);
                }
                LogHandler.Instance.Write($"Deleted savefiles: {paths}");
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
            LogHandler.Instance.Write($"Bepin located");
            return Directory.Exists(gameFolder+Path.DirectorySeparatorChar+"BepInEx");
        }
        public static bool CheckForMod(string gameFolder)
        {
            LogHandler.Instance.Write($"Multiplayer mod located");
            return File.Exists(gameFolder+$"{Path.DirectorySeparatorChar}BepInEx{Path.DirectorySeparatorChar}plugins{Path.DirectorySeparatorChar}KrokoshaCasualtiesMP.dll");
        }
        public static async Task<string> TryGameDownload(string[] urls)
        {
            LogHandler.Instance.Write("Trying to download the game");
            foreach(string url in urls)
            {
                try
                {
                    string result = await FileOperations.DownloadArchive(url, true);
                    return result;
                }
                catch (Exception ex)
                {
                    LogHandler.Instance.Write($"Failed to download from source {url}! | {ex.Message}");
                    continue;
                }
            }
            LogHandler.Instance.Write($"!!ALL SOURCES FAILED!!");
            throw new TimeoutException("All game download mirrors failed!");
        }
        public async static Task<string> DownloadArchive(string url, bool silentExceptions = false)
        {
            LogHandler.Instance.Write($"Trying to download an archive: {url}");
            using HttpClient client = new();
            Uri uri = new(url);
            string filename = FileOperations.GetZipFilename(url);
            string tempFolderFilePath = GetTempFolderPath()+Path.DirectorySeparatorChar+filename;
            try
            {
                HttpResponseMessage response = await client.GetAsync(uri);
                using(FileStream fs = new FileStream(tempFolderFilePath, FileMode.Create)) //TODO: fs.Name this retard to statics, get file paths HERE
                {
                    await response.Content.CopyToAsync(fs);
                    if(!await FileOperations.IsChecksumValid(fs))
                    {
                        InvalidDataException ex = new InvalidDataException($"Checksum check failed on {fs.Name}! File has been deleted!");
                        ex.Data.Add("path", fs.Name);
                        LogHandler.Instance.Write($"!!CHECKSUM CHECK ON {filename} FAILED!!");
                        throw ex;
                    };
                    LogHandler.Instance.Write($"Downloaded {filename} successfully!");
                    return tempFolderFilePath;
                }
            }
            catch(InvalidDataException ex)
            {
                File.Delete((string)ex.Data["path"]);
                if(!silentExceptions) MessageBox.Show($"File by this path is corrupted and has been downloaded with an invalid checksum!\n\n{ex.Data}\n\nIf this error persists multiple times, contact the installer developer or consider manual installation!", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                throw ex; //exception rethrow funny 
            }
            catch(TaskCanceledException ex) when(ex.InnerException is TimeoutException)
            {
                if(!silentExceptions) MessageBox.Show($"Connection has timed while downloading {filename}! Ensure that github.com is reachable and try again.", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                LogHandler.Instance.Write($"!!TIMEOUT WHILE DOWNLOADING {filename}!!");
                throw new TimeoutException();
            }
            catch(HttpRequestException ex)
            {
                if(!silentExceptions) MessageBox.Show($"Could not connect to github while downloading {filename}! Ensure that github.com is reachable and try again.", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                LogHandler.Instance.Write($"!!SERVER UNREACHABLE WHILE DOWNLOADING {filename}!!");
                throw new TimeoutException();
            }
            LogHandler.Instance.Write($"DownloadArchive Mystery Error!");
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
                    LogHandler.Instance.Write($"Unzipped {path} to {directoryPath}");
                }
                catch(Exception ex) //he he i am sure i won't regret cutting corners later (i have indeed regretted doing this exact thing (fuck me))
                {
                    unzippedPaths = [];
                    LogHandler.Instance.Write($"!!EXCEPTION WHILE UNZIPPING!! | {ex.ToString()}");
                    return false;
                }
                paths.Add(directoryPath);
            }
            unzippedPaths = paths.ToArray();
            return result;
        }
        public async static Task<bool> IsChecksumValid(FileStream fs) //yep, this sucks! refactor this whole piece of shit of a class already
        {
            SHA256 sha = SHA256.Create();
            fs.Seek(0, SeekOrigin.Begin);
            string path = fs.Name;
            string targetFolder = path.Substring(path.LastIndexOf(Path.DirectorySeparatorChar) + 1);
            byte[] SHA = await sha.ComputeHashAsync(fs);
            if(Installer.GameDownloadURLs[0].Contains(targetFolder))
            {
                return FileOperations.GetArchiveChecksums()["game"].SequenceEqual(SHA);
            }
            if(fs.Name.Contains("BepIn")) //dirty hack time, fuck consistency! i should really refactor this.
            {
                return FileOperations.GetArchiveChecksums()["bepin"].SequenceEqual(SHA);
            }
            if(fs.Name.Contains("main"))
            {
                return true; //probably the only thing that does change
                //fuck me dude we need a proper remote checksum generator
            }
            return false;
        }
        public static bool HandleCopyingFiles(string[] paths) //well, since the multiplayer mod is inside of it's own folder, i can't just generically move everything into game's directory, so they all get a special treatment.
        {
            static void CloneDirectory(string root, string dest)
            {
                LogHandler.Instance.Write($"Cloning directory {root} to {dest}");
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
                string targetFolder = path.Substring(path.LastIndexOf(Path.DirectorySeparatorChar) + 1);
                if(Installer.GameDownloadURLs[0].Contains(targetFolder))
                {
                    CloneDirectory(path, Installer.GameFolderPath);
                    copiedFolders++;
                    foreach (string dir in GetGameNames())
                    {
                        string result = Installer.GameFolderPath+Path.DirectorySeparatorChar+dir;
                        if(Directory.Exists(result))
                        {
                            Installer.GameFolderPath=result;
                            break;
                        }
                    }
                    Installer.GamePath = Installer.GameFolderPath+Path.DirectorySeparatorChar+GameName;
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
                LogHandler.Instance.Write($"!!!PARTIAL COPY ERROR CAUGHT!!!");
                return false;
            }
            return true;

        }
        public static void DeleteTempFiles()
        {
            if(Directory.Exists(GetTempFolderPath())) Directory.Delete(GetTempFolderPath(), true);
            LogHandler.Instance.Write($"Cleared temp files");
        }
    }
}
