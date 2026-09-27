using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BepInEx;
using Jotunn.Managers;
using UnityEngine;

namespace VikingFactory
{
    /// <summary>
    /// Records vanilla material slots from prefabs already loaded by the game.
    /// It does not copy textures into the mod.
    /// </summary>
    public static class MaterialProbe
    {
        public static string Write()
        {
            var directory = Path.Combine(Paths.ConfigPath, "VikingFactory");
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "materials.json");
            var seen = new HashSet<string>();
            var json = new StringBuilder();
            json.Append("{\n  \"slots\": [\n");
            var first = true;
            var renderers = PrefabManager.Cache.GetPrefabs(typeof(MeshRenderer));
            if (renderers != null)
            {
                foreach (var pair in renderers)
                {
                    var renderer = pair.Value as MeshRenderer;
                    if (renderer == null)
                        continue;
                    Record(json, seen, ref first, renderer);
                }
            }

            json.Append("\n  ]\n}\n");
            File.WriteAllText(path, json.ToString(), new UTF8Encoding(false));
            return path;
        }

        private static void Record(StringBuilder json, HashSet<string> seen, ref bool first, MeshRenderer renderer)
        {
            var materials = renderer.sharedMaterials;
            if (materials == null)
                return;
            for (var slot = 0; slot < materials.Length; slot++)
            {
                var material = materials[slot];
                if (material == null || material.shader == null)
                    continue;
                var textures = material.GetTexturePropertyNames();
                var signature = material.shader.name + "|" + material.name;
                if (textures != null)
                {
                    for (var i = 0; i < textures.Length; i++)
                    {
                        var texture = material.GetTexture(textures[i]);
                        signature += "|" + textures[i] + ":" + (texture != null ? texture.name : "");
                    }
                }

                if (!seen.Add(signature))
                    continue;
                if (!first)
                    json.Append(",\n");
                first = false;
                json.Append("    {\"prefab\": \"").Append(Esc(renderer.gameObject.name)).Append("\"");
                json.Append(", \"path\": \"").Append(Esc(PathOf(renderer.transform))).Append("\"");
                json.Append(", \"slot\": ").Append(slot);
                json.Append(", \"material\": \"").Append(Esc(material.name)).Append("\"");
                json.Append(", \"shader\": \"").Append(Esc(material.shader.name)).Append("\"");
                json.Append(", \"keywords\": [");
                var keywords = material.shaderKeywords;
                if (keywords != null)
                {
                    for (var i = 0; i < keywords.Length; i++)
                    {
                        if (i > 0)
                            json.Append(", ");
                        json.Append("\"").Append(Esc(keywords[i])).Append("\"");
                    }
                }

                json.Append("], \"textures\": [");
                if (textures != null)
                {
                    var written = 0;
                    for (var i = 0; i < textures.Length; i++)
                    {
                        var texture = material.GetTexture(textures[i]);
                        if (texture == null)
                            continue;
                        if (written > 0)
                            json.Append(", ");
                        written++;
                        var scale = material.GetTextureScale(textures[i]);
                        var offset = material.GetTextureOffset(textures[i]);
                        var image = texture as Texture2D;
                        json.Append("{\"property\": \"").Append(Esc(textures[i])).Append("\"");
                        json.Append(", \"texture\": \"").Append(Esc(texture.name)).Append("\"");
                        json.Append(", \"width\": ").Append(image != null ? image.width : 0);
                        json.Append(", \"height\": ").Append(image != null ? image.height : 0);
                        json.Append(", \"scale\": [").Append(scale.x.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
                        json.Append(", ").Append(scale.y.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)).Append("]");
                        json.Append(", \"offset\": [").Append(offset.x.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
                        json.Append(", ").Append(offset.y.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)).Append("]}");
                    }
                }

                json.Append("]}");
            }
        }

        private static string PathOf(Transform transform)
        {
            var names = new List<string>();
            while (transform != null)
            {
                names.Add(transform.name);
                transform = transform.parent;
            }

            names.Reverse();
            return string.Join("/", names.ToArray());
        }

        private static string Esc(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "").Replace("\n", " ");
        }
    }
}
