using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;

namespace ScavKRInstaller
{
    public static class Constants
    {
        public static string Version = "1.1.15";
        public static string[] GetSplash()
        {
            string[] Splashes = 
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
                "Let's not give up yet",
                "Sad, yet hopeful...",
                "3.39l | 0.86l/m",
                "Don't break your claws",
                "Bones are surprisingly fragile",
                "I want to rewrite this on c",
                $"I know you, {Environment.UserName}"
            };
            return Splashes;
        }
    }
}
