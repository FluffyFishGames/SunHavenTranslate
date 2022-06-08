using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Wish;

namespace TranslatorPlugin
{
    public class Extractor : MonoBehaviour
    {
        public static HashSet<string> Unknown = new HashSet<string>();

        private static Dictionary<string, List<string>> Members = new Dictionary<string, List<string>>()
        {
            /*{ "AnimalData", new List<string>() { "name", "description", "shopDescription" } },
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
            { "ItemData", new List<string>() { "name", "description", "shopDescription" } },*/
            { "QuestAsset", new List<string>() { "overrideTurnInText", "farewellText", "endTex", "questDescription", "bulletinBoardDescription", "questName", "worldProgress", "characterProgress" } },
            /*{ "TextMeshProUGUI", new List<string>() { "m_text" } },
            { "SceneSettings", new List<string>() { "formalSceneName" } },*/
            /*{ "MailAsset", new List<string>() { "message" } },*/
            /*{ "ClothingLayerData", new List<string>() { "menuName" } },
            { "BookShelf", new List<string>(){ "bookName", "text" } },
            { "Popup", new List<string>(){ "text", "description" } },
            { "SkillNode", new List<string>(){ "nodeTitle", "nodeName", "description" } },
            { "NPCAI", new List<string>(){ "title", "questLocation" } },
            { "Inspectable", new List<string>() { "inspectionText", "dialogueName", "interactionText" } },
            { "EliosFountain", new List<string>() { "interactionText" } },
            { "RepairSign", new List<string>() { "text" } },
            { "ScenePortalSpot", new List<string>() { "_cantEnterNotificiation", "enterOverrideText" } },*/
            { "EnemyAI", new List<string>() { "enemyName" } },
            { "QuestProgressRequirement", new List<string>() { "progressName" } },
            { "QuestKillRequirement", new List<string>() { "enemy" } },
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
        
        public static void AddUnknown(string unknown)
        {
            if (!Unknown.Contains(unknown) && Translator.IsUnknown(unknown))
            {
                Unknown.Add(unknown);
                //System.IO.File.AppendAllText("unknown.log", unknown + "\r\n");
            }
        }

        AsyncOperation op;
        bool wait = false;
        void Extract()
        {
            if (wait)
                return;
            if (op != null && !op.isDone)
                return;

            //var scenes = SceneManager.GetAllScenes();//
            if (CurrentScene >= SceneManager.sceneCountInBuildSettings)
            {
                System.IO.File.AppendAllText("log.txt", "Finished\r\n");
                Extracting = false;
                return;
            }

            
            var sceneName = System.IO.Path.GetFileNameWithoutExtension(UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(CurrentScene));
            System.IO.File.AppendAllText("log.txt", "Scene: " + sceneName + "\r\n");
            CurrentScene++;
            if (sceneName.StartsWith("Mine"))
                return;
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                System.IO.File.AppendAllText("log.txt", "Can't stream: " + sceneName + "\r\n");
                return;
            }
            
            wait = true;
            SingletonBehaviour<ScenePortalManager>.Instance.ChangeScene(new Vector2(0f, 0f), sceneName, () => {
                var fieldNames = new List<string>() { "amariDialogueChain", "angelDialogueChain", "demonDialogueChain", "dialogueChain", "elementalDialogueChain", "elfDialogueChain", "nagaDialogueChain" };
                var spawnManagers = FindObjectsOfType<NPCSpawnManager>();
                foreach (var spawnManager in spawnManagers)
                {
                    System.IO.File.AppendAllText("log.txt", "SpawnManager found\r\n");
                    var ff = typeof(NPCSpawnManager).GetFields(BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.NonPublic);
                    List<NPCSpawnInfo> spawnInfos = null;
                    foreach (var f in ff)
                    {
                        if (f.Name == "npcSpawnInfos")
                        {
                            System.IO.File.AppendAllText("log.txt", "npcSpawnInfos found\r\n"); 
                            spawnInfos = (List<NPCSpawnInfo>)f.GetValue(spawnManager);
                        }
                    }
                    System.IO.File.AppendAllText("log.txt", "Test " + spawnInfos + "\r\n");
                    //var spawnInfos = (List<NPCSpawnInfo>)spawnManager.GetType().GetField("npcSpawnInfos", BindingFlags.FlattenHierarchy).GetValue(spawnManager);
                    if (spawnInfos != null)
                    {
                        foreach (var n in spawnInfos)
                        {
                            System.IO.File.AppendAllText("log.txt", "SpawnInfo found\r\n");
                            ExtractString(n.npcDialogueText);
                            if (n.cutscene != null)
                            {
                                System.IO.File.AppendAllText("log.txt", "Cutscene found: " + n.cutscene.GetType() + "\r\n");
                                if (n.cutscene is HangoutCutscene hangout)
                                {
                                    var fields = typeof(HangoutCutscene).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
                                    for (var j = 0; j < fields.Length; j++)
                                    {
                                        if (fieldNames.Contains(fields[j].Name))
                                        {
                                            var v = fields[j].GetValue(hangout);
                                            if (v != null)
                                            {
                                                if (v is string vstr)
                                                    ExtractString(vstr);
                                                else if (v is List<DialogueChain> val)
                                                {
                                                    foreach (var str in val)
                                                    {
                                                        ExtractString(str.opener);
                                                        foreach (var s in str.responses)
                                                            ExtractString(s);
                                                        foreach (var s in str.finalOptions)
                                                            ExtractString(s.dialogue);
                                                        foreach (var s in str.options)
                                                            ExtractString(s.dialogue);
                                                        foreach (var s in str.finalResponses)
                                                            ExtractString(s);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                var hangouts = UnityEngine.Resources.FindObjectsOfTypeAll(typeof(HangoutCutscene));

                System.IO.File.AppendAllText("log.txt", "Hangouts: " + hangouts.Length + "\r\n");
                for (var i = 0; i < hangouts.Length; i++)
                {
                    var hangout = (HangoutCutscene)hangouts[i];
                    var fields = typeof(HangoutCutscene).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
                    for (var j = 0; j < fields.Length; j++)
                    {
                        if (fieldNames.Contains(fields[j].Name))
                        {
                            var v = fields[j].GetValue(hangout);
                            if (v != null)
                            {
                                if (v is string vstr)
                                    ExtractString(vstr);
                                else if (v is List<DialogueChain> val)
                                {
                                    foreach (var str in val)
                                    {
                                        ExtractString(str.opener);
                                        foreach (var s in str.responses)
                                            ExtractString(s);
                                        foreach (var s in str.finalOptions)
                                            ExtractString(s.dialogue);
                                        foreach (var s in str.options)
                                            ExtractString(s.dialogue);
                                        foreach (var s in str.finalResponses)
                                            ExtractString(s);
                                    }
                                }
                            }
                        }
                    }
                }
                wait = false;
                System.IO.File.WriteAllText("unknown.txt", String.Join("\r\n", Unknown));
            }, () => { });
            return;
            //System.IO.File.WriteAllText("unknown.txt", String.Join("\r\n", Unknown));
            /*Extracting = false;
            return;*/
            if (SceneManager.sceneCountInBuildSettings > CurrentScene)
            {
                var log = "Loading scene " + CurrentScene + "\r\n";

                SceneManager.UnloadScene(SceneManager.GetSceneAt(1).name);
                op = SceneManager.LoadSceneAsync(CurrentScene, LoadSceneMode.Additive);
                try
                {
                    SceneManager.SetActiveScene(SceneManager.GetSceneAt(1));
                }
                catch (Exception e) { }


                //Application.LoadLevel(CurrentScene);

                /*var skillNodes = UnityEngine.Resources.FindObjectsOfTypeAll(typeof(SkillNodeAsset));
                foreach (var s in skillNodes)
                {
                    if (s is SkillNodeAsset skillNode)
                    {
                        ExtractString(skillNode.description);
                        ExtractString(skillNode.nodeName);
                        ExtractString(skillNode.nodeTitle);
                        ExtractString(skillNode.singleDescriptionItem);
                        foreach (var n in skillNode.descriptionItems)
                            ExtractString(n);
                        foreach (var n in skillNode.thirdDescriptionItems)
                            ExtractString(n);
                    }
                }
                if (EnemyManager.Instance != null)
                {
                    foreach (var kv in EnemyManager.Instance.currentEnemyDictionary)
                    {
                        ExtractString(kv.Value.enemyName);
                    }
                }
                var spawnGroups = UnityEngine.Resources.FindObjectsOfTypeAll(typeof(EnemySpawnGroup));
                foreach (var spawnGroup in spawnGroups)
                {
                    if (spawnGroup is EnemySpawnGroup s)
                    {
                        foreach (var spawner in s.spawners)
                        {
                            if (spawner.CurrentEnemy != null)
                                ExtractString(spawner.CurrentEnemy.enemyName);
                        }
                    }
                }
                var spawners = UnityEngine.Resources.FindObjectsOfTypeAll(typeof(EnemySpawner));
                foreach (var sp in spawners)
                {
                    if (sp is EnemySpawner spawner)
                    {
                        if (spawner.CurrentEnemy != null)
                            ExtractString(spawner.CurrentEnemy.enemyName);
                    }
                }*/
                /*
                var assets = new List<object>();
                foreach (var f in Fields)
                {
                    var ass = UnityEngine.Resources.FindObjectsOfTypeAll(f.Key);
                    if (ass != null)
                    {
                        log += "Found " + ass.Length + " assets of type " + f.Key.FullName + "\r\n";
                        assets.AddRange(ass);
                    }
                }*/

                //ParseAssets(assets);
                CurrentScene++;
                //System.IO.File.WriteAllText("unknown.txt", String.Join("\r\n", Unknown));
            }
            else
            {
                System.IO.File.WriteAllText("unknown.txt", String.Join("\r\n", Unknown));
            }
        }

        private bool Extracting = false;

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
                                if (val is List<string> ss)
                                {
                                    foreach (var s in ss)
                                        ExtractString(s);
                                }
                                else if (val is string s)
                                {
                                    ExtractString(s);
                                }
                            }
                        }
                    }
                }
            }
            System.IO.File.AppendAllText("log.txt", log);
        }

        void ExtractString(string s)
        {
            var lines = s.Split(new string[] { "\r", "\n", "[]" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                var index = trimmed.LastIndexOf("(");
                if (trimmed.EndsWith(")") && index > 0)
                {
                    var p0 = trimmed.Substring(0, index).Trim();
                    var p1 = trimmed.Substring(index + 1, trimmed.Length - (index + 1) - 1).Trim();
                    if (!Ignore.ShouldIgnoreString(p0) && Translator.IsUnknown(p0) && !Unknown.Contains(p0))
                        Unknown.Add(p0);
                    if (!Ignore.ShouldIgnoreString(p1) && Translator.IsUnknown(p1) && !Unknown.Contains(p1))
                        Unknown.Add(p1);
                }
                else
                {
                    if (!Ignore.ShouldIgnoreString(trimmed) && Translator.IsUnknown(trimmed) && !Unknown.Contains(trimmed))
                        Unknown.Add(trimmed);
                }
            }
        }

        void Update()
        {
            return;
            if (Extracting)
                Extract();
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.G) && Input.GetKeyDown(KeyCode.H))
            {
                var data = SingletonBehaviour<GameSave>.Instance.CurrentSave.characterData.Relationships;
                
                foreach (var npcName in data.Keys)
                {
                    var old = SingletonBehaviour<GameSave>.Instance.CurrentSave.characterData.Relationships[npcName];
                    SingletonBehaviour<GameSave>.Instance.CurrentSave.characterData.Relationships[npcName] = old + 10f;
                    SingletonBehaviour<RelationshipHUD>.Instance.AddRelationshipLevel((int) old, 10);
                    Player.Instance?.GetRelationshipStats();
                }
            }
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.R) && Input.GetKeyDown(KeyCode.T))
            {
                wait = false;
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

                
                //System.IO.File.WriteAllText("log.txt", log);
                try
                {
                    //var assets = new List<object>();
                    //assets.AddRange(UnityEngine.Resources.LoadAll<Wish.QuestAsset>("Quest Assets"));
                    //ParseAssets(assets);

                    Unknown.Clear();
                    CurrentScene = 3;
                    Extracting = true;
                }
                catch (Exception e)
                {
                    System.IO.File.AppendAllText("log.txt", e.ToString() + "\r\n");
                }
            }
        }
    }
}
