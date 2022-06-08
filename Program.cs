using System;
using System.IO;
using System.Linq;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
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
        private static List<string> RecommendedVersions = new List<string>() { "0.5.1b" };

        private static Dictionary<string, List<string>> ModifyMembers = new Dictionary<string, List<string>>()
        {
            { "ItemData", new List<string>() { "name", "description", "shopDescription", "helpDescription", "useDescription" } },
            { "QuestAsset", new List<string>() { "endTex", "questDescription", "bulletinBoardDescription", "questName", "overrideTurnInText" } },
            { "SceneSettings", new List<string>() { "formalSceneName" } },
            { "MailAsset", new List<string>() { "message" } },
            { "ClothingLayerData", new List<string>() { "menuName" } },
            { "BookShelf", new List<string>(){ "bookName", "text" } },
            { "Popup", new List<string>(){ "text", "description" } },
            { "SkillNode", new List<string>(){ "nodeTitle", "nodeName", "description", "singleDescriptionItem", "SkillTitle", "SkillDescription", "SkillNodeAmountText" } },
            { "NPCAI", new List<string>(){ "title", "questLocation" } },
            { "Inspectable", new List<string>() { "inspectionText", "dialogueName", "interactionText" } },
            { "EliosFountain", new List<string>() { "interactionText" } },
            { "RepairSign", new List<string>() { "text" } },
            { "ScenePortalSpot", new List<string>() { "_cantEnterNotificiation", "enterOverrideText" } },
            { "EnemyAI", new List<string>() { "enemyName" } },
            { "QuestKillRequirement", new List<string>() { "enemy" } },
            { "Chest", new List<string>() { "interactText" } },
            { "HelpTooltipZone", new List<string>() { "title", "description" } }
        };

        private static Dictionary<string, List<string>> ExtractMembers = new Dictionary<string, List<string>>()
        {
            { "AnimalData", new List<string>() { "name", "description", "shopDescription", "helpDescription", "useDescription" } },
            { "ArmorData", new List<string>() { "name", "description", "shopDescription", "helpDescription", "useDescription" } },
            { "DecorationData", new List<string>() { "name", "description", "shopDescription", "helpDescription", "useDescription" } },
            { "FoodData", new List<string>() { "name", "description", "shopDescription", "helpDescription", "useDescription" } },
            { "PetData", new List<string>() { "name", "description", "shopDescription", "helpDescription", "useDescription" } },
            { "QuestItemData", new List<string>() { "name", "description", "shopDescription", "helpDescription", "useDescription" } },
            { "RecordData", new List<string>() { "name", "description", "shopDescription", "helpDescription", "useDescription" } },
            { "WallpaperData", new List<string>() { "name", "description", "shopDescription", "helpDescription", "useDescription" } },
            { "WateringCanData", new List<string>() { "name", "description", "shopDescription", "helpDescription", "useDescription" } },
            { "CropData", new List<string>() { "name", "description", "shopDescription", "helpDescription", "useDescription" } },
            { "FishData", new List<string>() { "name", "description", "shopDescription", "helpDescription", "useDescription" } },
            { "SeedData", new List<string>() { "name", "description", "shopDescription", "helpDescription", "useDescription" } },
            { "ItemData", new List<string>() { "name", "description", "shopDescription", "helpDescription", "useDescription" } },
            { "ToolData", new List<string>() { "name", "description", "shopDescription", "helpDescription", "useDescription" } },
            { "QuestAsset", new List<string>() { "endTex", "questDescription", "bulletinBoardDescription", "questName", "overrideTurnInText" } },
            { "TextMeshProUGUI", new List<string>() { "m_text" } },
            { "Chest", new List<string>() { "interactText" } },
            { "HelpTooltipZone", new List<string>() { "title", "description" } },
            { "SceneSettings", new List<string>() { "formalSceneName" } },
            { "MailAsset", new List<string>() { "message" } },
            { "ClothingLayerData", new List<string>() { "menuName" } },
            { "BookShelf", new List<string>(){ "bookName", "text" } },
            { "Popup", new List<string>(){ "text", "description" } },
            { "SkillNode", new List<string>(){ "nodeTitle", "nodeName", "description", "singleDescriptionItem", "SkillTitle", "SkillDescription", "SkillNodeAmountText" } },
            { "NPCAI", new List<string>(){ "title", "questLocation" } },
            { "Inspectable", new List<string>() { "inspectionText", "dialogueName", "interactionText" } },
            { "EliosFountain", new List<string>() { "interactionText" } },
            { "RepairSign", new List<string>() { "text" } },
            { "ScenePortalSpot", new List<string>() { "_cantEnterNotificiation", "enterOverrideText" } },
        };

        static void Main(string[] args)
        {
            Ignore.Initialize();

            var directory = Environment.CurrentDirectory;
            directory = @"G:\SteamLibrary\steamapps\common\Sun Haven";
            //args = new string[] { "extract" };
            if (args.Length > 1 && (args[0] == "extract" || args[0] == "merge"))
                directory = args[1];

            while (!Verify(directory))
            {
                if (directory != "") System.Console.WriteLine(IsGerman() ? "Spiel nicht gefunden unter: " + directory : "Game was not found at path " + directory);
                System.Console.WriteLine(IsGerman() ? "Bitte geben Sie den Pfad zu Ihrem Spiel an: " : "Please enter the game path: ");
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
                System.Console.WriteLine(IsGerman() ? "Spiel gefunden. Wende Änderungen an..." : "Game found. Applying patch...");
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

        private static Regex ParenthesisRegex = new Regex("\\(([^\\)]+)\\)");

        static List<string> ParseLine(string line, bool parseParenthesis = false)
        {
            line = line.Trim();
            var parts = line.Split(new string[] { "[]", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            for (var i = 0; i < parts.Count; i++)
            {
                var p = parts[i].Trim();
                if (Ignore.ShouldIgnoreString(p))
                {
                    parts.RemoveAt(i);
                    i--;
                }
                else parts[i] = p;
            }

            if (parseParenthesis)
            {
                var c = parts.Count;
                for (var i = 0; i < c; i++)
                {
                    if (parts[i].EndsWith(")"))
                    {
                        var index = parts[i].LastIndexOf("(");
                        parts.Add(parts[i].Substring(0, index).Trim());
                        parts.Add(parts[i].Substring(index + 1, parts[i].Length - (index + 1) - 1).Trim());
                        parts.RemoveAt(i);
                        c--;
                        i--;
                    }
                }
            }
            return parts;
        }
        static bool CheckIfStringShouldBeIgnored(MethodBody body, Instruction instruction)
        {
            var previousInstruction0 = instruction?.Previous ?? null;
            var previousInstruction1 = previousInstruction0?.Previous ?? null;
            var previousInstruction2 = previousInstruction1?.Previous ?? null;
            var previousInstruction3 = previousInstruction2?.Previous ?? null;
            var previousInstruction4 = previousInstruction3?.Previous ?? null;

            var nextInstruction0 = instruction?.Next ?? null;
            var nextInstruction1 = nextInstruction0?.Next ?? null;
            var nextInstruction2 = nextInstruction1?.Next ?? null;
            var nextInstruction3 = nextInstruction2?.Next ?? null;
            var nextInstruction4 = nextInstruction3?.Next ?? null;
            var nextInstruction5 = nextInstruction4?.Next ?? null;
            var nextInstruction6 = nextInstruction5?.Next ?? null;
            if ((previousInstruction3 != null &&
                (previousInstruction3.OpCode.Code == Code.Call && previousInstruction3.Operand is MethodReference methodRefP3 && (methodRefP3.Name == "get_Instance") && (!(methodRefP3.DeclaringType is GenericInstanceType genericT) || genericT.GetElementType().FullName != "Wish.SingletonBehaviour`1" || genericT.GenericArguments.Count == 0 || (genericT.GenericArguments[0].FullName != "Wish.HelpTooltips" && genericT.GenericArguments[0].FullName != "Wish.NotificationStack" && genericT.GenericArguments[0].FullName != "Wish.HelpNotification")))
                )
                ||
                (previousInstruction0 != null &&
                (
                 (previousInstruction0.OpCode.Code == Code.Ldfld && previousInstruction0.Operand is FieldReference fieldRef0 && fieldRef0.FieldType.Name == "Animator") ||
                 (previousInstruction0.OpCode.Code == Code.Call && previousInstruction0.Operand is MethodReference methodRefP0 && methodRefP0.Name == "get_Instance" && (!(methodRefP0.DeclaringType is GenericInstanceType genericT0) || genericT0.GetElementType().FullName != "Wish.SingletonBehaviour`1" || genericT0.GenericArguments.Count == 0 || (genericT0.GenericArguments[0].FullName != "Wish.HelpTooltips" && genericT0.GenericArguments[0].FullName != "Wish.NotificationStack" && genericT0.GenericArguments[0].FullName != "Wish.HelpNotification")))
                )
                )
                ||
                (nextInstruction0 != null &&
                (((nextInstruction0.OpCode.Code == Code.Callvirt || nextInstruction0.OpCode.Code == Code.Call) && nextInstruction0.Operand is MethodReference methodRef0 && (methodRef0.Name == "GetColor" || methodRef0.Name == "RemovePauseObject" || methodRef0.Name == "AddPauseObject" || methodRef0.Name == "SetTrigger" || methodRef0.Name == "GetProgressBoolCharacter" || methodRef0.Name == "GetProgressBoolWorld" || methodRef0.Name == "GetProgressFloat" || methodRef0.Name == "GetProgressFloatWorld" || methodRef0.Name == "GetProgressIntCharacter" || methodRef0.Name == "GetProgressIntWorld" || methodRef0.Name == "GetProgressStringCharacter" || methodRef0.Name == "GetProgressStringWorld" || methodRef0.Name == "op_Equality" || methodRef0.Name == "QuestComplete" || methodRef0.Name == "Find" || methodRef0.Name == "set_name" || methodRef0.Name == "Equals" || methodRef0.Name == "HasQuest" || methodRef0.Name == "GetProgress" || methodRef0.Name == "set_Scene" || methodRef0.Name == "GetQuest")) ||
                (nextInstruction0.OpCode.Code == Code.Newobj && nextInstruction0.Operand is MethodReference methodRef0a && (methodRef0a.DeclaringType.Name == "GameObject")))
                )
                ||
                (nextInstruction1 != null &&
                ((nextInstruction1.OpCode.Code == Code.Callvirt || nextInstruction1.OpCode.Code == Code.Call) && nextInstruction1.Operand is MethodReference methodRef1 && (methodRef1.Name == "SetColor" || methodRef1.Name == "GetNode" || methodRef1.Name == "SetBool" || methodRef1.Name == "SetProgressBool" || methodRef1.Name == "SetProgressBoolCharacter" || methodRef1.Name == "SetProgressFloat" || methodRef1.Name == "SetProgressIntCharacter" || methodRef1.Name == "SetProgressStringCharacter" || methodRef1.Name == "TryGetProgressBoolCharacter" || methodRef1.Name == "TryGetProgressBoolWorld" || methodRef1.Name == "TryGetProgressFloat" || methodRef1.Name == "TryGetProgressFloatWorld" || methodRef1.Name == "TryGetProgressIntCharacter" || methodRef1.Name == "TryGetProgressIntWorld" || methodRef1.Name == "TryGetProgressString" || methodRef1.Name == "TryGetProgressStringWorld" || methodRef1.Name == "op_Equality" || methodRef1.Name == "InstantiateTemporaryNPC" || methodRef1.Name == "get_transform" || methodRef1.Name == "SetFloat" || methodRef1.Name == "SetInt" || methodRef1.Name == "CompleteQuest" || methodRef1.Name == "StartQuest" || methodRef1.Name == "Equals" || methodRef1.Name == "GetProgressBoolCharacter" || methodRef1.Name == "GetProgressBoolWorld" || methodRef1.Name == "GetProgressFloat" || methodRef1.Name == "GetProgressFloatWorld" || methodRef1.Name == "GetProgressIntCharacter" || methodRef1.Name == "GetProgressIntWorld" || methodRef1.Name == "GetProgressStringCharacter" || methodRef1.Name == "GetProgressStringWorld"))
                )
                ||
                (nextInstruction2 != null &&
                ((nextInstruction2.OpCode.Code == Code.Callvirt || nextInstruction2.OpCode.Code == Code.Call || nextInstruction2.OpCode.Code == Code.Ldvirtftn) && nextInstruction2.Operand is MethodReference methodRef2 && (methodRef2.Name == "SetProgressBoolWorld" || methodRef2.Name == "SetProgressIntWorld" || methodRef2.Name == "SetProgressFloatWorld" || methodRef2.Name == "SetProgressStringWorld" || methodRef2.Name == "Continue"))
                )
                ||
                (nextInstruction3 != null &&
                ((nextInstruction3.OpCode.Code == Code.Callvirt || nextInstruction3.OpCode.Code == Code.Call) && nextInstruction3.Operand is MethodReference methodRef3 && (methodRef3.Name == "PausePlayerWithDialogue"))
                )
                ||
                (nextInstruction4 != null &&
                (nextInstruction4.OpCode.Code == Code.Newobj && nextInstruction4.Operand is MethodReference methodRef4 && (methodRef4.DeclaringType.Name == "Color" && methodRef4.Parameters.Count == 4))
                )
                ||
                (nextInstruction6 != null &&
                ((nextInstruction6.OpCode.Code == Code.Callvirt || nextInstruction6.OpCode.Code == Code.Call) && nextInstruction6.Operand is MethodReference methodRef6 && (methodRef6.Name == "InstantiateTemporaryNPC"))
                )
                )
            {
                return true;
            }
            var search = instruction;
            do
            {
                search = search.Next;
                if (search == null)
                    break;
                if (search.OpCode.Code == Code.Callvirt || search.OpCode.Code == Code.Call)
                {
                    if (search.Operand is MethodReference methodRef)
                    {
                        if (methodRef.Name == "Concat")
                            continue;
                        else if (methodRef.Name == "Log") // || methodRef.Name == "SetColor" || methodRef.Name == "GetNode" || methodRef.Name == "SetBool" || methodRef.Name == "RemovePauseObject" || methodRef.Name == "AddPauseObject" || methodRef.Name == "SetTrigger")
                            return true;
                        else
                            break;
                    }
                }
            }
            while (true);
            return false;
        }
        static Dictionary<string, HashSet<string>> GetTypeStrings(TypeDefinition type)
        {
            var ret = new Dictionary<string, HashSet<string>>();
            if (Ignore.ShouldIgnoreType(type.FullName))
                return ret;
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
                if (!Ignore.ShouldIgnoreMethod(method.FullName))
                {
                    if (method.Body != null)
                    {
                        foreach (var instruction in method.Body.Instructions)
                        {
                            if (instruction.OpCode.Code == Code.Ldstr)
                            {
                                if (!ret.ContainsKey(method.FullName))
                                    ret.Add(method.FullName, new HashSet<string>());

                                if (!CheckIfStringShouldBeIgnored(method.Body, instruction))
                                {
                                    var str = (instruction.Operand as string).Trim();
                                    ret[method.FullName].Add(str);
                                }
                            }
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
                            assets.file.reader.Read(data, 0, (int)data.Length);

                            var lengthName = BitConverter.ToInt32(data, 0);
                            var startDataLength = lengthName + 4;
                            if ((startDataLength % 4) > 0)
                                startDataLength += 4 - (startDataLength % 4);
                            var lengthText = BitConverter.ToInt32(data, startDataLength);
                            var title = System.Text.Encoding.UTF8.GetString(data, 4, lengthName);
                            var text = System.Text.Encoding.UTF8.GetString(data, startDataLength + 4, lengthText);

                            var lines = text.Split(new string[] { "\r", "\n" }, StringSplitOptions.None);
                            for (var k = 0; k < lines.Length; k++)
                            {
                                var line = lines[k];
                                var dialogMarker = line.LastIndexOf("::");
                                if (dialogMarker > 0)
                                {
                                    var dialogLine = line.Substring(dialogMarker + 2);
                                    var commandIndex = dialogLine.IndexOf("//");
                                    if (commandIndex > 0)
                                        dialogLine = dialogLine.Substring(0, commandIndex);
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

            Dictionary<string, string> results = new Dictionary<string, string>();
            var newTrans = "";
            var newOrig = "";
            foreach (var d in newDialogue)
            {
                var lines = ParseLine(d);
                foreach (var line in lines)
                {
                    if (!originalDialogue.Contains(line) && !Ignore.ShouldIgnoreContext("Dialogue", line))
                    {
                        var k = "Dialogue||" + line;
                        var v = line;
                        if (!results.ContainsKey(k))
                            results.Add(k, v);
                    }
                }
            }

            foreach (var n in newMethods)
            {
                var found = new HashSet<string>();
                if (originalMethods.ContainsKey(n.Key))
                {
                    foreach (var nn in n.Value)
                    {
                        var lines = ParseLine(nn);
                        foreach (var line in lines)
                        {
                            if (!originalMethods[n.Key].Contains(line) && !Ignore.ShouldIgnoreString(line))
                            {
                                found.Add(line);
                            }
                        }
                    }
                }
                else
                {
                    foreach (var nn in n.Value)
                    {
                        var lines = ParseLine(nn);
                        foreach (var line in lines)
                            if (!Ignore.ShouldIgnoreString(line))
                                found.Add(line);
                    }
                }
                if (found.Count > 0)
                {
                    foreach (var f in found)
                    {
                        if (!Ignore.ShouldIgnoreContext(n.Key, f))
                        {
                            var k = n.Key + "||" + f;
                            var v = f;
                            if (!results.ContainsKey(k))
                                results.Add(k, v);
                        }
                    }
                }
            }

            foreach (var n in newLines)
            {
                var lines = ParseLine(n, true);
                foreach (var line in lines)
                {
                    if (!original.Contains(line) && !Ignore.ShouldIgnoreString(line))
                    {
                        var k = line;
                        var v = line;

                        if (!results.ContainsKey(k))
                            results.Add(k, v);
                    }
                }
            }

            foreach (var kv in results)
            {
                newOrig += kv.Key + "\r\n";
                newTrans += kv.Value + "\r\n";
            }

            /*File.WriteAllText("news.txt", newText);
            File.WriteAllText("newDialogue.txt", newDialogueText);*/
            File.WriteAllText("new.orig", newOrig);
            File.WriteAllText("new.trans", newTrans);
        }

        static bool Verify(string directory)
        {
            return File.Exists(Path.Combine(directory, "Sun Haven_Data", "Managed", "Assembly-CSharp.dll")) &&
                File.Exists(Path.Combine(directory, "Sun Haven_Data", "Managed", "Unity.TextMeshPro.dll")) &&
                File.Exists(Path.Combine(directory, "Sun Haven_Data", "Managed", "UnityEngine.CoreModule.dll")) &&
                File.Exists(Path.Combine(directory, "Sun Haven.exe"));
        }

        static bool IsGerman()
        {
            return CultureInfo.InstalledUICulture.TwoLetterISOLanguageName.ToLowerInvariant() == "de";
        }

        static void Apply(string directory)
        {
            var version = GetGameVersion(directory).Trim();
            if (version == null)
            {
                System.Console.WriteLine(IsGerman() ? "Ups! Die Spielversion konnte nicht festgestellt werden. Ab hier könnte es gefährlich werden!" : "Oops! Couldn't get the game version. Proceed with caution!");
            }
            else if (!RecommendedVersions.Contains(version))
            {
                System.Console.WriteLine(IsGerman() ? "Ups! Ihre Spielversion ist nicht empfohlen für diese Mod. Ab hier könnte es gefährlich werden!" : "Oops! Your game version is not recommended for this mod. Proceed with caution!");
                System.Console.WriteLine(IsGerman() ? "Ihre Spielversion: " + version : "Your game version: " + version);
                System.Console.WriteLine(IsGerman() ? "Empfohlene Spielversion: " + String.Join(", ", RecommendedVersions) : "Recommended game versions: " + String.Join(", ", RecommendedVersions));
            }
            else
            {
                System.Console.WriteLine(IsGerman() ? "Ihre Spielversion: " + version : "Your game version: " + version);
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
                    System.Console.WriteLine(IsGerman() ? "Es sieht so aus, als ob Ihr Spiel aktualisiert wurde. Falls dies nicht der Fall ist, höre jetzt auf!" : "It seems like your game was updated. If this is not the case: Stop now!");
                }
            }

            var currentDirectory = Environment.CurrentDirectory;
            var checkFiles = new string[] { "TranslatorPlugin.dll", "table.orig", "table.trans", "classdata.tpk", "ignore", "options" };
            foreach (var checkFile in checkFiles)
            {
                if (!File.Exists(Path.Combine(currentDirectory, checkFile)))
                {
                    System.Console.WriteLine(IsGerman() ? checkFile + " konnte nicht gefunden werden. Fortsetzung nicht möglich :(" : checkFile + " is missing! Can't proceed :(");
                    System.Console.Read();
                    return;
                }
            }

            System.Console.WriteLine(IsGerman() ? "Prüfung abgeschlossen. Glückwunsch!" : "Sanity checks completed! Congratulations!");
            System.Console.WriteLine("");
            System.Console.WriteLine(IsGerman() ? "Willkommen bei meinem Sun Haven Übersetzer Mod" : "Welcome to my Sun Haven translator mod.");
            System.Console.WriteLine("");
            System.Console.WriteLine(IsGerman() ? "Diese Software ist kostenlos. Das heißt, wenn Sie hierfür bezahlt haben, wurden Sie betrogen." : "This software is freeware. This means if somebody made you pay for it you have been ripped off.");
            System.Console.WriteLine(IsGerman() ? "Die offizielle Downloadseite für diese Mod ist potatoepet.de" : "The official download site for this tool is potatoepet.de");
            System.Console.WriteLine(IsGerman() ? "Sie können den Quellcode dieser quelloffenen Mod auf github.com/FluffyFishGames finden" : "You can also find this software as open source on github.com/FluffyFishGames");
            System.Console.WriteLine("");
            System.Console.WriteLine(IsGerman() ? "Diese Mod wird Ihre Spieldateien verändern. Deswegen müssen Sie einige Dinge wissen." : "This tool will modify your game files. Therefore you need to know a couple of things.");
            System.Console.WriteLine(IsGerman() ? "Nicht jeder Fehler, den Sie im Spiel finden, tritt unbedingt in einem unmodifizierten Spiel auf." : "Not every bug you might encounter is necessarily in the unmodded game.");
            System.Console.WriteLine(IsGerman() ? "Bevor Sie also einen Fehler an PixelSprout melden, stellen Sie sicher, dass der Fehler auch im unmodifizierten Spiel auftritt." : "Before submitting bug reports to PixelSprout please ensure your error also occurs in an unmodded game first.");
            System.Console.WriteLine(IsGerman() ? "Sollte es ein Spielupdate geben, wird der Mod kaputt gehen. Versuchen Sie nicht den Mod einfach erneut auszuführen, da dies zu kaputten Spieldateien führen kann." : "In case of an update this mod WILL break. DO NOT just start it again to translate as it will corrupt your files.");
            System.Console.WriteLine(IsGerman() ? "Der beste Weg mit einem Update umzugehen, ist zu warten, bis der Mod aktualisiert wird." : "Best way to handle updates is to wait for this tool to get updated.");
            System.Console.WriteLine("");
            System.Console.WriteLine(IsGerman() ? "Zusätzliche Mods" : "Additional mods");
            System.Console.WriteLine(IsGerman() ? "Es gibt ein paar kleine zusätzliche Mods, die aktiviert werden können. Geben Sie dafür den Buchstaben \"c\" ein und wählen dann die entsprechenden Mods aus" : "There are a few small additional mods that can be activated. For this, enter the letter \"c\" and then select the appropriate mods");
            System.Console.WriteLine("");
            List<int> options = new List<int>();
            while (true)
            {
                System.Console.WriteLine(IsGerman() ? "Bitte bestätigen Sie, dass Sie diesen Text verstanden haben, indem Sie \"y\" eingeben und Ihre Eingabe mit der Enter-Taste bestätigen. Alternativ drücken Sie die Taste \"c\" um Anpassungen vorzunehmen." : "Please confirm that you have understood this small disclaimer by typing \"y\" and confirm your input by hitting return. Alternatively, press the \"c\" key to make adjustments.");
                var input = System.Console.ReadLine().ToLowerInvariant().Trim();
                if (input == "y")
                {
                    System.Console.WriteLine(IsGerman() ? "Los geht's!" : "Here we go!");
                    ApplyPatches(directory, gameWasUpdated, version, options);
                    break;
                }
                else if (input == "c")
                {
                    System.Console.WriteLine("");
                    System.Console.WriteLine(IsGerman() ? "Anpassungen" : "Adjustements");
                    System.Console.WriteLine("");
                    System.Console.WriteLine(IsGerman() ? "Folgende Anpassungen stehen zur Verfügung. Geben Sie die Zahlen der Einträge, getrennt durch ein Leerzeichen, ein, um diese zu aktivieren." : "The following adjustments are available. Enter the numbers of the entries, separated by a space, to activate them.");
                    System.Console.WriteLine("");
                    System.Console.WriteLine(IsGerman() ? "1 - Jederzeit schlafen können" : "1 - Go to sleep anytime");
                    System.Console.WriteLine(IsGerman() ? "2 - Immer neue Dialoge" : "2 - Always new dialogue");
                    var input2 = System.Console.ReadLine().ToLowerInvariant().Trim();
                    var entries = input2.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    options.Clear();
                    for (var j = 0; j < entries.Length; j++)
                    {
                        if (entries[j] == "1")
                            options.Add(1);
                        if (entries[j] == "2")
                            options.Add(2);
                    }
                }
                else
                {
                }
            }
        }

        static void ApplyPatches(string directory, bool gameWasUpdated, string version, List<int> options)
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

            System.Console.WriteLine("Patching Wish.QuestProgressRequirement...");
            var questProgressRequirement = assemblyCSharpModule.GetType("Wish.QuestProgressRequirement");
            var questProgressRequirementMethods = questProgressRequirement.GetMethods();
            foreach (var m in questProgressRequirementMethods)
            {
                if (m.Name == "GetProgressNameString")
                {
                    var body = m.Body;
                    var ilProcessor = body.GetILProcessor();
                    var firstInstruction = body.Instructions[0];
                    ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldarg_1));
                    ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldstr, ""));
                    ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Call, getStringRef));
                    ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Starg, 1));
                }
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

            Dictionary<string, Dictionary<string, FieldDefinition>> allOriginalFields = new Dictionary<string, Dictionary<string, FieldDefinition>>();

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

                Dictionary<string, FieldDefinition> originalFields = new Dictionary<string, FieldDefinition>();
                foreach (var field in fields)
                {
                    var newField = new FieldDefinition("__" + field.Name, field.Attributes, field.FieldType);
                    newField.IsNotSerialized = true;
                    @class.Fields.Add(newField);
                    originalFields.Add(field.Name, newField);
                }
                allOriginalFields.Add(@class.Name, originalFields);

                if (awakeBody.Instructions.Count < 5 || awakeBody.Instructions[2].OpCode.Code != Code.Ldfld || awakeBody.Instructions[3].OpCode.Code != Code.Call || awakeBody.Instructions[4].OpCode.Code != Code.Stfld)
                {
                    if (awakeBody.Instructions.Count < 2 || awakeBody.Instructions[0].OpCode.Code != Code.Ldarg_0 || awakeBody.Instructions[1].OpCode.Code != Code.Call || ((MethodReference)awakeBody.Instructions[1].Operand).FullName != baseAwake.FullName)
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
                                ilProcessor.InsertBefore(baseInstruction, ilProcessor.Create(OpCodes.Stfld, originalFields[field.Name]));

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
                                ilProcessor.Emit(OpCodes.Stfld, originalFields[field.Name]);

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
                            ilProcessor.Emit(OpCodes.Stfld, originalFields[field.Name]);

                            ilProcessor.Emit(OpCodes.Ldarg_0);
                            ilProcessor.Emit(OpCodes.Ldarg_0);
                            ilProcessor.Emit(OpCodes.Ldfld, field);
                            ilProcessor.Emit(OpCodes.Ldstr, awakeMethod.FullName);
                            ilProcessor.Emit(OpCodes.Call, getStringRef);
                            ilProcessor.Emit(OpCodes.Stfld, field);
                        }
                    }
                }
                if (awakeBody.Instructions.Count == 0 || awakeBody.Instructions[awakeBody.Instructions.Count - 1].OpCode.Code != Code.Ret)
                    ilProcessor.Emit(OpCodes.Ret);
            }

            System.Console.WriteLine("Patching Wish.ItemData...");
            var itemData = assemblyCSharpModule.GetType("Wish.ItemData");
            var itemDataMethods = itemData.GetMethods();
            var itemDataFields = itemData.Fields;
            FieldReference itemDataOldName = null;
            FieldReference itemDataNewName = null;
            foreach (var f in itemDataFields)
            {
                if (f.Name == "name")
                    itemDataNewName = assemblyCSharpModule.ImportReference(f);
                if (f.Name == "__name")
                    itemDataOldName = assemblyCSharpModule.ImportReference(f);
            }
            if (itemDataOldName != null && itemDataNewName != null)
            {
                foreach (var m in itemDataMethods)
                {
                    if (m.Name == "get_ItemText")
                    {
                        var body = m.Body;
                        var ilProcessor = body.GetILProcessor();
                        for (var i = 0; i < body.Instructions.Count; i++)
                        {
                            if (body.Instructions[i].OpCode == OpCodes.Ldfld && body.Instructions[i].Operand is FieldReference fref && fref == itemDataNewName)
                                body.Instructions[i].Operand = itemDataOldName;
                        }
                    }
                }
                System.Console.WriteLine("Patching Wish.ItemDatabase...");
                var itemDatabase = assemblyCSharpModule.GetType("Wish.ItemDatabase");
                var itemDatabaseMethods = itemDatabase.GetMethods();
                foreach (var m in itemDatabaseMethods)
                {
                    if (m.Name == "ConstructDatabase" && m.Parameters.Count > 0)
                    {
                        var body = m.Body;
                        var ilProcessor = body.GetILProcessor();
                        for (var i = 0; i < body.Instructions.Count; i++)
                        {
                            if (body.Instructions[i].OpCode == OpCodes.Ldfld && body.Instructions[i].Operand is FieldReference fref && fref == itemDataNewName)
                                body.Instructions[i].Operand = itemDataOldName;
                        }
                    }
                }
            }


            var cutscene = assemblyCSharpModule.GetType("Wish.Cutscene");
            var patchMethods = new Dictionary<string, Dictionary<string, List<string>>>() {
                {"<DialogueSingle>d__42", new() {
                    {"MoveNext", new() {
                        "opener"
                    }}
                }},
                {"<>c__DisplayClass42_1", new() {
                    {"<DialogueSingle>b__0", new() {
                        "Item1"
                    }}
                }},
                {"<>c__DisplayClass43_1", new() {
                    {"<DialogueSingleWithResponse>b__0", new() {
                        "Item1"
                    }}
                }},
                {"<>c__DisplayClass39_1", new() {
                    {"<DialogueTreeChain>b__0", new() {
                        "Item1"
                    }}
                }},
                {"<>c__DisplayClass39_2", new() {
                    {"<DialogueTreeChain>b__2", new() {
                        "Item1"
                    }}
                }},
                {"<>c__DisplayClass40_1", new() {
                    {"<DialogueTreeNormal>b__0", new() {
                        "Item1"
                    }}
                }},
                {"<>c__DisplayClass40_2", new() {
                    {"<DialogueTreeNormal>b__2", new() {
                        "Item1"
                    }}
                }},
                {"<DialogueSingleNoResponse>d__41", new() {
                    {"MoveNext", new() {
                        "opener"
                    }}
                }},
                {"<DialogueSingleWithResponse>d__43", new() {
                    {"MoveNext", new() {
                        "opener"
                    }}
                }},
                {"<DialogueTreeChain>d__39", new() {
                    {"MoveNext", new() {
                        "opener"
                    }}
                }},
                {"<DialogueTreeNormal>d__40", new() {
                    {"MoveNext", new() {
                        "opener"
                    }}
                }},
                //DialogueSingleWithResponse | opener | responses[responseIndex] | Item1
                //DialogueTreeChain | opener | Item1 | responses[responseIndex]
                // DialogueTreeNormal | opener | Item1 | responses[responseIndex]
            };

            foreach (var nested in cutscene.NestedTypes)
            {
                if (patchMethods.ContainsKey(nested.Name))
                {
                    foreach (var m in nested.Methods)
                    {
                        if (patchMethods[nested.Name].ContainsKey(m.Name))
                        {
                            System.Console.WriteLine("Patching " + m.FullName);
                            var body = m.Body;
                            body.SimplifyMacros();
                            var processor = body.GetILProcessor();
                            for (var i = 0; i < body.Instructions.Count; i++)
                            {
                                var inst = body.Instructions[i];
                                //System.Console.WriteLine(inst.OpCode + " _ " + inst.Operand.GetType());
                                if (inst.OpCode == OpCodes.Ldfld && inst.Operand is FieldReference fld)
                                {
                                    if (fld.Name == "responseIndex" && inst.Next.Next.Operand is MethodReference mref && mref.Name == "Add")
                                    {
                                        var next = inst.Next;
                                        processor.InsertAfter(next, processor.Create(OpCodes.Call, getStringRef));
                                        processor.InsertAfter(next, processor.Create(OpCodes.Ldstr, ""));
                                        i += 3;
                                    }
                                    else if (patchMethods[nested.Name][m.Name].Contains(fld.Name))
                                    {
                                        processor.InsertAfter(inst, processor.Create(OpCodes.Call, getStringRef));
                                        processor.InsertAfter(inst, processor.Create(OpCodes.Ldstr, ""));
                                        i += 2;
                                    }
                                }
                            }
                            body.OptimizeMacros();
                        }
                    }
                }
            }

            if (options.Contains(1))
            {
                System.Console.WriteLine("Applying \"Sleep at anytime\" patch on Wish.Player...");

                var player = assemblyCSharpModule.GetType("Wish.Player");
                var playerMethods = player.GetMethods();
                foreach (var m in playerMethods)
                {
                    if (m.Name == "RequestSleep")
                    {
                        var body = m.Body;
                        var ilProcessor = body.GetILProcessor();
                        for (var i = 0; i < body.Instructions.Count - 1; i++)
                        {
                            if (body.Instructions[i].OpCode == OpCodes.Ldc_I4_S && body.Instructions[i + 1].OpCode == OpCodes.Bge_S)
                            {
                                body.Instructions[i].Operand = (sbyte)6;
                                break;
                            }
                        }
                    }
                }
            }

            if (options.Contains(2))
            {
                System.Console.WriteLine("Applying \"Always new dialogue\" patch on Wish.NPCAI...");

                var npcai = assemblyCSharpModule.GetType("Wish.NPCAI");
                var npcaiMethods = npcai.GetMethods();
                foreach (var m in npcaiMethods)
                {
                    if (m.Name == "GenerateCycle")
                    {
                        var body = m.Body;
                        body.SimplifyMacros();
                        var ilProcessor = body.GetILProcessor();
                        var first = body.Instructions[0];
                        ilProcessor.InsertBefore(first, ilProcessor.Create(OpCodes.Ldc_I4_1));
                        ilProcessor.InsertBefore(first, ilProcessor.Create(OpCodes.Starg, 1));
                        body.Optimize();
                    }
                }
            }


            var fishSpawnManager = assemblyCSharpModule.GetType("Wish.FishSpawnManager");
            var _m = fishSpawnManager.GetMethods();
            MethodDefinition spawnFishMethod = null;
            foreach (var __m in _m)
            {
                if (__m.Name == "SpawnFish" && __m.Parameters[0].ParameterType.FullName != "System.String")
                {
                    spawnFishMethod = __m;
                    break;
                }
            }

            var spawnFishMethodBody = spawnFishMethod.Body;
            spawnFishMethodBody.SimplifyMacros();
            var spawnFishIL = spawnFishMethodBody.GetILProcessor();

            for (var i = 0; i < spawnFishMethodBody.Instructions.Count; i++)
            {
                var inst = spawnFishMethodBody.Instructions[i];
                if (inst.OpCode == OpCodes.Ldfld && inst.Operand is FieldReference fr && fr.Name == "name")
                {
                    spawnFishIL.Replace(inst, spawnFishIL.Create(OpCodes.Ldfld, assemblyCSharpModule.ImportReference(allOriginalFields["ItemData"]["name"])));
                }
            }
            spawnFishMethodBody.OptimizeMacros();

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
                var copyFiles = new string[] { "table.orig", "table.trans", "ignore" };
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
            var assets = manager.LoadAssetsFile(Path.Combine(directory, "Sun Haven_Data", "level1"), true);
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
                                                var v = value.AsString();
                                            System.Console.WriteLine(v);
                                                if (v.StartsWith("Version"))
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
            if (ret == null)
                return "Unknown0";
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
            if (Ignore.ShouldIgnoreType(type.FullName))
                return;
            foreach (var t in type.NestedTypes)
            {
                ReplaceStrings(t, replaces, getString);
            }

            var str = "";
            foreach (var method in type.Methods)
            {
                if (Ignore.ShouldIgnoreMethod(method.FullName))
                    continue;
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
                                if (!CheckIfStringShouldBeIgnored(body, instruction))
                                {
                                    bool needsTranslation = true;
                                    var st = (instruction.Operand as string);
                                    if (!st.Contains("[]")) // for now treat all obvious dialogue as translateable
                                    {
                                        var currentStr = "";
                                        for (var j = 0; j < st.Length; j++)
                                        {
                                            bool check = st[j] == '\r' || st[j] == '\n';
                                            if (j < st.Length - 1 && st[j] == '[' && st[j + 1] == ']')
                                            {
                                                check = true;
                                                j++;
                                            }
                                            if (check)
                                            {
                                                if (currentStr != "")
                                                {
                                                    var trimmed = currentStr.Trim();
                                                    if (trimmed != "")
                                                    {
                                                        if (!replaces[method.FullName].Contains(trimmed))
                                                            needsTranslation = false;
                                                    }
                                                    currentStr = "";
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
                                    }

                                    if (needsTranslation)
                                    {
                                        processor.InsertAfter(instruction, processor.Create(OpCodes.Call, getString));
                                        processor.InsertAfter(instruction, processor.Create(OpCodes.Ldstr, method.FullName));
                                    }
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
