using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Mono.Cecil.Cil;
using System.Text.RegularExpressions;
using ModManagerGUI;

namespace SunHavenTranslate
{
    public class Mod : IMod
    {

        public class Ignore
        {
            private static HashSet<string> Types = new HashSet<string>();
            private static HashSet<string> Methods = new HashSet<string>();
            private static HashSet<string> Strings = new HashSet<string>();
            private static HashSet<string> Context = new HashSet<string>();

            public static void Initialize()
            {
                if (File.Exists("./ignore"))
                {
                    var ignore = File.ReadAllLines("./ignore");
                    foreach (var line in ignore)
                    {
                        if (line.StartsWith("Type||"))
                            Types.Add(line.Substring(6));
                        else if (line.StartsWith("Method||"))
                            Methods.Add(line.Substring(8));
                        else if (line.Contains("||"))
                            Context.Add(line);
                        else
                            Strings.Add(line);
                    }
                }
            }

            public static bool ShouldIgnoreContext(string context, string str)
            {
                return Context.Contains(context + "||" + str);
            }

            public static bool ShouldIgnoreMethod(string name)
            {
                return Methods.Contains(name);
            }

            public static bool ShouldIgnoreString(string str)
            {
                return Strings.Contains(str);
            }

            public static bool ShouldIgnoreType(string type)
            {
                return Types.Contains(type);
            }
        }

        public class Enums
        {
            private static HashSet<string> IgnoreEnums = new HashSet<string>()
        {
            "BulletinBoardType"
        };

            public class EnumsContainer
            {
                public MethodReference GetName;
                public MethodReference GetTypeFromHandle;
                public Dictionary<string, Enum> Enums = new Dictionary<string, Enum>();
            }

            public class Enum
            {
                public TypeDefinition Type;
                public Dictionary<string, FieldDefinition> Fields = new Dictionary<string, FieldDefinition>();
            }

            public static void ChangeEnumToString(Mod mod, EnumsContainer container, ModuleDefinition module, MethodReference getObject, MethodReference getString)
            {
                foreach (var type in module.Types)
                {
                    ChangeEnumToString(mod, container, type, getObject, getString);
                }
            }

            public static void ChangeEnumToString(Mod mod, EnumsContainer container, TypeDefinition type, MethodReference getObject, MethodReference getString)
            {
                foreach (var subType in type.NestedTypes)
                    ChangeEnumToString(mod, container, subType, getObject, getString);

                if (type.Namespace == "Wish")
                {
                    foreach (var method in type.Methods)
                    {
                        if (type.Name == "Skills" && method.Name == "SetLevelProgress")
                            continue;
                        var body = method.Body;
                        if (body != null)
                        {
                            body.SimplifyMacros();
                            var ilProcessor = body.GetILProcessor();
                            for (var i = 0; i < body.Instructions.Count; i++)
                            {
                                if (body.Instructions[i].OpCode.Code == Mono.Cecil.Cil.Code.Box &&
                                    container.Enums.ContainsKey(((TypeReference)body.Instructions[i].Operand).FullName))
                                {
                                    if (body.Instructions.Count > i && (body.Instructions[i + 1].OpCode.Code != Mono.Cecil.Cil.Code.Call || ((MethodReference)body.Instructions[i + 1].Operand).FullName != getObject.FullName))
                                    {
                                        var instruction = body.Instructions[i];
                                        var lastCall = instruction;
                                        while ((lastCall.Next.OpCode.Code == Mono.Cecil.Cil.Code.Callvirt || lastCall.Next.OpCode.Code == Mono.Cecil.Cil.Code.Call) && ((MethodReference)lastCall.Next.Operand).Name == "ToLower")
                                            lastCall = lastCall.Next;
                                        ilProcessor.InsertAfter(lastCall, ilProcessor.Create(Mono.Cecil.Cil.OpCodes.Call, getObject));
                                        ilProcessor.InsertAfter(lastCall, ilProcessor.Create(Mono.Cecil.Cil.OpCodes.Ldstr, method.FullName));
                                        mod.WriteLog("Modifying Enum implicit ToString in " + method + ". Offset: " + instruction.Offset);
                                        i += 2;
                                    }
                                }
                                if (i < body.Instructions.Count - 1 &&
                                    body.Instructions[i].OpCode.Code == Mono.Cecil.Cil.Code.Constrained &&
                                    container.Enums.ContainsKey(((TypeReference)body.Instructions[i].Operand).FullName) &&
                                    body.Instructions[i + 1].OpCode.Code == Mono.Cecil.Cil.Code.Callvirt &&
                                    ((MethodReference)body.Instructions[i + 1].Operand).Name == "ToString")
                                {
                                    if (body.Instructions.Count > i + 1 && (body.Instructions[i + 2].OpCode.Code != Mono.Cecil.Cil.Code.Call || ((MethodReference)body.Instructions[i + 2].Operand).FullName != getObject.FullName))
                                    {
                                        var instruction = body.Instructions[i + 1];
                                        var lastCall = instruction;
                                        while ((lastCall.Next.OpCode.Code == Mono.Cecil.Cil.Code.Callvirt || lastCall.Next.OpCode.Code == Mono.Cecil.Cil.Code.Call) && ((MethodReference)lastCall.Next.Operand).Name == "ToLower")
                                            lastCall = lastCall.Next;
                                        ilProcessor.InsertAfter(lastCall, ilProcessor.Create(Mono.Cecil.Cil.OpCodes.Call, getString));
                                        ilProcessor.InsertAfter(lastCall, ilProcessor.Create(Mono.Cecil.Cil.OpCodes.Ldstr, method.FullName));
                                        mod.WriteLog("Modifying Enum explicit ToString in " + method + ". Offset: " + instruction.Offset);
                                        i += 2;
                                    }
                                }
                            }

                            body.OptimizeMacros();
                            body.Optimize();
                        }
                    }
                }
            }

