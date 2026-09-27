using System;
using System.IO;

namespace VikingFactory.Diagnostics
{
    /// <summary>
    /// Startup probe. It does not invent recipe data when the game is absent.
    /// </summary>
    internal static class Program
    {
        private static int Main()
        {
            Console.WriteLine("VikingFactory diagnostic");
            Console.WriteLine("Core assembly: VikingFactory.Core 0.1.0");
            var install = ReadProp("VALHEIM_INSTALL");
            if (string.IsNullOrWhiteSpace(install))
            {
                Console.WriteLine("VALHEIM_INSTALL is empty. No live item, recipe, or station catalogue was exported.");
                Console.WriteLine("Next: install Valheim, copy Environment.props.example to Environment.props, and set VALHEIM_INSTALL.");
                return 2;
            }

            var managed = Path.Combine(install, "valheim_Data", "Managed", "assembly_valheim.dll");
            if (!File.Exists(managed))
                managed = Path.Combine(install, "Valheim_Data", "Managed", "assembly_valheim.dll");

            if (!File.Exists(managed))
            {
                Console.WriteLine("Game assembly not found under " + install);
                return 2;
            }

            Console.WriteLine("Found " + managed);
            Console.WriteLine("Live catalogue export runs inside the game, after vanilla prefabs load, to BepInEx/config/VikingFactory/catalogue.json.");
            return 0;
        }

        private static string ReadProp(string name)
        {
            var fromEnv = Environment.GetEnvironmentVariable(name);
            if (!string.IsNullOrWhiteSpace(fromEnv))
                return fromEnv;

            var path = Path.Combine(Directory.GetCurrentDirectory(), "Environment.props");
            if (!File.Exists(path))
                return "";

            var text = File.ReadAllText(path);
            var token = "<" + name + ">";
            var start = text.IndexOf(token, StringComparison.Ordinal);
            if (start < 0)
                return "";
            start += token.Length;
            var end = text.IndexOf("</" + name + ">", start, StringComparison.Ordinal);
            if (end < 0)
                return "";
            return text.Substring(start, end - start).Trim();
        }
    }
}
