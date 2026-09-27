using System;
using System.IO;
using System.Xml.Linq;
using VikingFactory.Core;
using Xunit;

namespace VikingFactory.Core.Tests
{
    public sealed class VersionTests
    {
        [Fact]
        public void Version_props_matches_the_plugin_const_and_manifest()
        {
            var root = FindRoot();
            var props = XDocument.Load(Path.Combine(root, "Version.props"));
            var version = props.Root?.Element("PropertyGroup")?.Element("Version")?.Value.Trim();
            if (string.IsNullOrWhiteSpace(version))
                throw new InvalidOperationException("Version.props has no Version.");

            var assemblyVersion = typeof(BalanceDefaults).Assembly.GetName().Version?.ToString(3);
            Assert.Equal(version, assemblyVersion);

            var manifest = File.ReadAllText(Path.Combine(root, "packaging", "manifest.json"));
            Assert.Contains("\"version_number\": \"" + version + "\"", manifest);

            var plugin = File.ReadAllText(Path.Combine(root, "src", "VikingFactory.Plugin", "VikingFactoryPlugin.cs"));
            Assert.Contains("PluginVersion = \"" + version + "\"", plugin);
        }

        private static string FindRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "Version.props")))
                    return dir.FullName;
                dir = dir.Parent;
            }

            throw new InvalidOperationException("Version.props was not found above the test output.");
        }
    }
}
