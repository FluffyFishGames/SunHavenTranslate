﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;

namespace TranslatorPlugin
{
    public class Translator
    {
        private static HashSet<string> AllTranslations = new HashSet<string>();
        private static HashSet<string> MissingTranslations = new HashSet<string>();
        private static Dictionary<string, string> Translations = new Dictionary<string, string>();
        private static Dictionary<string, string> DialogReplacements = new Dictionary<string, string>();
        private static bool Initialized = false;
        private static Dictionary<Type, int> RipCount = new Dictionary<Type, int>();
        private static bool QuestAssetsParsed = false;

        public static void Initialize()
        {
            if (!Initialized)
            {
                Initialized = true;
                var lines1 = System.IO.File.ReadAllLines("table.orig");
                var lines2 = System.IO.File.ReadAllLines("table.trans");
                if (lines2.Length >= lines1.Length)
                {
                    for (var i = 0; i < lines1.Length; i++)
                    {
                        var l = lines1[i];
                        var ind = l.IndexOf("||");
                        if (ind > 0)
                            l = l.Substring(0, ind).Trim() + "||" + l.Substring(ind + 2).Trim();
                        else l = l.Trim();
                        //System.IO.File.AppendAllText("log.txt", l + "\r\n");
                        if (!Translations.ContainsKey(l))
                        {
                            Translations.Add(l, lines2[i]);
                            AllTranslations.Add(l.Trim());
                            AllTranslations.Add(lines2[i].Trim());
                            /*                            if (!AllTranslations.Contains(lines1[i].Trim()))
                                                            AllTranslations.Add(lines1[i].Trim());
                                                        if (!AllTranslations.Contains(lines2[i].Trim()))
                                                            AllTranslations.Add(lines2[i].Trim());*/
                        }
                    }
                }
                /*
                var dialogLines0 = System.IO.File.ReadAllLines("dialogs.orig");
                var dialogLines1 = System.IO.File.ReadAllLines("dialogs.trans");

                for (var j = 0; j < dialogLines0.Length; j++)
                {
                    var line = dialogLines0[j].Trim();
                    if (!DialogReplacements.ContainsKey(line))
                        DialogReplacements.Add(line, dialogLines1[j]);
                    if (!AllTranslations.Contains(dialogLines0[j].Trim()))
                        AllTranslations.Add(dialogLines0[j].Trim());
                    if (!AllTranslations.Contains(dialogLines1[j].Trim()))
                        AllTranslations.Add(dialogLines1[j].Trim());
                }
                */
                /*
                System.IO.File.WriteAllText("log.txt", Translations.Count + " translations and " + DialogReplacements.Count + " dialog replacements loaded!\r\n");
                System.IO.File.WriteAllText("missing.txt", "");
                System.IO.File.WriteAllText("textfields.txt", "");
                */

                UnityEngine.SceneManagement.SceneManager.sceneLoaded += SceneManager_sceneLoaded;
            }
        }

        private static void SceneManager_sceneLoaded(UnityEngine.SceneManagement.Scene arg0, UnityEngine.SceneManagement.LoadSceneMode arg1)
        {
            if (!QuestAssetsParsed)
            {
                QuestAssetsParsed = true;
                var questAssets = UnityEngine.Resources.LoadAll<Wish.QuestAsset>("Quest Assets");
                foreach (var quest in questAssets)
                {
                    if (quest.questProgressRequirements != null)
                    {
                        foreach (var p in quest.questProgressRequirements)
                        {
                            var newProgressName = TranslateString(p.progressName, "");
                            if (p.progressName == newProgressName && !Extractor.Unknown.Contains(p.progressName))
                            {
                                // it wasn't changed. Cache it in the extractor for extraction.
                                Extractor.Unknown.Add(p.progressName);
                            }
                            p.progressName = newProgressName;
                        }
                    }
                }

                var go = new UnityEngine.GameObject();
                UnityEngine.Object.DontDestroyOnLoad(go);
                go.AddComponent<Extractor>();
            }
        }

        public static bool IsUnknown(string line)
        {
            return !AllTranslations.Contains(line.Trim());
        }

        public static void CheckForUnknown(string text)
        {
            if (!text.Contains("<alpha=#00 id = \"a\">"))
            {
                var lines = text.Split(new char[] { '\r', '\n' });
                foreach (var line in lines)
                {
                    if (!AllTranslations.Contains(line.Trim()) && !MissingTranslations.Contains(line.Trim()))
                    {
                        MissingTranslations.Add(line.Trim());
                        //System.IO.File.AppendAllText("missing.txt", line + "\r\n");
                    }
                }
            }
        }

