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
        private static string GameArchiveFilename="";
        private static string BepinZipFilename="";
        private static string ModZipFilename="";
        public static string SelectedSetupVersion="";
        public static void DiscoverFilenames()
        {
            GameArchiveFilename=GetZipFilename(VersionManager.Instance.Versions["Latest"].Game.ToArray());
            BepinZipFilename=GetZipFilename(VersionManager.Instance.Versions["Latest"].Bepin.ToArray()[0]);
            ModZipFilename=GetZipFilename(VersionManager.Instance.Versions["Latest"].MultiplayerMod.ToArray()[0]);
        }
        public static string GetZipFilename(string[] urls)
        {
            if(urls.Length==0) throw new ArgumentException("Empty URL list provided to filename discovery!");
            if(urls==null) throw new ArgumentNullException("Null URL list provided to filename discovery!");
            if(urls.Length==1)
            {
                return GetZipFilename(urls[0]);
            }
            string[] result=new string[urls.Length];
            for(int i = 0;i<urls.Length;i++)
            {
                result[i]=GetZipFilename(urls[i]);
            }
            string comparer=result[0];
            foreach(string s in result)
            {
                if(!s.Equals(comparer))
                {
                    LogHandler.Instance.Write($"Discovered filename inconsistency in {result[0]}! This is very, very bad and should never happen!");
                    throw new InvalidDataException("Filename inconsistency!");
                }
            }
            return result[0];
        }
        public static bool HandleProvidedGamePath(ref string path)
        {
            string gameName=Constants.GameName;
            if (!path.All(char.IsAscii))
            {
                LogHandler.Instance.Write($"Path contains non-latin characters.");
                throw new ArgumentException("Non-latin characters in the gamepath!");
            }
            if (!File.Exists(path)&&!Directory.Exists(path))
            {
                Installer.GameFolderPath="";
                Installer.GamePath="";
                LogHandler.Instance.Write($"File/dir path does not exist!!");
                throw new ArgumentException("Provided directory or file does not exist!");
            }
            FileAttributes attributes = File.GetAttributes(path);
            if((attributes & FileAttributes.Directory) == FileAttributes.Directory)
            {
                string possibleGamePath = path+Path.DirectorySeparatorChar+gameName;
                if(File.Exists(possibleGamePath))
                {
                    path=possibleGamePath;
                    LogHandler.Instance.Write($"{path} is valid!");
                    Installer.GamePath=possibleGamePath;
                    Installer.GameFolderPath=Path.GetDirectoryName(possibleGamePath);
                    return true;
                }
                else if (Installer.InDownloadMode)
                {
                    LogHandler.Instance.Write($"{path} is a valid download target!");
                    Installer.GameFolderPath=path;
                    return true;
                }
                LogHandler.Instance.Write($"Bad game folder path!");
                throw new ArgumentException("Provided directory does not contain a game executable!");
            }
            if((attributes & FileAttributes.Archive) == FileAttributes.Archive)
            {
                if(Path.GetFileName(path) == gameName)
                {
                    Installer.GamePath=path;
                    Installer.GameFolderPath=Path.GetDirectoryName(path);
                    LogHandler.Instance.Write($"{path} is valid mod installation target!");
                    return true;
                }
                LogHandler.Instance.Write($"Bad game file path!");
                throw new ArgumentException("Provided file is not a game executable!");
            }
            LogHandler.Instance.Write($"{path} is invalid!");
            throw new ArgumentException("Game path mystery error!");
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
            string devName=Constants.DevName;
            string[] gameNames=Constants.GetGameNames(); //Before beta 4, game saved to CasualtiesUnknownDemo. We'll clean both in case it changes again.
            string savefileName=Constants.SavefileName;
            bool result=false;
            foreach(string appdataPath in appdataPaths)
            {
                foreach(string name in gameNames)
                {
                    string path=appdataPath+Path.DirectorySeparatorChar+devName+Path.DirectorySeparatorChar+name+Path.DirectorySeparatorChar+savefileName;
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
        public static bool CheckForSkinMod(string gameFolder)
        {
            LogHandler.Instance.Write($"SkinChange located");
            return File.Exists(gameFolder+$"{Path.DirectorySeparatorChar}BepInEx{Path.DirectorySeparatorChar}plugins{Path.DirectorySeparatorChar}ChangeSkin.dll");
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
                if(!silentExceptions) MessageBox.Show($"File by this path is corrupted and has been downloaded with an invalid checksum!\n\n{ex.Data["path"]}\n\nIf this error persists multiple times, contact the installer developer or consider manual installation!", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                throw new InvalidDataException($"Checksum check failed on {ex.Data["path"]}! File has been deleted!", ex); //well, i really want to rethrow here but i'd rather go with 2 exceptions than a fucked up stack. this is kinda bad
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
        public async static Task<bool> IsChecksumValid(FileStream fs)
        {
            using(SHA256 sha = SHA256.Create())
            {
                fs.Seek(0, SeekOrigin.Begin);
                string path = fs.Name;
                string targetFolder = path.Substring(path.LastIndexOf(Path.DirectorySeparatorChar) + 1);
                if (fs.Length < 1024*3) //3kb
                {
                    return false; //partial download, signature of some filemirrors, they *techincally* allow you to download something, but it's empty.
                }
                byte[] SHA = await sha.ComputeHashAsync(fs);
                if(path.Contains(GameArchiveFilename))
                {
                    //disabled temporarily
                    //return Constants.GetArchiveChecksums()[Constants.ArchiveType.Game].SequenceEqual(SHA);
                    return true;
                }
                if(path.Contains(BepinZipFilename))
                {
                    //return Constants.GetArchiveChecksums()[Constants.ArchiveType.Bepin].SequenceEqual(SHA);
                    return true;
                }
                if(path.Contains(ModZipFilename)) //changeskin magik out
                {
                    return true; //probably the only thing that does change
                                 //fuck me dude we need a proper remote checksum generator
                }
                return false;
            }
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
                string targetFolder = path.Substring(path.LastIndexOf(Path.DirectorySeparatorChar)+1);
                if(FileOperations.GameArchiveFilename.Contains(targetFolder))
                {
                    CloneDirectory(path, Installer.GameFolderPath);
                    copiedFolders++;
                    foreach (string dir in Constants.GetGameNames())
                    {
                        string result = Installer.GameFolderPath+Path.DirectorySeparatorChar+dir;
                        if(Directory.Exists(result))
                        {
                            Installer.GameFolderPath=result;
                            break;
                        }
                    }
                    Installer.GamePath = Installer.GameFolderPath+Path.DirectorySeparatorChar+Constants.GameName;
                    continue;
                }
                if(Installer.BepinArchivePath.Contains(targetFolder))
                {
                    CloneDirectory(path, Installer.GameFolderPath);
                    copiedFolders++;
                    continue;
                }
                if(Installer.ModArchivePath.Contains(targetFolder))
                {
                    string[] dirs = Directory.GetDirectories(path);
                    string finalModPath = dirs[0];
                    CloneDirectory(finalModPath, Installer.GameFolderPath);
                    copiedFolders++;
                    continue;
                }
                if(Installer.ChangeSkinArchivePath.Contains(targetFolder))
                {
                    CloneDirectory(path, Installer.GameFolderPath+Path.DirectorySeparatorChar+"BepInEx"+Path.DirectorySeparatorChar+"plugins");
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
