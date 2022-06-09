using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SunHavenTranslate
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            ModManagerGUI.ModManager.Start(new Mod(), new ModManagerGUI.Configuration()
            {
                ApplicationName = "Sun Haven Translator",
                GameName = "Sun Haven",
                FileNames = new string[] { "Sun Haven" },
                DeveloperName = "Pixelsprout Studios",
                SteamAppID = "1432860",
                AdditionalMods = new string[][] { new string[] { "Jederzeit schlafen können", "Sleep anytime" }, new string[] { "Immer neue Dialoge (Schnelle Romanze)", "Always new dialogue (fast romance)" } }
            });
        }
    }
}
