using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ScavKRInstaller
{
    public static class Constants
    {
        public static readonly string Version = "1.2.1";
        public static readonly string GameName = "CasualtiesUnknown.exe";
        public static readonly string DevName = "Orsoniks";
        public static readonly string SavefileName = "save.sv";
        public static readonly string SourcelistURL = @"https://gist.github.com/danxnader/3a655ba7b868999b65b71856a765499a/raw/sources.json";
        public static string[] GetGameNames()
        {
            return new string[] { "CasualtiesUnknownDemo", "CasualtiesUnknown" };
        }
        public static Dictionary<ArchiveType, byte[]> GetArchiveChecksums()
        {
            return new Dictionary<ArchiveType, byte[]>
            {
                { ArchiveType.Game, [188, 68, 84, 231, 77, 176, 224, 123, 248, 183, 80, 143, 194, 185, 241, 15, 99, 69, 224, 134, 196, 62, 159, 68, 134, 250, 253, 92, 246, 180, 100, 110, ]},
                { ArchiveType.Bepin, [248, 129, 32, 27, 121, 218, 3, 229, 19, 191, 151, 205, 243, 150, 7, 255, 167, 249, 224, 211, 26, 81, 155, 26, 238, 202, 142, 182, 15, 131, 9, 231, ]},
                { ArchiveType.Mod, [167, 64, 77, 178, 185, 240, 17, 161, 171, 117, 125, 251, 238, 108, 91, 203, 227, 161, 72, 89, 11, 170, 181, 27, 15, 170, 18, 189, 111, 10, 252, 160, ]}
            };
        }
        public enum ArchiveType
        {
            Game,
            Bepin,
            Mod
        }
        public static string FallbackVersionFilePath
        {
            get
            {
                string path = String.Join(Path.DirectorySeparatorChar.ToString(), Directory.GetCurrentDirectory(), "sources.json");
                if(File.Exists(path))
                {
                    return path;
                }
                return null;
            }
        }
        public static string[] GetSplash()  
        {
            string[] Splashes = //31 chars max
            {
                "FENTANYL: ADMINISTERED",
                "I don't feel my legs!",
                "glubglub",
                "Eat the mushroom",
                "Sleeping pills + Heroin",
                "mmm tasty yellow meat",
                "Drowning in a puddle",
                "Angry Salad",
                "2296",
                "Why do expies have 5 liters of blood if they're physically smaller? It doesn't make sense, they must have very high blood pressure or their body most accomodate for a significantly larger volume.",
                "Landmines under bushes",
                "Ridiculous orange eyes",
                "Woundview shitcode!",
                "Unity networking sucks",
                "Your gun will jam",
                "Uranium is warm",
                "Mystery barrels!",
                "Mystery pills!",
                "Take a sip",
                "Let's not give up just yet",
                "Sad, yet hopeful...",
                "3.39l | 0.86l/m",
                "Don't break your claws",
                "Bones are surprisingly fragile",
                "I want to rewrite this on c",
                $"I know you, {Environment.UserName}",
                "Ketchup Intravenously",
                "Thanks losyrr!",
                "Thanks GaryTheCat!",
                "Thanks Arcarnisan!",
                "Go play Desecrators",
                "Go play CleanFall",
                "3 turbulence crystals",
                "Eat your friends!",
                "Push your friends onto mines!",
                "Inject drugs into your friends!",
                "Soda (3049mL), Fentanyl (364mL)",
                "Professional Cannibalism",
                "You can kinda do PvP too"
            };
            return Splashes;
        }
    }
}