            public static EnumsContainer FindEnums(Mod mod, string directory, ModuleDefinition module)
            {
                var container = new EnumsContainer();
                for (var i = 0; i < module.AssemblyReferences.Count; i++)
                {
                    var reference = module.AssemblyReferences[i];
                    if (reference.Name == "mscorlib")
                    {
                        var mscorlibPath = System.IO.Path.Combine(directory, "mscorlib.dll");
                        var mscorlibAssembly = AssemblyDefinition.ReadAssembly(mscorlibPath);
                        var enumType = mscorlibAssembly.MainModule.GetType("System.Enum");
                        foreach (var m in enumType.Methods)
                        {
                            if (m.Name == "GetName")// && (m.Attributes & MethodAttributes.Virtual) == MethodAttributes.Virtual && m.Parameters.Count == 0)
                                container.GetName = module.ImportReference(m);
                        }
                        var typeType = mscorlibAssembly.MainModule.GetType("System.Type");
                        foreach (var m in typeType.Methods)
                        {
                            if (m.Name == "GetTypeFromHandle" && m.Parameters[0].ParameterType.IsValueType)
                                container.GetTypeFromHandle = module.ImportReference(m);
                        }
                        mscorlibAssembly.Dispose();
                    }
                }

                int values = 0;
                int enums = 0;
                if (container.GetName != null)
                {
                    foreach (var type in module.Types)
                    {
                        if (type.Namespace == "Wish" && !IgnoreEnums.Contains(type.Name))
                        {
                            if (type.BaseType != null && type.BaseType.FullName == "System.Enum")
                            {
                                enums++;
                                var @enum = new Enum() { Type = type };
                                foreach (var field in type.Fields)
                                {
                                    if (field.IsLiteral)
                                    {
                                        values++;
                                        @enum.Fields.Add(field.Name, field);
                                    }
                                }
                                container.Enums.Add(type.FullName, @enum);
                            }
                        }
                    }
                    mod.WriteLog("Found " + enums + " Enums with " + values + " values.");
                    return container;
                }
                return null;
            }
        }

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

        public override bool Verify(string directory)
        {
            if (directory == null)
                return false;
            for (var i = 0; i < ModManagerGUI.ModManager.Configuration.FileNames.Length; i++)
            {
                var name = ModManagerGUI.ModManager.Configuration.FileNames[i];
                if (File.Exists(Path.Combine(directory, name + "_Data", "Managed", "Assembly-CSharp.dll")) &&
                    File.Exists(Path.Combine(directory, name + "_Data", "Managed", "Unity.TextMeshPro.dll")) &&
                    File.Exists(Path.Combine(directory, name + "_Data", "Managed", "UnityEngine.CoreModule.dll")) &&
                    File.Exists(Path.Combine(directory, name + ".exe")))
                    return true;
            }
            return false;
        }

        public string GetGameName(string directory)
        {
            if (directory == null)
                return null;
            for (var i = 0; i < ModManagerGUI.ModManager.Configuration.FileNames.Length; i++)
            {
                var name = ModManagerGUI.ModManager.Configuration.FileNames[i];
                if (File.Exists(Path.Combine(directory, name + "_Data", "Managed", "Assembly-CSharp.dll")) &&
                    File.Exists(Path.Combine(directory, name + "_Data", "Managed", "Unity.TextMeshPro.dll")) &&
                    File.Exists(Path.Combine(directory, name + "_Data", "Managed", "UnityEngine.CoreModule.dll")) &&
                    File.Exists(Path.Combine(directory, name + ".exe")))
                    return name;
            }
            return null;
        }

