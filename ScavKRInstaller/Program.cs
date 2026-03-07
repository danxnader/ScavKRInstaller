using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ScavKRInstaller
{
    public static class Program
    {
        private static bool serviceMode=false;
        private static bool quit = false;
        [DllImport("kernel32.dll", SetLastError=true)]
        private static extern bool AttachConsole(int dwProcessId);
        [DllImport("kernel32.dll", SetLastError=true)]
        private static extern bool FreeConsole();
        [STAThread]
        public static void Main(string[] args)
        {
            serviceMode=AttachConsole(-1);
            if(serviceMode)
            {
                Console.Write("\n");
                LogHandler.Instance.Logged+=Instance_Logged;
                LogHandler.Instance.Write("Console attached, running in service mode.");
                string path="";
                if(args.Length==0 || args.Contains("-H") || args.Contains("--help"))
                {
                    LogHandler.Instance.Write($@"
ScavKRInstaller rev. {Constants.Version} service mode

Example: ScavKRInstaller.exe -i [path]

Argument list:
-i [path] : destination path. If game executable is provided, the mod is installed. If directory is provided, the game is downloaded.
-s : do not clean savefiles.");
                    goto exitSilently;
                }
                for(int i = 0; i < args.Length; i++)
                {
                    if (args[i]=="-i")
                    {
                        if(args[i+1]!=null)
                        {
                            path=args[i+1];
                            i++;
                            if (File.Exists(path))
                            {
                                Installer.InDownloadMode=false;
                            }
                            else if (Directory.Exists(path))
                            {
                                Installer.InDownloadMode=true;
                            }
                            else
                            {
                                LogHandler.Instance.Write("Provided path is invalid.");
                                goto exit;
                            }
                        }
                        else
                        {
                            goto exit;
                        }
                    }
                    if(args[i]=="-s")
                    {
                        Installer.DeleteSavefile=false;
                    }
                }
                if(path!=String.Empty)
                {
                    try
                    {
                        InstallerTask.Fetch().GetAwaiter().GetResult();
                    }
                    catch(NullReferenceException ex)
                    {
                        LogHandler.Instance.Write("Failed to fetch sources");
                        goto exit;
                    }
                    catch (Exception ex)
                    {
                        LogHandler.Instance.Write($"Something magical has happened while fetching:\n{ex.ToString()}");
                    }
                    InstallerTask.Install([path]).GetAwaiter().GetResult();
                }
                else
                {
                    LogHandler.Instance.Write("No provided directory.");
                }
            }
            else
            {
                var app = new App();
                app.InitializeComponent();
                app.Run();
            }
        exit:
            if(serviceMode)
            {
                LogHandler.Instance.Write("Quiting");
            }
        exitSilently:
            FreeConsole();
        }

        private static void Instance_Logged()
        {
            Console.WriteLine(LogHandler.Instance.GetLastEntry());
        }
    }
}
