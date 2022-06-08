using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TMPro;
using Wish;

namespace TranslatorPlugin
{
    public class Translator
    {
        private static string Font;
        private static float TextScale;

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
                Ignore.Initialize();
                if (System.IO.File.Exists("options"))
                {
                    var options = System.IO.File.ReadAllText("options");
                    var oo = options.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var o in oo)
                    {
                        var e = o.Split(new char[] { '=' }, 2);
                        if (e.Length == 2)
                        {
                            var key = e[0].ToLowerInvariant();
                            var val = e[1].ToLowerInvariant();
                            if (key == "font")
                                Font = val;
                            else if (key == "text-scale")
                                TextScale = float.Parse(val);
                        }
                    }
                }
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

        static bool FixedQuest = false;
        private static void SceneManager_sceneLoaded(UnityEngine.SceneManagement.Scene arg0, UnityEngine.SceneManagement.LoadSceneMode arg1)
        {
            try
            {
                if (!FixedQuest && Player.Instance != null && Player.Instance.QuestList != null)
                {
                    var quest = Player.Instance.QuestList.GetQuest("TheSunDragonsProtection6Quest");
                    if (quest != null)
                    {
                        FixedQuest = true;
                        var progress = quest.GetProgress("erforsche");
                        if (progress > 0.2f)
                        {
                            quest.SetProgress("erforsche", 0f);
                            quest.SetProgress("investigate", 1f);
                        }
                    }
                }
            }
            catch (Exception e)
            {

            }
            if (!QuestAssetsParsed)
            {

                var skillNodes = UnityEngine.Resources.FindObjectsOfTypeAll(typeof(SkillNodeAsset));
                foreach (var s in skillNodes)
                {
                    if (s is SkillNodeAsset skillNode)
                    {
                        skillNode.description = TranslateString(skillNode.description, "");
                        skillNode.nodeName = TranslateString(skillNode.nodeName, "");
                        skillNode.nodeTitle = TranslateString(skillNode.nodeTitle, "");
                        skillNode.singleDescriptionItem = TranslateString(skillNode.singleDescriptionItem, "");
                        for (var i = 0; i < skillNode.descriptionItems.Count; i++)
                            skillNode.descriptionItems[i] = TranslateString(skillNode.descriptionItems[i], "");
                        for (var i = 0; i < skillNode.thirdDescriptionItems.Count; i++)
                            skillNode.thirdDescriptionItems[i] = TranslateString(skillNode.thirdDescriptionItems[i], "");
                    }
                }

                var questAssets = UnityEngine.Resources.LoadAll<Wish.QuestAsset>("Quest Assets");
                foreach (var quest in questAssets)
                {
                    if (quest.questProgressRequirements != null)
                    {
                        foreach (var p in quest.questProgressRequirements)
                        {
                            var newProgressName = TranslateString(p.progressName, "");
                            if (p.progressName == newProgressName && Translator.IsUnknown(p.progressName) && !Extractor.Unknown.Contains(p.progressName))
                            {
                                // it wasn't changed. Cache it in the extractor for extraction.
                                Extractor.Unknown.Add(p.progressName);
                            }
                            //System.IO.File.AppendAllText("test.log", "Replaced string " + p.progressName + " with " + newProgressName + "\r\n");
                            p.progressName = newProgressName;
                            for (var k = 0; k < p.questProgress.Count; k++)
                            {
                                var progressText = TranslateString(p.questProgress[k].progress, "");
                                if (progressText == p.questProgress[k].progress && Translator.IsUnknown(p.questProgress[k].progress) && !Extractor.Unknown.Contains(p.questProgress[k].progress))
                                {
                                    Extractor.Unknown.Add(p.questProgress[k].progress);
                                }
                            }
                        }
                    }

                    if (quest.killRequirements != null)
                    {
                        foreach (var p in quest.killRequirements)
                        {
                            var newProgressName = TranslateString(p.enemy, "");
                            if (p.enemy == newProgressName && Translator.IsUnknown(p.enemy) && !Extractor.Unknown.Contains(p.enemy))
                            {
                                // it wasn't changed. Cache it in the extractor for extraction.
                                Extractor.Unknown.Add(p.enemy);
                            }
                            p.enemy = newProgressName;
                        }
                    }

                    if (quest.itemRequirements != null)
                    {
                        foreach (var p in quest.itemRequirements)
                        {
                            var newProgressName = TranslateString(p.extraText, "");
                            if (p.extraText == newProgressName && Translator.IsUnknown(p.extraText) && !Extractor.Unknown.Contains(p.extraText))
                            {
                                // it wasn't changed. Cache it in the extractor for extraction.
                                Extractor.Unknown.Add(p.extraText);
                            }
                            p.extraText = newProgressName;
                        }
                    }
                }

                QuestAssetsParsed = true;

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
            "EXPTMP",
            "ExpTMP",
            "ExpTitleMP"
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
            "FloatingText(Clone)",
            "TooltipPanel",
            "Spl Panel",
            "background"
        };

        private static HashSet<string> TextFields = new HashSet<string>();
        private static TMP_FontAsset FontAsset;

        public static void ChangeTextMesh(TMPro.TextMeshProUGUI text)
        {
            /*
            if (text.transform.parent != null && !TextFields.Contains(text.transform.parent.name))
            {
                TextFields.Add(text.transform.parent.name);
                System.IO.File.AppendAllText("text.txt", text.transform.parent.name + "\r\n");
            }
            if (!TextFields.Contains(text.transform.name))
            {
                TextFields.Add(text.transform.name);
                System.IO.File.AppendAllText("text.txt", text.transform.name + "\r\n");
            }*/

            if (Font != "default")
            {
                if (FontAsset == null)
                {
                    MaterialReferenceManager.TryGetFontAsset(Font.GetHashCode(), out FontAsset);

                    if (FontAsset == null)
                    {
                        FontAsset = UnityEngine.Resources.Load<TMP_FontAsset>(TMP_Settings.defaultFontAssetPath + Font);
                        if (FontAsset == null)
                        {
                            try
                            {
                                var path = System.IO.Path.Combine(Environment.CurrentDirectory, Font.ToLowerInvariant());
                                //System.IO.File.AppendAllText("log.txt", "Trying to load font " + Font + " from " + path + "!\r\n");
                                if (System.IO.File.Exists(path))
                                {
                                    var bytes = System.IO.File.ReadAllBytes(path);
                                    //System.IO.File.AppendAllText("log.txt", "Loaded " + bytes.Length + " bytes!\r\n");
                                    var bundle = UnityEngine.AssetBundle.LoadFromMemory(bytes);
                                    //System.IO.File.AppendAllText("log.txt", "Asset bundle: " + bundle + "!\r\n");
                                    FontAsset = bundle.LoadAsset<TMP_FontAsset>("Assets/" + Font + ".asset");
                                    //System.IO.File.AppendAllText("log.txt", "Font " + Font + " found in assetbundle!\r\n");
                                }

                            }
                            catch (Exception e)
                            {
                            }
                        }
                        if (FontAsset != null)
                            MaterialReferenceManager.AddFontAsset(FontAsset);
                    }
                }
                if (FontAsset != null)
                    text.font = FontAsset;
            }
            if (TextScale != 100)
            {
                text.fontSize = text.fontSize * (TextScale / 100f);
                text.fontSizeMin = text.fontSizeMin * (TextScale / 100f);
                text.fontSizeMax = text.fontSizeMax * (TextScale / 100f);
            }
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

        public static string TrimAndGetWhitespaces(string str, out string before, out string after)
        {
            var emptySpacesBefore = 0;
            var emptySpacesAfter = 0;
            for (var i = 0; i < str.Length; i++)
            {
                if (str[i] != ' ')
                    break;
                emptySpacesBefore++;
            }
            for (var i = str.Length - 1; i >= 0; i--)
            {
                if (str[i] != ' ')
                    break;
                emptySpacesAfter++;
            }
            before = new string(' ', emptySpacesBefore);
            after = new string(' ', emptySpacesAfter);
            return str.Trim();
        }

        public static string TranslateDialog(string text)
        {
            var lines = text.Split(new string[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
            var result = "";
            for (var k = 0; k < lines.Length; k++)
            {
                var line = lines[k];
                var dialogMarker = line.LastIndexOf("::");
                if (dialogMarker > 0)
                {
                    var dialogLine = line.Substring(dialogMarker + 3);
                    var addEnd = false;
                    var command = "";
                    var commandIndex = dialogLine.IndexOf("//");
                    if (commandIndex > 0)
                    {
                        command = dialogLine.Substring(commandIndex);
                        dialogLine = dialogLine.Substring(0, commandIndex);
                    }
                    if (dialogLine.EndsWith("(End)"))
                    {
                        addEnd = true;
                        dialogLine = dialogLine.Substring(0, dialogLine.Length - 5);
                    }

                    var trimmed = dialogLine.Trim();
                    var parts = trimmed.Split(new string[] { "[]" }, StringSplitOptions.None);
                    var newLine = "";
                    var first = true;
                    foreach (var part in parts)
                    {
                        var p = TrimAndGetWhitespaces(part, out var trimBefore, out var trimAfter);
                        if (!first)
                            newLine += "[]";
                        if (Translations.ContainsKey("Dialogue||" + p))
                            newLine += trimBefore + Translations["Dialogue||" + p] + trimAfter;
                        else
                        {
                            Extractor.AddUnknown(p);
                            newLine += trimBefore + p + trimAfter;
                        }
                        first = false;
                    }
                    newLine = line.Substring(0, dialogMarker + 2) + " " + newLine + (addEnd ? "(End)" : "") + command + "\r\n";
                    result += newLine;
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


        private static Regex ParenthesisRegex = new Regex("\\(([^\\)]+)\\)");

        private static string TranslateLine(string st, string context)
        {
            var ret = "";
            var parts = st.Split(new string[] { "[]" }, StringSplitOptions.None);
            bool first = true;
            for (var i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                var trimmed = TrimAndGetWhitespaces(part, out var trimBefore, out var trimAfter);
                if (trimmed != "")
                {
                    var commandIndex = trimmed.LastIndexOf("//");
                    string command = "";
                    if (commandIndex > 0)
                    {
                        command = trimmed.Substring(commandIndex);
                        trimmed = trimmed.Substring(0, commandIndex);
                    }
                    if (context != null && Translations.ContainsKey(context + "||" + trimmed))
                        ret += (!first ? "[]" : "") + trimBefore + Translations[context + "||" + trimmed] + trimAfter + command;
                    else if (Translations.ContainsKey(trimmed))
                        ret += (!first ? "[]" : "") + trimBefore + Translations[trimmed] + trimAfter + command;
                    else if (trimmed.EndsWith(")"))
                    {
                        var index = trimmed.LastIndexOf("(");

                        var p1 = trimmed.Substring(0, index).Trim();
                        var spaces = index - p1.Length;
                        var p2 = trimmed.Substring(index + 1, trimmed.Length - (index + 1) - 1).Trim();

                        if (Translations.ContainsKey(p1))
                            p1 = Translations[p1];
                        //else
                        //    Extractor.AddUnknown(p1);
                        if (Translations.ContainsKey(p2))
                            p2 = Translations[p2];
                        //else
                        //    Extractor.AddUnknown(p1);
                        ret += (!first ? "[]" : "") + trimBefore + p1 + new string(' ', spaces) + "(" + p2 + ")" + trimAfter + command;
                    }
                    else
                    {
                        Extractor.AddUnknown(part);
                        ret += (!first ? "[]" : "") + part + command;
                    }
                }
                else
                    ret += (!first ? "[]" : "") + part;
                first = false;
            }
            return ret;
        }
        public static string TranslateString(string st, string context)
        {
            Initialize();
            if (st == null) return null;
            context = context.Trim();
            var currentStr = "";
            var ret = "";
            for (var i = 0; i < st.Length; i++)
            {
                if (st[i] == '\r' || st[i] == '\n')
                {
                    if (currentStr != "")
                    {
                        ret += TranslateLine(currentStr, context);
                    }
                    ret += st[i];
                    currentStr = "";
                }
                else currentStr += st[i];
            }

            ret += TranslateLine(currentStr, context);
            return ret;
        }
    }
}
