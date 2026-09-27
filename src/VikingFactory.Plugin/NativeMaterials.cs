using System.Collections.Generic;
using Jotunn.Managers;
using UnityEngine;

namespace VikingFactory
{
    /// <summary>
    /// Clones vanilla materials recorded by MaterialProbe. The originals are not changed.
    /// </summary>
    public static class NativeMaterials
    {
        private static readonly Dictionary<string, string> SlotToMaterial = new Dictionary<string, string>
        {
            { "Oak", "woodwall_worn" },
            { "OakLight", "woodwall_worn" },
            { "Iron", "Ironbeam_mat" },
            { "Bronze", "PotsNpans_mat" },
            { "Hide", "Tanningrack_mat" },
            { "Stone", "stonekit_floor_interior" }
        };

        private static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();

        public static bool TryResolve(string slotName, out Material material, out string note)
        {
            material = null;
            note = "";
            if (string.IsNullOrEmpty(slotName) || !SlotToMaterial.ContainsKey(slotName))
                return false;
            if (Cache.TryGetValue(slotName, out material))
            {
                note = slotName + "=cached";
                return true;
            }

            var sourceName = SlotToMaterial[slotName];
            var source = Find(sourceName);
            if (source == null)
            {
                note = slotName + "=MISSING " + sourceName + " (development fallback)";
                return false;
            }

            material = new Material(source);
            material.name = "vf_" + sourceName;
            if (sourceName == "woodwall_worn")
            {
                var names = material.GetTexturePropertyNames();
                for (var i = 0; i < names.Length; i++)
                    material.SetTextureScale(names[i], Vector2.one);
                note = slotName + "=woodwall_worn shader " + source.shader.name + " clone scale set to 1,1 (source _MainTex scale was authored for the wall mesh)";
            }
            else
                note = slotName + "=" + sourceName + " shader " + source.shader.name;

            Cache[slotName] = material;
            return true;
        }

        private static Material Find(string materialName)
        {
            var renderers = PrefabManager.Cache.GetPrefabs(typeof(MeshRenderer));
            if (renderers == null)
                return null;
            foreach (var pair in renderers)
            {
                var renderer = pair.Value as MeshRenderer;
                if (renderer == null)
                    continue;
                var materials = renderer.sharedMaterials;
                if (materials == null)
                    continue;
                for (var i = 0; i < materials.Length; i++)
                {
                    var candidate = materials[i];
                    if (candidate != null && candidate.name == materialName)
                        return candidate;
                }
            }

            return null;
        }
    }
}
