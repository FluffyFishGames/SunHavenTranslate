using System;
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
                        if (!Translations.ContainsKey(lines1[i].Trim()))
                        {
                            Translations.Add(lines1[i].Trim(), lines2[i]);
                            if (!AllTranslations.Contains(lines1[i].Trim()))
                                AllTranslations.Add(lines1[i].Trim());
                            if (!AllTranslations.Contains(lines2[i].Trim()))
                                AllTranslations.Add(lines2[i].Trim());
                        }
                    }
                }

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
                            p.progressName = GetString(p.progressName);
                        }
                    }
                }
            }
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
        public static void ChangeText(TMPro.TextMeshProUGUI text)
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
            /** DEBUG
            var textField = text.transform.name + " (" + (text.transform.parent != null ? text.transform.parent.name : "");
            if (!TextFields.Contains(textField))
            {
                TextFields.Add(textField);
                System.IO.File.AppendAllText("textfields.txt", textField + ")\r\n");// ":\r\nFont Size:" + text.fontSize + "\r\nRect height:" + text.rectTransform.rect.height + "\r\nSize delta:" + text.rectTransform.sizeDelta.x + ";" + text.rectTransform.sizeDelta.y + "\r\nAnchor min: " + text.rectTransform.anchorMin.x + ";" + text.rectTransform.anchorMin.y + "\r\nAnchor max: " + text.rectTransform.anchorMax.x + ";" + text.rectTransform.anchorMax.y + "\r\nAnchored Position: " + text.rectTransform.anchoredPosition.x + ";" + text.rectTransform.anchoredPosition.y + "\r\nOffset min: " + text.rectTransform.offsetMin.x + ";" + text.rectTransform.offsetMin.y + "\r\nOffset max: " + text.rectTransform.offsetMax.x + ";" + text.rectTransform.offsetMax.y + "\r\nPivot: " + text.rectTransform.pivot.x + ";" + text.rectTransform.pivot.y + "\r\n");
            }
            */
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
                    if (DialogReplacements.ContainsKey(trimmed))
                        result += line.Substring(0, dialogMarker + 2) + " " + DialogReplacements[trimmed] + (addEnd ? "(End)" : "") + (commandIndex > 0 ? line.Substring(commandIndex) : "") + "\r\n";
                    else
                        result += line + "\r\n";
                }
                else result += line + "\r\n";
            }
            result = result.Substring(0, result.Length - 2);
            return result;
        }

        public static object GetObject(object s)
        {
            if (s is string str)
                return GetString(str);
            else if (s is Enum)
                return GetString(Enum.GetName(s.GetType(), s));
            else return s;
        }

        public static string GetString(string st)
        {
            Initialize();

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
                            if (Translations.ContainsKey(trimmed))
                                ret += Translations[trimmed];
                            else
                            {
                                if (!AllTranslations.Contains(currentStr) && !MissingTranslations.Contains(currentStr))
                                {
                                    MissingTranslations.Add(currentStr);
                                    //System.IO.File.AppendAllText("missing.txt", currentStr + "\r\n");
                                }
                                ret += currentStr;
                            }
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
                if (Translations.ContainsKey(lastTrimmed))
                    ret += Translations[lastTrimmed];
                else
                {
                    if (!AllTranslations.Contains(currentStr) && !MissingTranslations.Contains(currentStr))
                    {
                        MissingTranslations.Add(currentStr);
                        //System.IO.File.AppendAllText("missing.txt", currentStr + "\r\n");
                    }
                    ret += currentStr;
                }
            }
            else
                ret += currentStr;
            
            return ret;
        }
    }
}