        private static HashSet<string> DontTouchText = new HashSet<string>() {
            "CoinsTMP",
            "OrbsTMP",
            "TicketsTMP",
            "ResolutionTMP",
            "ZoomLevelTMP",
            "ResolutionTMP",
            "DaySpeedTMP",
            "ZoomTMP",
            "SoundEffectTMP",
            "AmbientTMP",
            "MasterVolumeTMP",
            "MusicTMP",
            "AmountTMP",
            "HotkeyTMP",
            "HealthTMP",
            "ManaTMP",
            "PriceTMP",
            "ItemName",
            "Option(Clone)",
            "LevelTMP",
            "EXPTMP"
        };

        private static HashSet<string> DontTouchTextParent = new HashSet<string>() {
            "PlayButton",
            "PlayButton (1)",
            "PlayButton (2)",
            "PlayButton (3)",
            "Volume",
            "Dropdown",
            "AttackPanel",
            "Def Panel",
            "NPCName",
            "BuyButon",
            "Interaction(Clone)",
            "Nameplate",
            "FloatingText(Clone)"
        };

        private static HashSet<string> TextFields = new HashSet<string>();
        public static void ChangeTextMesh(TMPro.TextMeshProUGUI text)
        {
            if (!text.autoSizeTextContainer)
            {
                if (!DontTouchText.Contains(text.transform.name) && (text.transform.parent == null || !DontTouchTextParent.Contains(text.transform.parent.name)))
                {
                    text.fontSizeMax = text.fontSize;
                    text.fontSizeMin = text.fontSize / 3f;
                    text.enableAutoSizing = true;
                }
            }
        }

        public static string TranslateDialog(string text)
        {
            var lines = text.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            var result = "";
            for (var k = 0; k < lines.Length; k++)
            {
                var line = lines[k];
                var dialogMarker = line.IndexOf("::");
                if (dialogMarker > 0)
                {
                    var dialogLine = line.Substring(dialogMarker + 3);
                    var commandIndex = dialogLine.IndexOf("//");
                    if (commandIndex > 0)
                        dialogLine = dialogLine.Substring(0, commandIndex - 1);
                    var addEnd = false;
                    if (dialogLine.EndsWith("(End)"))
                    {
                        addEnd = true;
                        dialogLine = dialogLine.Substring(0, dialogLine.Length - 5);
                    }

                    commandIndex = line.IndexOf("//");
                    var trimmed = dialogLine.Trim();
                    if (Translations.ContainsKey("Dialogue||" + trimmed))
                        result += line.Substring(0, dialogMarker + 2) + " " + Translations["Dialogue||" + trimmed] + (addEnd ? "(End)" : "") + (commandIndex > 0 ? line.Substring(commandIndex) : "") + "\r\n";
                    else
                        result += line + "\r\n";
                }
                else result += line + "\r\n";
            }
            result = result.Substring(0, result.Length - 2);
            return result;
        }

        public static object TranslateObject(object s, string context)
        {
            if (s is string str)
                return TranslateString(str, context);
            else if (s is Enum)
                return TranslateString(Enum.GetName(s.GetType(), s), context);
            else return s;
        }

        public static string TranslateString(string st, string context)
        {
            Initialize();
            context = context.Trim();
            var currentStr = "";
            var ret = "";
            for (var i = 0; i < st.Length; i++)
            {
                if (st[i] == '\r' || st[i] == '\n')
                {
                    if (currentStr != "")
                    {
                        var trimmed = currentStr.Trim();
                        if (trimmed != "")
                        {
                            if (context != null && Translations.ContainsKey(context + "||" + trimmed))
                                ret += Translations[context + "||" + trimmed];
                            else if (Translations.ContainsKey(trimmed))
                                ret += Translations[trimmed];
                            else
                                ret += currentStr;
                        }
                        else ret += currentStr;
                    }
                    ret += st[i];
                    currentStr = "";
                }
                else currentStr += st[i];
            }
            var lastTrimmed = currentStr.Trim();
            if (lastTrimmed != "")
            {
                if (context != null && Translations.ContainsKey(context + "||" + lastTrimmed))
                    ret += Translations[context + "||" + lastTrimmed];
                else if (Translations.ContainsKey(lastTrimmed))
                    ret += Translations[lastTrimmed];
                else
                    ret += currentStr;
            }
            else
                ret += currentStr;
            return ret;
        }
    }
}