        private void WriteLog(string log)
        {
            if (OnLog != null)
                OnLog(log);
        }

        public override void Apply(string gameDirectory, HashSet<int> options)
        {
            Ignore.Initialize();
            var gameName = GetGameName(gameDirectory);
            WriteLog("Game name is: " + gameName);
            WriteLog("Copying files if needed...");

            bool isModded = CheckIfModded(gameDirectory, gameName);
            CopyFiles(gameDirectory, isModded, gameName);

            var translatorBackupPath = Path.Combine(gameDirectory, "TranslatorBackup");
            var managedPath = Path.Combine(gameDirectory, "Sun Haven_Data", "Managed");

            WriteLog("Reading assemblies and fetching types and enums...");

            var resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(Path.Combine(managedPath));

            WriteLog("Fetching methods from TranslatorPlugin.dll");
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

            WriteLog("Fetching methods from UnityEngine.CoreModule.dll");

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
                WriteLog("Couldn't find necessary methods. Can't proceed :(");
                return;
            }

            WriteLog("Patching Unity.TextMeshPro.dll");
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
                    WriteLog("Patching TMPro.TMP_Text.text setter...");
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
                WriteLog("Adding start method to TMPro.TextMeshProUGUI...");
                startMethod = new MethodDefinition("Start", MethodAttributes.Family | MethodAttributes.HideBySig | MethodAttributes.Virtual, textMeshProModule.TypeSystem.Void);
                textMeshProGUI.Methods.Add(startMethod);
            }

            if (startMethod != null)
            {
                WriteLog("Filling method body of TMPro.TextMeshProUGUI.Start...");
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

            WriteLog("Saving Unity.TextMeshPro.dll");
            WriteLog(textMeshProModule.FileName);
            WriteLog(System.IO.Path.Combine(managedPath, "Unity.TextMeshPro.dll"));
            textMeshProAssembly.Write(System.IO.Path.Combine(managedPath, "Unity.TextMeshPro.dll"));
            WriteLog("Assembly saved successfully!");

            WriteLog("Loading method patching data...");
            var original = System.IO.File.ReadAllLines(System.IO.Path.Combine(gameDirectory, "table.orig"));

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
            WriteLog("Found " + replaces.Count + " methods to patch.");

            var assemblyCSharp = System.IO.Path.Combine(translatorBackupPath, "Assembly-CSharp.dll");
            var assemblyCSharpAssembly = AssemblyDefinition.ReadAssembly(assemblyCSharp, new ReaderParameters { AssemblyResolver = resolver });
            var assemblyCSharpModule = assemblyCSharpAssembly.MainModule;

            WriteLog("Looking for enum types...");
            var enums = Enums.FindEnums(this, managedPath, assemblyCSharpModule);
            WriteLog("Patching methods which use enums to use Translator...");
            Enums.ChangeEnumToString(this, enums, assemblyCSharpModule, assemblyCSharpModule.ImportReference(translateObjectMethod), assemblyCSharpModule.ImportReference(translateStringMethod));

            getStringRef = assemblyCSharpModule.ImportReference(translateStringMethod);
            var translateDialogRef = assemblyCSharpModule.ImportReference(translateDialogMethod);
            var textAssetConstructorRef = assemblyCSharpModule.ImportReference(textAssetConstructor);
            var textAssetRef = assemblyCSharpModule.ImportReference(textAssetClass);
            var objectSetNameRef = assemblyCSharpModule.ImportReference(objectSetName);
            var objectGetNameRef = assemblyCSharpModule.ImportReference(objectGetName);
            var getTextRef = assemblyCSharpModule.ImportReference(getTextMethod);
            var dialogueTreeClass = assemblyCSharpModule.GetType("Wish.DialogueTree");

            WriteLog("Replacing inline strings...");
            foreach (var type in assemblyCSharpModule.Types)
            {
                if (type.Namespace == "Wish")
                    ReplaceStrings(type, replaces, getStringRef);
            }

            WriteLog("Patching Wish.QuestProgressRequirement...");
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


            WriteLog("Patching Wish.DialougeTree:ParseTextAsset...");
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
                WriteLog("Patching Wish." + kv.Key);

                var @class = assemblyCSharpModule.GetType("Wish." + kv.Key);
                MethodDefinition awakeMethod = null;
                WriteLog("Searching for awake method...");
                foreach (var method in @class.Methods)
                {
                    if (method.Name == "Awake")
                        awakeMethod = method;
                }

                WriteLog("Searching for fields...");
                List<FieldDefinition> fields = new List<FieldDefinition>();
                foreach (var field in @class.Fields)
                {
                    if (kv.Value.Contains(field.Name))
                        fields.Add(field);
                }

                WriteLog("Checking for base type...");
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
                    WriteLog("Creating new Awake method...");
                    awakeMethod = new MethodDefinition("Awake", MethodAttributes.Family | MethodAttributes.HideBySig | MethodAttributes.Virtual, assemblyCSharpModule.TypeSystem.Void);
                    @class.Methods.Add(awakeMethod);
                }

