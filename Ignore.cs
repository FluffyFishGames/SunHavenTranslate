using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SunHavenTranslate
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
}
