using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Wish;

namespace TranslatorPlugin
{
    public class Extractor : MonoBehaviour
    {
        public static HashSet<string> Unknown = new HashSet<string>();

        private static Dictionary<string, List<string>> Members = new Dictionary<string, List<string>>()
        {
            { "AnimalData", new List<string>() { "name", "description", "shopDescription" } },
            { "ArmorData", new List<string>() { "name", "description", "shopDescription" } },
            { "DecorationData", new List<string>() { "name", "description", "shopDescription" } },
            { "FoodData", new List<string>() { "name", "description", "shopDescription" } },
            { "PetData", new List<string>() { "name", "description", "shopDescription" } },
            { "QuestItemData", new List<string>() { "name", "description", "shopDescription" } },
            { "RecordData", new List<string>() { "name", "description", "shopDescription" } },
            { "WallpaperData", new List<string>() { "name", "description", "shopDescription" } },
            { "WateringCanData", new List<string>() { "name", "description", "shopDescription" } },
            { "CropData", new List<string>() { "name", "description", "shopDescription" } },
            { "FishData", new List<string>() { "name", "description", "shopDescription" } },
            { "SeedData", new List<string>() { "name", "description", "shopDescription" } },
            { "ItemData", new List<string>() { "name", "description", "shopDescription" } },
            { "QuestAsset", new List<string>() { "endTex", "questDescription", "bulletinBoardDescription", "questName", "worldProgress", "characterProgress" } },
            { "TextMeshProUGUI", new List<string>() { "m_text" } },
            { "SceneSettings", new List<string>() { "formalSceneName" } },
            { "MailAsset", new List<string>() { "message" } },
            { "ClothingLayerData", new List<string>() { "menuName" } },
            { "BookShelf", new List<string>(){ "bookName", "text" } },
            { "Popup", new List<string>(){ "text", "description" } },
            { "SkillNode", new List<string>(){ "nodeTitle", "nodeName", "description" } },
            { "NPCAI", new List<string>(){ "title", "questLocation" } },
            { "Inspectable", new List<string>() { "inspectionText", "dialogueName", "interactionText" } },
            { "EliosFountain", new List<string>() { "interactionText" } },
            { "RepairSign", new List<string>() { "text" } },
            { "ScenePortalSpot", new List<string>() { "_cantEnterNotificiation", "enterOverrideText" } },
            { "EnemyAI", new List<string>() { "enemyName" } },
            { "QuestProgressRequirement", new List<string>() { "progressName" } },
            { "QuestProgress", new List<string>() { "progress" } }
        };

        private Dictionary<Type, List<FieldInfo>> Fields = new Dictionary<Type, List<FieldInfo>>();

        void Start()
        {
            foreach (var t in Members)
            {
                var type = Type.GetType("Wish." + t.Key);
                if (type != null)
                {
                    var members = type.GetFields();
                    List<FieldInfo> results = new List<FieldInfo>();
                    foreach (var member in members)
                    {
                        if (t.Value.Contains(member.Name))
                            results.Add(member);
                    }

                    if (results.Count > 0)
                        Fields.Add(type, results);
                }
            }
        }

        private int CurrentScene = 0;
        
        void Extract()
        {
            if (Application.levelCount > CurrentScene)
            {
                var log = "Loading scene " + CurrentScene + "\r\n";
                Application.LoadLevel(CurrentScene);

                var assets = new List<object>();
                foreach (var f in Fields)
                {
                    var ass = UnityEngine.Resources.FindObjectsOfTypeAll(f.Key);
                    if (ass != null)
                    {
                        log += "Found " + ass.Length + " assets of type " + f.Key.FullName + "\r\n";
                        assets.AddRange(ass);
                    }
                }

                ParseAssets(assets);
                CurrentScene++;
                System.IO.File.AppendAllText("log.txt", log);
                this.Extract();
            }
            else
            {
                System.IO.File.WriteAllText("unknown.txt", String.Join("\r\n", Unknown));
            }
        }

        void ParseAssets(List<object> assets)
        {
            var log = "Parsing " + assets.Count + " assets...\r\n";
            foreach (var asset in assets)
            {
                if (asset != null)
                {
                    var t = asset.GetType();
                    if (Fields.ContainsKey(t))
                    {
                        foreach (var f in Fields[t])
                        {
                            var val = f.GetValue(asset);
                            if (val != null)
                            {
                                if (val is string s)
                                {
                                    var lines = s.Split(new char[] { '\r', '\n' });
                                    foreach (var line in lines)
                                    {
                                        var trimmed = line.Trim();
                                        if (Translator.IsUnknown(trimmed) && !Unknown.Contains(trimmed))
                                            Unknown.Add(trimmed);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            System.IO.File.AppendAllText("log.txt", log);
        }

        void Update()
        {
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.R) && Input.GetKeyDown(KeyCode.T))
            {
                var log = "";
                foreach (var t in Members)
                {
                    Assembly assembly = null;
                    foreach (Assembly ass in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        if (System.IO.Path.GetFileNameWithoutExtension(ass.Location) == "Assembly-CSharp")
                        {
                            assembly = ass;
                            break;
                        }
                    }

                    if (assembly != null)
                    {
                        try
                        {
                            log += "Assembly found\r\n";
                            var type = assembly.GetType("Wish." + t.Key);
                            if (type != null)
                            {
                                log += "Type " + type.FullName + " found\r\n";
                                var members = type.GetFields();
                                List<FieldInfo> results = new List<FieldInfo>();
                                foreach (var member in members)
                                {
                                    if (t.Value.Contains(member.Name))
                                    {
                                        log += "Member " + member.Name + " found\r\n";
                                        results.Add(member);
                                    }
                                }

                                if (results.Count > 0)
                                    Fields.Add(type, results);
                            }
                        }
                        catch (Exception e)
                        {
                            log += e.ToString() + "\r\n";
                        }
                    }
                }

                System.IO.File.WriteAllText("log.txt", log);
                try
                {
                    var assets = new List<object>();
                    assets.AddRange(UnityEngine.Resources.LoadAll<Wish.QuestAsset>("Quest Assets"));
                    ParseAssets(assets);

                    Extract();
                }
                catch (Exception e)
                {
                    System.IO.File.AppendAllText("log.txt", e.ToString() + "\r\n");
                }
            }
        }
    }
}