                var awakeBody = awakeMethod.Body;
                var ilProcessor = awakeBody.GetILProcessor();

                WriteLog("Patching method body...");
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

            WriteLog("Patching Wish.ItemData...");
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
                WriteLog("Patching Wish.ItemDatabase...");
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
                            WriteLog("Patching " + m.FullName);
                            var body = m.Body;
                            body.SimplifyMacros();
                            var processor = body.GetILProcessor();
                            for (var i = 0; i < body.Instructions.Count; i++)
                            {
                                var inst = body.Instructions[i];
                                //WriteLog(inst.OpCode + " _ " + inst.Operand.GetType());
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

            if (options.Contains(0))
            {
                WriteLog("Applying \"Sleep at anytime\" patch on Wish.Player...");

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

            if (options.Contains(1))
            {
                WriteLog("Applying \"Always new dialogue\" patch on Wish.NPCAI...");

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

            WriteLog("Saving Assembly-CSharp.dll");
            assemblyCSharpModule.Resources.Add(new EmbeddedResource("Modded", ManifestResourceAttributes.Public, new byte[] { }));
            assemblyCSharpAssembly.Write(System.IO.Path.Combine(managedPath, "Assembly-CSharp.dll"));

            WriteLog("Patching complete! :)");
        }

        void CopyFiles(string gameDirectory, bool isModded, string gameName)
        {
            var translatorBackupPath = Path.Combine(gameDirectory, "TranslatorBackup");
            if (!Directory.Exists(translatorBackupPath))
            {
                WriteLog("Translator Backup directory doesn't exist yet. Creating now.");
                Directory.CreateDirectory(translatorBackupPath);
            }

            var currentDirectory = Environment.CurrentDirectory;
            if (Path.GetFullPath(currentDirectory) != Path.GetFullPath(gameDirectory))
            {
                var copyFiles = new string[] { "table.orig", "table.trans", "ignore" };
                foreach (var copyFile in copyFiles)
                {
                    WriteLog("Copying " + copyFile);
                    File.Copy(Path.Combine(currentDirectory, copyFile), Path.Combine(gameDirectory, copyFile), true);
                }
            }

            var managedPath = Path.Combine(gameDirectory, gameName + "_Data", "Managed");

            WriteLog("Copying TranslatorPlugin.dll to Managed directory...");
            File.Copy(Path.Combine(currentDirectory, "TranslatorPlugin.dll"), Path.Combine(managedPath, "TranslatorPlugin.dll"), true);

            var checkFiles = new string[] { "Assembly-CSharp.dll", "Unity.TextMeshPro.dll", "UnityEngine.CoreModule.dll" };
            foreach (var checkFile in checkFiles)
            {
                if (!isModded || !System.IO.File.Exists(Path.Combine(translatorBackupPath, checkFile)))
                {
                    WriteLog(checkFile + " need to be copied. Copying now.");
                    File.Copy(Path.Combine(managedPath, checkFile), Path.Combine(translatorBackupPath, checkFile), true);
                }
            }
        }

        private void ReplaceStrings(TypeDefinition type, Dictionary<string, HashSet<string>> replaces, MethodReference getString)
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
                    WriteLog("Patching " + method.FullName);
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
        }

        bool CheckIfStringShouldBeIgnored(MethodBody body, Instruction instruction)
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

        bool CheckIfModded(string gameDirectory, string gameName)
        {
            var managedPath = Path.Combine(gameDirectory, gameName + "_Data", "Managed");
            var resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(Path.Combine(managedPath));
            var assemblyCSharp = System.IO.Path.Combine(managedPath, "Assembly-CSharp.dll");
            var assemblyCSharpAssembly = AssemblyDefinition.ReadAssembly(assemblyCSharp, new ReaderParameters { AssemblyResolver = resolver });
            var assemblyCSharpModule = assemblyCSharpAssembly.MainModule;
            try
            {
                foreach (var resource in assemblyCSharpModule.Resources)
                {
                    if (resource.Name == "Modded")
                        return true;
                }
                return false;
            }
            finally
            {
                assemblyCSharpModule.Dispose();
                assemblyCSharpAssembly.Dispose();
            }
        }
    }
}
