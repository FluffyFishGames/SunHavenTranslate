using System;
using System.IO;
using System.Linq;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace SunHavenTranslate
{
    class Program
    {
        private static List<string> RecommendedVersions = new List<string>() { "0.3.1b" };

        private static Dictionary<string, List<string>> ModifyMembers = new Dictionary<string, List<string>>()
        {
            { "ItemData", new List<string>() { "name", "description", "shopDescription" } },
            { "QuestAsset", new List<string>() { "endTex", "questDescription", "bulletinBoardDescription", "questName" } },
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
            { "EnemyAI", new List<string>() { "enemyName" } }
        };

        private static Dictionary<string, List<string>> ExtractMembers = new Dictionary<string, List<string>>()
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
            { "QuestAsset", new List<string>() { "endTex", "questDescription", "bulletinBoardDescription", "questName" } },
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
        };

        static void Main(string[] args)
        {
            var directory = Environment.CurrentDirectory;
            if (args.Length > 1 && (args[0] == "extract" || args[0] == "merge"))
                directory = args[1];

            while (!Verify(directory))
            {
                System.Console.WriteLine("Game was not found at path " + directory);
                System.Console.WriteLine("Please enter the game path: ");
                directory = System.Console.ReadLine();
            }

            if (args.Length > 0)
            {
                if (args[0] == "merge")
                {
                    Merge();
                }
                else if (args[0] == "extract")
                {
                    System.Console.WriteLine("Game found. Extracting texts...");
                    Extract(Path.Combine(directory, "Sun Haven_Data"));
                    return;
                }
            }
            else
            {
                System.Console.WriteLine("Game found. Applying patch...");
                Apply(directory);
            }
        }

        static void Merge()
        {
            var newLines = File.ReadAllLines("news.txt");
            var newDialogue = File.ReadAllLines("newDialogue.txt");
            var newMethods = File.ReadAllLines("newMethods.txt");
            string currentMethod = null;
            var lines = new HashSet<string>();
            foreach (var m in newMethods)
            {
                if (m.Contains("Wish.") && m.EndsWith(")"))
                    currentMethod = m;
                else if (currentMethod != null)
                    lines.Add(currentMethod + "||" + m.Trim());
            }

            foreach (var d in newDialogue)
                lines.Add("Dialogue||" + d.Trim());
            foreach (var l in newLines)
                lines.Add(l.Trim());

            var origLines = File.ReadAllLines("table.orig").ToHashSet();
            var newFound = new HashSet<string>();
            foreach (var l in lines)
            {
                if (!origLines.Contains(l))
                    newFound.Add(l);
            }

            var newFoundText = "";
            foreach (var l in newFound)
            {
                newFoundText += l + "\r\n";
            }
            File.WriteAllText("newFound.txt", newFoundText);
        }

        static Dictionary<string, HashSet<string>> GetTypeStrings(TypeDefinition type)
        {
            var ret = new Dictionary<string, HashSet<string>>();
            foreach (var subType in type.NestedTypes)
            {
                var r = GetTypeStrings(subType);
                foreach (var n in r)
                {
                    if (!ret.ContainsKey(n.Key))
                        ret.Add(n.Key, new HashSet<string>());
                    foreach (var nn in n.Value)
                        ret[n.Key].Add(nn);
                }
            }
            foreach (var method in type.Methods)
            {
                if (method.Body != null)
                {
                    foreach (var instruction in method.Body.Instructions)
                    {
                        if (instruction.OpCode.Code == Code.Ldstr)
                        {
                            if (!ret.ContainsKey(method.FullName))
                                ret.Add(method.FullName, new HashSet<string>());
                            ret[method.FullName].Add((instruction.Operand as string).Trim());
                        }
                    }
                }
            }

            foreach (var field in type.Fields)
            {
                if (field.HasDefault && field.FieldType.FullName == "System.String")
                {
                    if (!ret.ContainsKey(field.FullName))
                        ret.Add(field.FullName, new HashSet<string>());
                    ret[field.FullName].Add(System.Text.Encoding.UTF8.GetString(field.InitialValue).Trim());
                }
            }
            return ret;
        }

        static void Extract(string directory)
        {
            var originalLines = File.ReadAllLines("table.orig");
            var original = new HashSet<string>();
            var originalMethods = new Dictionary<string, HashSet<string>>();
            var originalDialogue = new HashSet<string>();
            var newLines = new HashSet<string>();
            var newMethods = new Dictionary<string, HashSet<string>>();
            var newDialogue = new HashSet<string>();
            foreach (var line in originalLines)
            {
                if (line.StartsWith("Dialogue||"))
                    originalDialogue.Add(line.Substring("Dialogue||".Length));
                else
                {
                    var methodIndex = line.IndexOf(")||");
                    if (methodIndex > 0)
                    {
                        var method = line.Substring(0, methodIndex + 1);
                        var methodLine = line.Substring(methodIndex + 3);
                        if (!originalMethods.ContainsKey(method))
                            originalMethods.Add(method, new HashSet<string>());
                        originalMethods[method].Add(methodLine);
                    }
                    else
                    {
                        original.Add(line);
                    }
                }
            }
            var assemblyCSharp = System.IO.Path.Combine(directory, "Managed", "Assembly-CSharp.dll");
            var assembly = AssemblyDefinition.ReadAssembly(assemblyCSharp);
            var module = assembly.MainModule;
            var enums = Enums.FindEnums(Path.Combine(directory, "Managed"), module);
            foreach (var en in enums.Enums)
            {
                foreach (var kv in en.Value.Fields)
                {
                    var val = kv.Value.Name.Trim();
                    if (val.Trim() != "" && !newLines.Contains(val))
                        newLines.Add(val);
                }
            }
            foreach (var type in module.Types)
            {
                if (type.Namespace == "Wish")
                {
                    var l = GetTypeStrings(type);
                    foreach (var n in l)
                    {
                        if (!newMethods.ContainsKey(n.Key))
                            newMethods.Add(n.Key, new HashSet<string>());
                        foreach (var nn in n.Value)
                            newMethods[n.Key].Add(nn);
                    }
                }
            }
            module.Dispose();
            assembly.Dispose();

            var manager = new AssetsManager();
            var files = System.IO.Directory.GetFiles(directory);
            var classNames = new HashSet<string>();
            foreach (var file in files)
            {
                var fileName = System.IO.Path.GetFileName(file);
                if (fileName.EndsWith(".assets") || fileName.StartsWith("level"))
                {
                    System.Console.WriteLine("Loading file: " + file);
                    var assets = manager.LoadAssetsFile(file, true);
                    manager.LoadClassPackage("classdata.tpk");
                    manager.LoadClassDatabaseFromPackage(assets.file.typeTree.unityVersion);

                    var gameObjectAssets = assets.table.GetAssetsOfType(0x01);
                    for (var j = 0; j < gameObjectAssets.Count; j++)
                    {
                        var ext = manager.GetExtAsset(assets, 0, gameObjectAssets[j].index);
                        var bff = ext.instance.GetBaseField();
                        var components = bff.Get("m_Component").Get("Array");
                        int componentSize = components.GetValue().AsArray().size;
                        for (int n = 0; n < componentSize; n++)
                        {
                            var componentPtr = components[n].GetLastChild();
                            var comp = manager.GetExtAsset(assets, componentPtr);
                            if (comp.instance != null)
                            {
                                var firstBaseField = comp.instance.GetBaseField();
                                var script = firstBaseField.Get("m_Script");
                                if (script.templateField != null)
                                {
                                    var sc = manager.GetExtAsset(assets, script);
                                    var scriptAti = sc.instance;
                                    if (scriptAti == null)
                                        System.Console.WriteLine("Unknown script name");
                                    else
                                    {
                                        var className = scriptAti.GetBaseField().Get("m_Name").GetValue().AsString();

                                        if (!classNames.Contains(className))
                                        {
                                            classNames.Add(className);
                                            System.Console.WriteLine(className);
                                        }
                                        if (ExtractMembers.ContainsKey(className))
                                        {
                                            try
                                            {
                                                var s = comp;
                                                var bf = s.instance.GetBaseField();
                                                string managedPath = System.IO.Path.Combine(directory, "Managed");
                                                if (Directory.Exists(managedPath))
                                                {
                                                    bf = manager.GetMonoBaseFieldCached(assets, s.info, managedPath);
                                                }
                                                foreach (var member in ExtractMembers[className])
                                                {
                                                    var c = bf.Get(member);

                                                    var cv = c.GetValue();
                                                    if (cv != null)
                                                    {
                                                        if (cv.GetValueType() == EnumValueTypes.String || cv.GetValueType() == EnumValueTypes.ValueType_String)
                                                        {
                                                            var val = cv.AsString().Trim();
                                                            if (val != "" && !newLines.Contains(val))
                                                                newLines.Add(val);
                                                        }
                                                        else
                                                        {

                                                        }
                                                    }
                                                }
                                            }
                                            catch (Exception e)
                                            {
                                                System.Console.WriteLine(e);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    var monoBehaviourAssets = assets.table.GetAssetsOfType(0x72);
                    for (var j = 0; j < monoBehaviourAssets.Count; j++)
                    {
                        var template = manager.GetTemplateBaseField(assets.file, monoBehaviourAssets[j]);
                        var typeInstance = manager.GetTypeInstance(assets.file, monoBehaviourAssets[j]);
                        var firstBaseField = typeInstance.GetBaseField();
                        var firstExt = manager.GetExtAsset(assets, firstBaseField.Get("m_GameObject"));
                        var scriptAti = manager.GetExtAsset(assets, firstBaseField.Get("m_Script")).instance;
                        if (scriptAti == null)
                            System.Console.WriteLine("Unknown script name");
                        else
                        {
                            var className = scriptAti.GetBaseField().Get("m_Name").GetValue().AsString();

                            if (ExtractMembers.ContainsKey(className))
                            {
                                try
                                {
                                    var s = manager.GetExtAsset(assets, 0, monoBehaviourAssets[j].index);
                                    var bf = s.instance.GetBaseField();
                                    string managedPath = System.IO.Path.Combine(directory, "Managed");
                                    if (Directory.Exists(managedPath))
                                    {
                                        bf = manager.GetMonoBaseFieldCached(assets, monoBehaviourAssets[j], managedPath);
                                    }
                                    foreach (var member in ExtractMembers[className])
                                    {
                                        var c = bf.Get(member);
                                        var cv = c.GetValue();
                                        if (cv != null)
                                        {
                                            if (cv.GetValueType() == EnumValueTypes.String || cv.GetValueType() == EnumValueTypes.ValueType_String)
                                            {
                                                var val = cv.AsString().Trim();
                                                if (val != "" && !newLines.Contains(val))
                                                    newLines.Add(val);
                                            }
                                            else
                                            {

                                            }
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    System.Console.WriteLine(e);
                                }
                            }
                        }
                    }

                    var textAssets = assets.table.GetAssetsOfType(0x31);
                    for (var j = 0; j < textAssets.Count; j++)
                    {
                        if (textAssets[j].ReadName(assets.table.file, out var name))
                        {
                            var st = new MemoryStream();
                            var reader = new AssetsFileReader(st);
                            var data = new byte[textAssets[j].curFileSize];
                            assets.file.reader.Position = textAssets[j].absoluteFilePos;
                            assets.file.reader.Read(data, 0, (int) data.Length);

                            var lengthName = BitConverter.ToInt32(data, 0);
                            var startDataLength = lengthName + 4;
                            if ((startDataLength % 4) > 0)
                                startDataLength += 4 - (startDataLength % 4);
                            var lengthText = BitConverter.ToInt32(data, startDataLength);
                            var title = System.Text.Encoding.UTF8.GetString(data, 4, lengthName);
                            var text = System.Text.Encoding.UTF8.GetString(data, startDataLength + 4, lengthText);

                            var lines = text.Split(new string[] { "\r\n" }, StringSplitOptions.None);
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
                                    if (dialogLine.EndsWith("(End)"))
                                        dialogLine = dialogLine.Substring(0, dialogLine.Length - 5);

                                    dialogLine = dialogLine.Trim();
                                    if (!newDialogue.Contains(dialogLine))
                                        newDialogue.Add(dialogLine);
                                }
                            }
                            st.Close();
                        }
                    }
                }
            }

            var newDialogueText = "";
            foreach (var d in newDialogue)
            {
                if (!originalDialogue.Contains(d))
                    newDialogueText += d + "\r\n";
            }

            var newMethodsText = "";
            foreach (var n in newMethods)
            {
                var found = new HashSet<string>();
                if (originalMethods.ContainsKey(n.Key))
                {
                    foreach (var nn in n.Value)
                    {
                        if (!originalMethods[n.Key].Contains(nn))
                            found.Add(nn);
                    }
                }
                else
                    foreach (var nn in n.Value)
                        found.Add(nn);
                if (found.Count > 0)
                {
                    newMethodsText += n.Key + "\r\n";
                    foreach (var f in found)
                        newMethodsText += f + "\r\n";
                }
            }

            var newText = "";
            foreach (var n in newLines)
            {
                if (!original.Contains(n))
                    newText += n + "\r\n";
            }

            File.WriteAllText("news.txt", newText);
            File.WriteAllText("newDialogue.txt", newDialogueText);
            File.WriteAllText("newMethods.txt", newMethodsText);
        }

        static bool Verify(string directory)
        {
            return File.Exists(Path.Combine(directory, "Sun Haven_Data", "Managed", "Assembly-CSharp.dll")) &&
                File.Exists(Path.Combine(directory, "Sun Haven_Data", "Managed", "Unity.TextMeshPro.dll")) &&
                File.Exists(Path.Combine(directory, "Sun Haven_Data", "Managed", "UnityEngine.CoreModule.dll")) &&
                File.Exists(Path.Combine(directory, "Sun Haven.exe"));
        }

        static void Apply(string directory)
        {
            var version = GetGameVersion(directory).Trim();
            if (version == null)
            {
                System.Console.WriteLine("Oops! Couldn't get the game version. Proceed with caution!");
            }
            else if (!RecommendedVersions.Contains(version))
            {
                System.Console.WriteLine("Oops! Your game version is not recommended for this mod. Proceed with caution!");
                System.Console.WriteLine("Your game version: " + version);
                System.Console.WriteLine("Recommended game versions: " + String.Join(", ", RecommendedVersions));
            }
            else
            {
                System.Console.WriteLine("Your game version: " + version);
            }

            var translatorBackupPath = Path.Combine(directory, "TranslatorBackup");
            var versionFile = Path.Combine(translatorBackupPath, "version");
            var gameWasUpdated = false;
            if (File.Exists(versionFile))
            {
                var lastPatchVersion = File.ReadAllText(Path.Combine(translatorBackupPath, "version")).Trim();
                if (lastPatchVersion != version)
                {
                    gameWasUpdated = true;
                    System.Console.WriteLine("It seems like your game was updated. If this is not the case: Stop now!");
                }
            }

            var currentDirectory = Environment.CurrentDirectory;
            var checkFiles = new string[] { "TranslatorPlugin.dll", "table.orig", "table.trans", "classdata.tpk" };
            foreach (var checkFile in checkFiles)
            {
                if (!File.Exists(Path.Combine(currentDirectory, checkFile)))
                {
                    System.Console.WriteLine(checkFile + " is missing! Can't proceed :(");
                    System.Console.Read();
                    return;
                }
            }

            System.Console.WriteLine("Sanity checks completed! Congratulations!");
            System.Console.WriteLine("");
            System.Console.WriteLine("Welcome to my Sun Haven translator mod.");
            System.Console.WriteLine("");
            System.Console.WriteLine("This software is freeware. This means if somebody made you pay for it you have been ripped off.");
            System.Console.WriteLine("The official download site for this tool is potatoepet.de");
            System.Console.WriteLine("You can also find this software as open source on github.com/FluffyFishGames");
            System.Console.WriteLine("");
            System.Console.WriteLine("This tool will modify your game files. Therefore you need to know a couple of things.");
            System.Console.WriteLine("Not every bug you might encounter is necessarily in the unmodded game.");
            System.Console.WriteLine("Before submitting bug reports to PixelSprout please ensure your error also occurs in an unmodded game first.");
            System.Console.WriteLine("In case of an update this mod WILL break. DO NOT just start it again to translate as it will corrupt your files.");
            System.Console.WriteLine("Best way to handle updates is to wait for this tool to get updated.");
            System.Console.WriteLine("Please confirm that you have understood this small disclaimer by typing \"y\" and confirm your input by hitting return.");
            if (System.Console.ReadLine().ToLowerInvariant().Trim() == "y")
            {
                System.Console.WriteLine("Here we go!");
                ApplyPatches(directory, gameWasUpdated, version);
            }
        }

        static void ApplyPatches(string directory, bool gameWasUpdated, string version)
        {
            System.Console.WriteLine("Copying files if needed...");
            CopyFiles(directory, gameWasUpdated);

            var translatorBackupPath = Path.Combine(directory, "TranslatorBackup");
            var managedPath = Path.Combine(directory, "Sun Haven_Data", "Managed");
            
            System.Console.WriteLine("Reading assemblies and fetching types and enums...");

            var resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(Path.Combine(managedPath));

            System.Console.WriteLine("Fetching methods from TranslatorPlugin.dll");
            MethodDefinition translateStringMethod = null;
            MethodDefinition translateObjectMethod = null;
            MethodDefinition translateDialogMethod = null;
            MethodDefinition changeTextMeshMethod = null;
            MethodDefinition getTextMethod = null;
            MethodDefinition textAssetConstructor = null;

            var translatorAssembly = AssemblyDefinition.ReadAssembly(System.IO.Path.Combine(managedPath, "TranslatorPlugin.dll"));
            var translatorType = translatorAssembly.MainModule.GetType("TranslatorPlugin.Translator");

            foreach (var method in translatorType.Methods)
            {
                if (method.Name == "TranslateString")
                    translateStringMethod = method;
                if (method.Name == "TranslateObject")
                    translateObjectMethod = method;
                if (method.Name == "TranslateDialog")
                    translateDialogMethod = method;
                if (method.Name == "ChangeTextMesh")
                    changeTextMeshMethod = method;
            }

            System.Console.WriteLine("Fetching methods from UnityEngine.CoreModule.dll");

            var unityEngineCoreModule = System.IO.Path.Combine(translatorBackupPath, "UnityEngine.CoreModule.dll");
            var coreModuleAssembly = AssemblyDefinition.ReadAssembly(unityEngineCoreModule, new ReaderParameters() { AssemblyResolver = resolver });
            MethodDefinition objectSetName = null;
            MethodDefinition objectGetName = null;
            var objectClass = coreModuleAssembly.MainModule.GetType("UnityEngine.Object");
            foreach (var p in objectClass.Properties)
            {
                if (p.Name == "name")
                {
                    objectSetName = p.SetMethod;
                    objectGetName = p.GetMethod;
                }
            }
            var textAssetClass = coreModuleAssembly.MainModule.GetType("UnityEngine.TextAsset");
            
            foreach (var p in textAssetClass.Properties)
            {
                if (p.Name == "text")
                    getTextMethod = p.GetMethod;
            }
            foreach (var m in textAssetClass.Methods)
            {
                if (m.Name == ".ctor" && m.Parameters.Count == 1)
                    textAssetConstructor = m;
            }

            if (translateStringMethod == null || translateObjectMethod == null || translateDialogMethod == null || changeTextMeshMethod == null || getTextMethod == null || textAssetConstructor == null)
            {
                System.Console.WriteLine("Couldn't find necessary methods. Can't proceed :(");
                System.Console.Read();
                return;
            }

            System.Console.WriteLine("Patching Unity.TextMeshPro.dll");
            var textmeshPro = System.IO.Path.Combine(translatorBackupPath, "Unity.TextMeshPro.dll");
            var textMeshProAssembly = AssemblyDefinition.ReadAssembly(textmeshPro, new ReaderParameters() { AssemblyResolver = resolver });
            var textMeshProModule = textMeshProAssembly.MainModule;
            var tmpText = textMeshProModule.GetType("TMPro.TMP_Text");
            PropertyDefinition textProperty = null;
            foreach (var property in tmpText.Properties)
            {
                if (property.Name == "text")
                    textProperty = property;
            }
            var getStringRef = textMeshProModule.ImportReference(translateStringMethod);
            var changeTextRef = textMeshProModule.ImportReference(changeTextMeshMethod);

            if (textProperty != null && translateStringMethod != null)
            {
                var mRef = textMeshProModule.ImportReference(translateStringMethod);
                var setMethod = textProperty.SetMethod;
                var body = setMethod.Body;
                var processor = body.GetILProcessor();
                var firstInstruction = body.Instructions[0];
                if (firstInstruction.OpCode.Code == Code.Ldarg_0)
                {
                    System.Console.WriteLine("Patching TMPro.TMP_Text.text setter...");
                    processor.InsertBefore(firstInstruction, processor.Create(OpCodes.Ldarg_1));
                    processor.InsertBefore(firstInstruction, processor.Create(OpCodes.Ldstr, setMethod.FullName));
                    processor.InsertBefore(firstInstruction, processor.Create(OpCodes.Call, mRef));
                    processor.InsertBefore(firstInstruction, processor.Create(OpCodes.Starg_S, setMethod.Parameters[0]));
                }
            }

            var textMeshProGUI = textMeshProModule.GetType("TMPro.TextMeshProUGUI");
            MethodDefinition startMethod = null;
            foreach (var method in textMeshProGUI.Methods)
            {
                if (method.Name == "Start")
                    startMethod = method;
            }

            if (startMethod == null)
            {
                System.Console.WriteLine("Adding start method to TMPro.TextMeshProUGUI...");
                startMethod = new MethodDefinition("Start", MethodAttributes.Family | MethodAttributes.HideBySig | MethodAttributes.Virtual, textMeshProModule.TypeSystem.Void);
                textMeshProGUI.Methods.Add(startMethod);
            }

            if (startMethod != null)
            {
                System.Console.WriteLine("Filling method body of TMPro.TextMeshProUGUI.Start...");
                var setMethod = textProperty.SetMethod;
                var getMethod = textProperty.GetMethod;
                var body = startMethod.Body;
                body.Instructions.Clear();
                var processor = body.GetILProcessor();

                processor.Emit(OpCodes.Nop);
                processor.Emit(OpCodes.Ldarg_0);
                processor.Emit(OpCodes.Ldarg_0);
                processor.Emit(OpCodes.Call, textMeshProModule.ImportReference(getMethod));
                processor.Emit(OpCodes.Ldstr, startMethod.FullName);
                processor.Emit(OpCodes.Call, getStringRef);
                processor.Emit(OpCodes.Call, textMeshProModule.ImportReference(setMethod));
                processor.Emit(OpCodes.Ldarg_0);
                processor.Emit(OpCodes.Call, changeTextRef);
                processor.Emit(OpCodes.Nop);
                processor.Emit(OpCodes.Ret);
            }

            System.Console.WriteLine("Saving Unity.TextMeshPro.dll");
            System.Console.WriteLine(textMeshProModule.FileName);
            System.Console.WriteLine(System.IO.Path.Combine(managedPath, "Unity.TextMeshPro.dll"));
            textMeshProAssembly.Write(System.IO.Path.Combine(managedPath, "Unity.TextMeshPro.dll"));
            System.Console.WriteLine("Assembly saved successfully!");

            System.Console.WriteLine("Loading method patching data...");
            var original = System.IO.File.ReadAllLines(System.IO.Path.Combine(directory, "table.orig"));
            
            Dictionary<string, HashSet<string>> replaces = new Dictionary<string, HashSet<string>>();
            string currentPointer = null;
            for (var i = 0; i < original.Length; i++)
            {
                var pipes = original[i].IndexOf("||");
                if (pipes > 0)
                {
                    var name = original[i].Substring(0, pipes);
                    if (name.EndsWith(")"))
                    {
                        if (!replaces.ContainsKey(name))
                            replaces.Add(name, new HashSet<string>());
                        replaces[name].Add(original[i].Substring(pipes + 2));
                    }
                }
            }
            System.Console.WriteLine("Found " + replaces.Count + " methods to patch.");

            var assemblyCSharp = System.IO.Path.Combine(translatorBackupPath, "Assembly-CSharp.dll");
            var assemblyCSharpAssembly = AssemblyDefinition.ReadAssembly(assemblyCSharp, new ReaderParameters { AssemblyResolver = resolver });
            var assemblyCSharpModule = assemblyCSharpAssembly.MainModule;

            System.Console.WriteLine("Looking for enum types...");
            var enums = Enums.FindEnums(managedPath, assemblyCSharpModule);
            System.Console.WriteLine("Patching methods which use enums to use Translator...");
            Enums.ChangeEnumToString(enums, assemblyCSharpModule, assemblyCSharpModule.ImportReference(translateObjectMethod), assemblyCSharpModule.ImportReference(translateStringMethod));

            getStringRef = assemblyCSharpModule.ImportReference(translateStringMethod);
            var translateDialogRef = assemblyCSharpModule.ImportReference(translateDialogMethod);
            var textAssetConstructorRef = assemblyCSharpModule.ImportReference(textAssetConstructor);
            var textAssetRef = assemblyCSharpModule.ImportReference(textAssetClass);
            var objectSetNameRef = assemblyCSharpModule.ImportReference(objectSetName);
            var objectGetNameRef = assemblyCSharpModule.ImportReference(objectGetName);
            var getTextRef = assemblyCSharpModule.ImportReference(getTextMethod);
            var dialogueTreeClass = assemblyCSharpModule.GetType("Wish.DialogueTree");

            System.Console.WriteLine("Replacing inline strings...");
            foreach (var type in assemblyCSharpModule.Types)
            {
                if (type.Namespace == "Wish")
                    ReplaceStrings(type, replaces, getStringRef);
            }

            System.Console.WriteLine("Patching Wish.DialougeTree:ParseTextAsset...");
            foreach (var m in dialogueTreeClass.Methods)
            {
                if (m.Name == "ParseTextAsset")
                {
                    var body = m.Body;
                    if (body.Instructions[0].OpCode.Code == Code.Newobj)
                    {
                        var textAssetVar = new VariableDefinition(textAssetRef);
                        body.Variables.Add(textAssetVar);

                        var firstInstruction = body.Instructions[0];
                        var ilProcessor = body.GetILProcessor();
                        ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldarg_1));
                        ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Callvirt, getTextRef));
                        ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Call, translateDialogRef));
                        ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Newobj, textAssetConstructorRef));
                        ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Stloc, textAssetVar));
                        ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldloc, textAssetVar));
                        ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldarg_1));
                        ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Call, objectGetNameRef));
                        ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Call, objectSetNameRef));
                        ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldloc, textAssetVar));
                        ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Starg, 1));
                    }
                }
            }

            foreach (var kv in ModifyMembers)
            {
                System.Console.WriteLine("Patching Wish." + kv.Key);

                var @class = assemblyCSharpModule.GetType("Wish." + kv.Key);
                MethodDefinition awakeMethod = null;
                System.Console.WriteLine("Searching for awake method...");
                foreach (var method in @class.Methods)
                {
                    if (method.Name == "Awake")
                        awakeMethod = method;
                }

                System.Console.WriteLine("Searching for fields...");
                List<FieldDefinition> fields = new List<FieldDefinition>();
                foreach (var field in @class.Fields)
                {
                    if (kv.Value.Contains(field.Name))
                        fields.Add(field);
                }

                System.Console.WriteLine("Checking for base type...");
                TypeReference @base = @class.BaseType;
                MethodDefinition baseAwake = null;

                while (@base != null && baseAwake == null)
                {
                    var typeDef = assemblyCSharpModule.GetType(@base.FullName);
                    if (typeDef != null)
                    {
                        foreach (var method in typeDef.Methods)
                        {
                            if (method.Name == "Awake")
                                baseAwake = method;
                        }
                        if (baseAwake != null)
                            break;
                        @base = typeDef.BaseType;
                    }
                    else break;
                }

                if (awakeMethod == null)
                {
                    System.Console.WriteLine("Creating new Awake method...");
                    awakeMethod = new MethodDefinition("Awake", MethodAttributes.Family | MethodAttributes.HideBySig | MethodAttributes.Virtual, assemblyCSharpModule.TypeSystem.Void);
                    @class.Methods.Add(awakeMethod);
                }

                var awakeBody = awakeMethod.Body;
                var ilProcessor = awakeBody.GetILProcessor();

                System.Console.WriteLine("Patching method body...");
                if (awakeBody.Instructions.Count == 1 && awakeBody.Instructions[0].OpCode.Code == Code.Ret)
                    awakeBody.Instructions.Clear();

                if (awakeBody.Instructions.Count < 5 || awakeBody.Instructions[2].OpCode.Code != Code.Ldfld || awakeBody.Instructions[3].OpCode.Code != Code.Call || awakeBody.Instructions[4].OpCode.Code != Code.Stfld)
                {
                    if (awakeBody.Instructions.Count < 2 || awakeBody.Instructions[0].OpCode.Code != Code.Ldarg_0 || awakeBody.Instructions[1].OpCode.Code != Code.Call || ((MethodReference) awakeBody.Instructions[1].Operand).FullName != baseAwake.FullName)
                    {
                        if (baseAwake != null)
                        {
                            if (awakeBody.Instructions.Count == 0)
                            {
                                ilProcessor.Emit(OpCodes.Ldarg_0);
                                ilProcessor.Emit(OpCodes.Call, assemblyCSharpModule.ImportReference(baseAwake));
                            }
                            else
                            {
                                var firstInstruction = awakeBody.Instructions[0];
                                ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldarg_0));
                                ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Call, assemblyCSharpModule.ImportReference(baseAwake)));
                            }
                        }
                    }
                    if (baseAwake != null)
                    {
                        if (awakeBody.Instructions.Count > 2)
                        {
                            var baseInstruction = awakeBody.Instructions[2];
                            foreach (var field in fields)
                            {
                                ilProcessor.InsertBefore(baseInstruction, ilProcessor.Create(OpCodes.Ldarg_0));
                                ilProcessor.InsertBefore(baseInstruction, ilProcessor.Create(OpCodes.Ldarg_0));
                                ilProcessor.InsertBefore(baseInstruction, ilProcessor.Create(OpCodes.Ldfld, field));
                                ilProcessor.InsertBefore(baseInstruction, ilProcessor.Create(OpCodes.Ldstr, awakeMethod.FullName));
                                ilProcessor.InsertBefore(baseInstruction, ilProcessor.Create(OpCodes.Call, getStringRef));
                                ilProcessor.InsertBefore(baseInstruction, ilProcessor.Create(OpCodes.Stfld, field));
                            }
                        }
                        else
                        {
                            foreach (var field in fields)
                            {
                                ilProcessor.Emit(OpCodes.Ldarg_0);
                                ilProcessor.Emit(OpCodes.Ldarg_0);
                                ilProcessor.Emit(OpCodes.Ldfld, field);
                                ilProcessor.Emit(OpCodes.Ldstr, awakeMethod.FullName); 
                                ilProcessor.Emit(OpCodes.Call, getStringRef);
                                ilProcessor.Emit(OpCodes.Stfld, field);
                            }
                        }
                    }
                    else
                    {
                        if (awakeBody.Instructions.Count > 0 && awakeBody.Instructions[awakeBody.Instructions.Count - 1].OpCode.Code == Code.Ret)
                            awakeBody.Instructions.RemoveAt(awakeBody.Instructions.Count - 1);
                        foreach (var field in fields)
                        {
                            ilProcessor.Emit(OpCodes.Ldarg_0);
                            ilProcessor.Emit(OpCodes.Ldarg_0);
                            ilProcessor.Emit(OpCodes.Ldfld, field);
                            ilProcessor.Emit(OpCodes.Ldstr, awakeMethod.FullName);
                            ilProcessor.Emit(OpCodes.Call, getStringRef);
                            ilProcessor.Emit(OpCodes.Stfld, field);
                        }
                    }
                }
                if (awakeBody.Instructions[awakeBody.Instructions.Count - 1].OpCode.Code != Code.Ret)
                    ilProcessor.Emit(OpCodes.Ret);
            }

            System.Console.WriteLine("Saving Assembly-CSharp.dll");
            assemblyCSharpAssembly.Write(System.IO.Path.Combine(managedPath, "Assembly-CSharp.dll"));

            System.Console.WriteLine("Saving version number...");
            System.IO.File.WriteAllText(Path.Combine(translatorBackupPath, "version"), version);
            System.Console.WriteLine("Patching complete! :)");
            System.Console.ReadLine();
        }

        static void CopyFiles(string directory, bool gameWasUpdated)
        {
            var translatorBackupPath = Path.Combine(directory, "TranslatorBackup");
            if (!Directory.Exists(translatorBackupPath))
            {
                System.Console.WriteLine("Translator Backup directory doesn't exist yet. Creating now.");
                Directory.CreateDirectory(translatorBackupPath);
            }

            var currentDirectory = Environment.CurrentDirectory;
            if (Path.GetFullPath(currentDirectory) != Path.GetFullPath(directory))
            {
                var copyFiles = new string[] { "table.orig", "table.trans" };
                foreach (var copyFile in copyFiles)
                {
                    System.Console.WriteLine("Copying " + copyFile);
                    File.Copy(Path.Combine(currentDirectory, copyFile), Path.Combine(directory, copyFile), true);
                }
            }

            var managedPath = Path.Combine(directory, "Sun Haven_Data", "Managed");

            System.Console.WriteLine("Copying TranslatorPlugin.dll to Managed directory...");
            File.Copy(Path.Combine(currentDirectory, "TranslatorPlugin.dll"), Path.Combine(managedPath, "TranslatorPlugin.dll"), true);

            var checkFiles = new string[] { "Assembly-CSharp.dll", "Unity.TextMeshPro.dll", "UnityEngine.CoreModule.dll" };
            foreach (var checkFile in checkFiles)
            {
                if (gameWasUpdated)
                {
                    System.Console.WriteLine(checkFile + " need to be copied. Copying now.");
                    File.Copy(Path.Combine(managedPath, checkFile), Path.Combine(translatorBackupPath, checkFile), true);
                }
                else if (!File.Exists(Path.Combine(translatorBackupPath, checkFile)))
                {
                    System.Console.WriteLine(checkFile + " is not copied yet. Copying now.");
                    File.Copy(Path.Combine(managedPath, checkFile), Path.Combine(translatorBackupPath, checkFile), true);
                }
            }
        }

        static string GetGameVersion(string directory)
        {
            string ret = null;
            var manager = new AssetsManager();
            var assets = manager.LoadAssetsFile(Path.Combine(directory, "Sun Haven_Data", "level0"), true);
            manager.LoadClassPackage("classdata.tpk");
            manager.LoadClassDatabaseFromPackage(assets.file.typeTree.unityVersion);

            var gameObjectAssets = assets.table.GetAssetsOfType(0x01);
            for (var i = 0; i < gameObjectAssets.Count; i++)
            {
                assets.file.reader.Position = gameObjectAssets[i].absoluteFilePos;
                int size = assets.file.reader.ReadInt32();
                assets.file.reader.Position += size * (assets.file.header.format > 0x10 ? 0x0c : 0x10) + 0x04;
                var assetName = assets.file.reader.ReadCountStringInt32();
                if (assetName == "VersionTMP")
                {
                    var ext = manager.GetExtAsset(assets, 0, gameObjectAssets[i].index);
                    var baseField = ext.instance.GetBaseField();
                    var components = baseField.Get("m_Component").Get("Array");
                    int componentSize = components.GetValue().AsArray().size;
                    for (int n = 0; n < componentSize; n++)
                    {
                        var componentPtr = components[n].children[components[n].childrenCount - 1];
                        var comp = manager.GetExtAsset(assets, componentPtr);
                        if (comp.instance != null)
                        {
                            var firstBaseField = comp.instance.GetBaseField();
                            var script = firstBaseField.Get("m_Script");
                            if (script.templateField != null)
                            {
                                var sc = manager.GetExtAsset(assets, script);
                                var scriptAti = sc.instance;
                                if (scriptAti != null)
                                {
                                    var className = scriptAti.GetBaseField().Get("m_Name").GetValue().AsString();
                                    if (className == "TextMeshProUGUI")
                                    {
                                        var scriptBaseField = comp.instance.GetBaseField();
                                        string managedPath = System.IO.Path.Combine(directory, "Sun Haven_Data", "Managed");
                                        if (Directory.Exists(managedPath))
                                            scriptBaseField = manager.GetMonoBaseFieldCached(assets, comp.info, managedPath);
                                        var field = scriptBaseField.Get("m_text");
                                        if (field != null)
                                        {
                                            var value = field.GetValue();
                                            if (value != null && value.GetValueType() == EnumValueTypes.String)
                                            {
                                                ret = value.AsString();
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            ret = ret.Replace("Version ", "");
            manager.UnloadAll(true);
            foreach (var kv in MonoDeserializer.loadedAssemblies)
            {
                System.Console.WriteLine(kv.Value.MainModule.FileName);
                kv.Value.Dispose();
            }
            return ret;
        }

        private static void ReplaceStrings(TypeDefinition type, Dictionary<string, HashSet<string>> replaces, MethodReference getString)
        {
            foreach (var t in type.NestedTypes)
            {
                ReplaceStrings(t, replaces, getString);
            }

            var str = "";
            foreach (var method in type.Methods)
            {
                if (replaces.ContainsKey(method.FullName))
                {
                    var repl = replaces[method.FullName];
                    if (method.Body != null)
                    {
                        var body = method.Body;

                        body.SimplifyMacros();
                        
                        var processor = body.GetILProcessor();
                        for (var i = 0; i < body.Instructions.Count; i++)
                        {
                            var instruction = body.Instructions[i];
                            if (instruction.OpCode.Code == Code.Ldstr)
                            {
                                bool needsTranslation = true;
                                var st = (instruction.Operand as string);
                                var currentStr = "";
                                for (var j = 0; j < st.Length; j++)
                                {
                                    if (st[j] == '\r' || st[j] == '\n')
                                    {
                                        if (currentStr != "")
                                        {
                                            var trimmed = currentStr.Trim();
                                            if (trimmed != "")
                                            {
                                                if (!replaces[method.FullName].Contains(trimmed))
                                                    needsTranslation = false;
                                            }
                                        }
                                    }
                                    else currentStr += st[j];
                                }
                                var lastTrimmed = currentStr.Trim();
                                if (lastTrimmed != "")
                                {
                                    if (!replaces[method.FullName].Contains(lastTrimmed))
                                        needsTranslation = false;
                                }

                                
                                if (needsTranslation)
                                {
                                    processor.InsertAfter(instruction, processor.Create(OpCodes.Call, getString));
                                    processor.InsertAfter(instruction, processor.Create(OpCodes.Ldstr, method.FullName));
                                }
                            }
                        }

                        body.OptimizeMacros();
                        body.Optimize();
                    }
                }
            }

            /*
            foreach (var field in type.Fields)
            {
                if (replaces.ContainsKey(field.FullName))
                {
                    var repl = replaces[field.FullName];
                    var val = System.Text.Encoding.UTF8.GetString(field.InitialValue);
                    var lines = val.Split(new string[] { "\r\n" }, StringSplitOptions.None);
                    bool first = true;
                    var result = "";
                    foreach (var line in lines)
                    {
                        if (first) result += "\r\n";

                        if (repl.Contains(line))
                            result += repl[line];
                        else
                            result += line;
                        first = false;
                    }
                    if (val != result)
                    {
                        System.Console.WriteLine("Replacing initial value of field " + field + ".");
                        field.InitialValue = System.Text.Encoding.UTF8.GetBytes(result);
                    }
                }
            }*/
        }
    }
}
