using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace SunHavenTranslate
{
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

        public static void ChangeEnumToString(EnumsContainer container, ModuleDefinition module, MethodReference getObject, MethodReference getString)
        {
            foreach (var type in module.Types)
            {
                ChangeEnumToString(container, type, getObject, getString);
            }
        }

        public static void ChangeEnumToString(EnumsContainer container, TypeDefinition type, MethodReference getObject, MethodReference getString)
        {
            foreach (var subType in type.NestedTypes)
                ChangeEnumToString(container, subType, getObject, getString);

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
                                container.Enums.ContainsKey(((TypeReference) body.Instructions[i].Operand).FullName))
                            {
                                if (body.Instructions.Count > i && (body.Instructions[i + 1].OpCode.Code != Mono.Cecil.Cil.Code.Call || ((MethodReference) body.Instructions[i + 1].Operand).FullName != getObject.FullName))
                                {
                                    var instruction = body.Instructions[i];
                                    var lastCall = instruction;
                                    while ((lastCall.Next.OpCode.Code == Mono.Cecil.Cil.Code.Callvirt || lastCall.Next.OpCode.Code == Mono.Cecil.Cil.Code.Call) && ((MethodReference) lastCall.Next.Operand).Name == "ToLower")
                                        lastCall = lastCall.Next;
                                    ilProcessor.InsertAfter(lastCall, ilProcessor.Create(Mono.Cecil.Cil.OpCodes.Call, getObject));
                                    ilProcessor.InsertAfter(lastCall, ilProcessor.Create(Mono.Cecil.Cil.OpCodes.Ldstr, method.FullName));
                                    System.Console.WriteLine("Modifying Enum implicit ToString in " + method + ". Offset: " + instruction.Offset);
                                    i += 2;
                                }
                            }
                            if (i < body.Instructions.Count - 1 &&
                                body.Instructions[i].OpCode.Code == Mono.Cecil.Cil.Code.Constrained &&
                                container.Enums.ContainsKey(((TypeReference) body.Instructions[i].Operand).FullName) &&
                                body.Instructions[i + 1].OpCode.Code == Mono.Cecil.Cil.Code.Callvirt &&
                                ((MethodReference) body.Instructions[i + 1].Operand).Name == "ToString")
                            {
                                if (body.Instructions.Count > i + 1 && (body.Instructions[i + 2].OpCode.Code != Mono.Cecil.Cil.Code.Call || ((MethodReference) body.Instructions[i + 2].Operand).FullName != getObject.FullName))
                                {
                                    var instruction = body.Instructions[i + 1];
                                    var lastCall = instruction;
                                    while ((lastCall.Next.OpCode.Code == Mono.Cecil.Cil.Code.Callvirt || lastCall.Next.OpCode.Code == Mono.Cecil.Cil.Code.Call) && ((MethodReference) lastCall.Next.Operand).Name == "ToLower")
                                        lastCall = lastCall.Next;
                                    ilProcessor.InsertAfter(lastCall, ilProcessor.Create(Mono.Cecil.Cil.OpCodes.Call, getString));
                                    ilProcessor.InsertAfter(lastCall, ilProcessor.Create(Mono.Cecil.Cil.OpCodes.Ldstr, method.FullName)); 
                                    System.Console.WriteLine("Modifying Enum explicit ToString in " + method + ". Offset: " + instruction.Offset);
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

        public static EnumsContainer FindEnums(string directory, ModuleDefinition module)
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
                System.Console.WriteLine("Found " + enums + " Enums with " + values + " values.");
                return container;
            }
            return null;
        }
    }
}
