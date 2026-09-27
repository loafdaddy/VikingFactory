using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using VikingFactory.Core;

namespace VikingFactory
{
    /// <summary>
    /// Builds a piece visual from a prototype GLB. Positions use an explicit (x, y, -z)
    /// conversion from the pack's right-handed Y-up coordinates into Unity.
    /// </summary>
    public static class GlbPieceVisual
    {
        public struct BoxSpec
        {
            public Vector3 Center;
            public Vector3 Size;
        }

        public static bool TryAttach(GameObject prefab, string path, BoxSpec[] staticBoxes, out string report)
        {
            report = "";
            if (prefab == null || string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                report = "Model file was not found: " + path;
                return false;
            }

            var renderer = prefab.GetComponent<Renderer>();
            var shader = renderer != null && renderer.sharedMaterial != null ? renderer.sharedMaterial.shader : null;
            if (shader == null)
            {
                report = "The piece has no shader, so the model was not attached.";
                return false;
            }

            var bytes = File.ReadAllBytes(path);
            var jsonLength = BitConverter.ToInt32(bytes, 12);
            var json = Encoding.UTF8.GetString(bytes, 20, jsonLength);
            var binHeader = 20 + jsonLength;
            var binLength = BitConverter.ToInt32(bytes, binHeader);
            var bin = new byte[binLength];
            Buffer.BlockCopy(bytes, binHeader + 8, bin, 0, binLength);

            var root = (Dictionary<string, object>)JsonValue.Parse(json);
            var nodes = (List<object>)root["nodes"];
            var meshes = (List<object>)root["meshes"];
            var materials = root.ContainsKey("materials") ? (List<object>)root["materials"] : new List<object>();
            var accessors = (List<object>)root["accessors"];
            var views = (List<object>)root["bufferViews"];

            var useNative = false;
            for (var i = 0; i < nodes.Count; i++)
            {
                var node = (Dictionary<string, object>)nodes[i];
                if (Extras(node, "material_profile") == "valheim")
                    useNative = true;
            }

            var colors = new Material[materials.Count];
            var notes = new StringBuilder();
            for (var i = 0; i < materials.Count; i++)
            {
                var factor = Color.white;
                var material = (Dictionary<string, object>)materials[i];
                var slotName = material.ContainsKey("name") ? (string)material["name"] : "";
                if (material.ContainsKey("pbrMetallicRoughness"))
                {
                    var pbr = (Dictionary<string, object>)material["pbrMetallicRoughness"];
                    if (pbr.ContainsKey("baseColorFactor"))
                        factor = ReadColor((List<object>)pbr["baseColorFactor"]);
                }

                string note = "";
                Material native;
                if (useNative && NativeMaterials.TryResolve(slotName, out native, out note))
                    colors[i] = native;
                else
                {
                    colors[i] = new Material(shader);
                    colors[i].color = factor;
                    colors[i].name = "vf_fallback_" + slotName;
                    if (string.IsNullOrEmpty(note))
                        note = slotName + "=development colour fallback";
                }

                if (!string.IsNullOrEmpty(note))
                    notes.Append(" ").Append(note);
            }

            var created = new Transform[nodes.Count];
            var parents = new int[nodes.Count];
            for (var i = 0; i < nodes.Count; i++)
                parents[i] = -1;
            for (var i = 0; i < nodes.Count; i++)
            {
                var node = (Dictionary<string, object>)nodes[i];
                if (!node.ContainsKey("children"))
                    continue;
                var children = (List<object>)node["children"];
                for (var c = 0; c < children.Count; c++)
                    parents[Convert.ToInt32(children[c])] = i;
            }

            var colliderNodes = new List<KeyValuePair<GameObject, Vector3>>();
            var visual = new GameObject("model");
            visual.transform.SetParent(prefab.transform, false);
            for (var i = 0; i < nodes.Count; i++)
            {
                var node = (Dictionary<string, object>)nodes[i];
                var name = node.ContainsKey("name") ? (string)node["name"] : "node";
                var go = new GameObject(name);
                created[i] = go.transform;
                if (node.ContainsKey("translation"))
                    go.transform.localPosition = Flip((List<object>)node["translation"]);
                if (node.ContainsKey("mesh"))
                    AddMesh(go, meshes[Convert.ToInt32(node["mesh"])], accessors, views, bin, colors);
                var axisName = Extras(node, "rotation_axis");
                if (!string.IsNullOrEmpty(axisName))
                {
                    var spin = go.AddComponent<SpinningPart>();
                    spin.LocalAxis = Axis(axisName);
                    spin.Swing = Extras(node, "motion") == "swing";
                }
                if (Extras(node, "scroll") == "u")
                    go.AddComponent<ScrollingPart>();
                if (Extras(node, "collider") == "box")
                {
                    var size = ExtrasList(node, "size");
                    if (size != null && size.Count >= 3)
                        colliderNodes.Add(new KeyValuePair<GameObject, Vector3>(go, new Vector3(Num(size[0]), Num(size[1]), Num(size[2]))));
                }
            }

            for (var i = 0; i < created.Length; i++)
            {
                var parent = parents[i] >= 0 ? created[parents[i]] : visual.transform;
                created[i].SetParent(parent, false);
            }

            Transform staticPart = null;
            var markerReport = new StringBuilder();
            for (var i = 0; i < created.Length; i++)
            {
                if (created[i].name == "Static")
                    staticPart = created[i];
                var nodeName = created[i].name;
                if (nodeName == "Rotor" || nodeName == "Crank" || nodeName == "Arm_Yaw" || nodeName == "Millstone"
                    || nodeName == "Drill" || nodeName == "Selector" || nodeName == "Belt_Surface"
                    || nodeName.StartsWith("Roller") || nodeName.StartsWith("Kinetic")
                    || nodeName == "Pickup" || nodeName == "Dropoff")
                    markerReport.Append(" ").Append(created[i].name).Append("=(")
                    .Append(created[i].localPosition.x.ToString("0.###", CultureInfo.InvariantCulture)).Append(", ")
                    .Append(created[i].localPosition.y.ToString("0.###", CultureInfo.InvariantCulture)).Append(", ")
                    .Append(created[i].localPosition.z.ToString("0.###", CultureInfo.InvariantCulture)).Append(")");
            }

            var colliderHost = staticPart != null ? staticPart : visual.transform;
            if (staticBoxes != null)
            {
                for (var i = 0; i < staticBoxes.Length; i++)
                {
                    var box = colliderHost.gameObject.AddComponent<BoxCollider>();
                    box.center = staticBoxes[i].Center;
                    box.size = staticBoxes[i].Size;
                }
            }
            else if (colliderNodes.Count > 0)
            {
                // Expansion models carry collider boxes as metadata on empty nodes under Static.
                foreach (var pair in colliderNodes)
                {
                    var box = pair.Key.AddComponent<BoxCollider>();
                    box.size = pair.Value;
                }
            }
            else
            {
                var bounds = LocalBounds(prefab.transform, visual);
                var box = colliderHost.gameObject.AddComponent<BoxCollider>();
                box.center = colliderHost.InverseTransformPoint(prefab.transform.TransformPoint(bounds.center));
                box.size = bounds.size;
            }

            if (renderer != null)
                renderer.enabled = false;
            var rootCollider = prefab.GetComponent<Collider>();
            if (rootCollider != null)
                rootCollider.enabled = false;

            report = Path.GetFileName(path) + markerReport + notes;
            return true;
        }

        private static void AddMesh(GameObject owner, object meshValue, List<object> accessors, List<object> views, byte[] bin, Material[] colors)
        {
            var mesh = (Dictionary<string, object>)meshValue;
            var primitives = (List<object>)mesh["primitives"];
            for (var p = 0; p < primitives.Count; p++)
            {
                var primitive = (Dictionary<string, object>)primitives[p];
                var mode = primitive.ContainsKey("mode") ? Convert.ToInt32(primitive["mode"]) : 4;
                if (mode != 4)
                    continue;
                var attributes = (Dictionary<string, object>)primitive["attributes"];
                var positions = ReadVec3(accessors, views, bin, Convert.ToInt32(attributes["POSITION"]));
                var normals = attributes.ContainsKey("NORMAL") ? ReadVec3(accessors, views, bin, Convert.ToInt32(attributes["NORMAL"])) : null;
                var uvs = attributes.ContainsKey("TEXCOORD_0") ? ReadVec2(accessors, views, bin, Convert.ToInt32(attributes["TEXCOORD_0"])) : null;
                int[] triangles;
                if (primitive.ContainsKey("indices"))
                {
                    var indices = ReadIndices(accessors, views, bin, Convert.ToInt32(primitive["indices"]));
                    triangles = new int[indices.Length];
                    for (var v = 0; v + 2 < indices.Length; v += 3)
                    {
                        triangles[v] = indices[v];
                        triangles[v + 1] = indices[v + 2];
                        triangles[v + 2] = indices[v + 1];
                    }
                }
                else
                {
                    triangles = new int[positions.Length];
                    for (var v = 0; v + 2 < positions.Length; v += 3)
                    {
                        triangles[v] = v;
                        triangles[v + 1] = v + 2;
                        triangles[v + 2] = v + 1;
                    }
                }

                var unityMesh = new Mesh();
                unityMesh.name = owner.name + "_" + p;
                unityMesh.vertices = positions;
                if (normals != null && normals.Length == positions.Length)
                    unityMesh.normals = normals;
                if (uvs != null && uvs.Length == positions.Length)
                    unityMesh.uv = uvs;
                unityMesh.triangles = triangles;
                unityMesh.RecalculateBounds();
                if (normals == null)
                    unityMesh.RecalculateNormals();

                var child = new GameObject("surface_" + p);
                child.transform.SetParent(owner.transform, false);
                var filter = child.AddComponent<MeshFilter>();
                filter.sharedMesh = unityMesh;
                var draw = child.AddComponent<MeshRenderer>();
                var materialIndex = primitive.ContainsKey("material") ? Convert.ToInt32(primitive["material"]) : -1;
                draw.sharedMaterial = materialIndex >= 0 && materialIndex < colors.Length ? colors[materialIndex] : colors[0];
            }
        }

        private static Vector3[] ReadVec3(List<object> accessors, List<object> views, byte[] bin, int accessorIndex)
        {
            var accessor = (Dictionary<string, object>)accessors[accessorIndex];
            var view = (Dictionary<string, object>)views[Convert.ToInt32(accessor["bufferView"])];
            var offset = view.ContainsKey("byteOffset") ? Convert.ToInt32(view["byteOffset"]) : 0;
            if (accessor.ContainsKey("byteOffset"))
                offset += Convert.ToInt32(accessor["byteOffset"]);
            var count = Convert.ToInt32(accessor["count"]);
            var result = new Vector3[count];
            for (var i = 0; i < count; i++)
            {
                var at = offset + i * 12;
                result[i] = new Vector3(
                    BitConverter.ToSingle(bin, at),
                    BitConverter.ToSingle(bin, at + 4),
                    -BitConverter.ToSingle(bin, at + 8));
            }

            return result;
        }

        private static Vector2[] ReadVec2(List<object> accessors, List<object> views, byte[] bin, int accessorIndex)
        {
            var accessor = (Dictionary<string, object>)accessors[accessorIndex];
            var view = (Dictionary<string, object>)views[Convert.ToInt32(accessor["bufferView"])];
            var offset = view.ContainsKey("byteOffset") ? Convert.ToInt32(view["byteOffset"]) : 0;
            if (accessor.ContainsKey("byteOffset"))
                offset += Convert.ToInt32(accessor["byteOffset"]);
            var count = Convert.ToInt32(accessor["count"]);
            var result = new Vector2[count];
            for (var i = 0; i < count; i++)
            {
                var at = offset + i * 8;
                result[i] = new Vector2(BitConverter.ToSingle(bin, at), BitConverter.ToSingle(bin, at + 4));
            }

            return result;
        }

        private static int[] ReadIndices(List<object> accessors, List<object> views, byte[] bin, int accessorIndex)
        {
            var accessor = (Dictionary<string, object>)accessors[accessorIndex];
            var view = (Dictionary<string, object>)views[Convert.ToInt32(accessor["bufferView"])];
            var offset = view.ContainsKey("byteOffset") ? Convert.ToInt32(view["byteOffset"]) : 0;
            if (accessor.ContainsKey("byteOffset"))
                offset += Convert.ToInt32(accessor["byteOffset"]);
            var count = Convert.ToInt32(accessor["count"]);
            var component = Convert.ToInt32(accessor["componentType"]);
            var stride = component == 5125 ? 4 : component == 5123 ? 2 : 1;
            if (view.ContainsKey("byteStride"))
                stride = Convert.ToInt32(view["byteStride"]);
            var result = new int[count];
            for (var i = 0; i < count; i++)
            {
                var at = offset + i * stride;
                if (component == 5125)
                    result[i] = BitConverter.ToInt32(bin, at);
                else if (component == 5123)
                    result[i] = BitConverter.ToUInt16(bin, at);
                else
                    result[i] = bin[at];
            }

            return result;
        }

        private static Vector3 Flip(List<object> values)
        {
            return new Vector3(Num(values[0]), Num(values[1]), -Num(values[2]));
        }

        private static Color ReadColor(List<object> values)
        {
            return new Color(Num(values[0]), Num(values[1]), Num(values[2]), values.Count > 3 ? Num(values[3]) : 1f);
        }

        private static float Num(object value)
        {
            return Convert.ToSingle(value, CultureInfo.InvariantCulture);
        }

        private static Bounds LocalBounds(Transform root, GameObject visual)
        {
            var filters = visual.GetComponentsInChildren<MeshFilter>(true);
            var has = false;
            var bounds = new Bounds(Vector3.zero, Vector3.one * 0.5f);
            foreach (var filter in filters)
            {
                if (filter.sharedMesh == null)
                    continue;
                var mesh = filter.sharedMesh.bounds;
                for (var i = 0; i < 8; i++)
                {
                    var corner = mesh.center + Vector3.Scale(mesh.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var local = root.InverseTransformPoint(filter.transform.TransformPoint(corner));
                    if (!has)
                    {
                        bounds = new Bounds(local, Vector3.zero);
                        has = true;
                    }
                    else
                        bounds.Encapsulate(local);
                }
            }

            return bounds;
        }

        private static List<object> ExtrasList(Dictionary<string, object> node, string key)
        {
            if (!node.ContainsKey("extras"))
                return null;
            var extras = node["extras"] as Dictionary<string, object>;
            return extras != null && extras.ContainsKey(key) ? extras[key] as List<object> : null;
        }

        private static string Extras(Dictionary<string, object> node, string key)
        {
            if (!node.ContainsKey("extras"))
                return "";
            var extras = (Dictionary<string, object>)node["extras"];
            return extras.ContainsKey(key) ? Convert.ToString(extras[key]) : "";
        }

        private static Vector3 Axis(string name)
        {
            if (name == "x")
                return Vector3.right;
            if (name == "y")
                return Vector3.up;
            return Vector3.forward;
        }
    }

    public class SpinningPart : MonoBehaviour
    {
        public Vector3 LocalAxis = Vector3.forward;
        public bool Swing;
        private Machines.WorkshopMachine _machine;
        private float _phase;

        private void Update()
        {
            if (_machine == null)
                _machine = GetComponentInParent<Machines.WorkshopMachine>();
            if (_machine == null || !_machine.Turning)
                return;
            if (Swing)
            {
                _phase += Time.deltaTime * 1.6f * (float)(_machine.VisualRpm / BalanceDefaults.MilestoneRpm);
                transform.localRotation = Quaternion.AngleAxis(Mathf.Sin(_phase) * 50f, LocalAxis);
                return;
            }

            // Degrees per second = RPM × 6. The sign shows which way this segment turns.
            transform.Rotate(LocalAxis, (float)_machine.VisualSpeed * 6f * Time.deltaTime, Space.Self);
        }
    }

    /// <summary>
    /// Scrolls a belt's own property block. The shared native material is not changed.
    /// </summary>
    public class ScrollingPart : MonoBehaviour
    {
        private Renderer[] _renderers;
        private MaterialPropertyBlock _block;
        private float _offset;

        private void Awake()
        {
            _renderers = GetComponentsInChildren<Renderer>();
            _block = new MaterialPropertyBlock();
        }

        private void Update()
        {
            var machine = GetComponentInParent<Machines.WorkshopMachine>();
            if (machine == null || !machine.Turning || _renderers == null)
                return;
            _offset = Mathf.Repeat(_offset + Time.deltaTime * 0.35f * Mathf.Sign((float)machine.VisualSpeed), 1f);
            for (var i = 0; i < _renderers.Length; i++)
            {
                var renderer = _renderers[i];
                if (renderer == null)
                    continue;
                renderer.GetPropertyBlock(_block);
                _block.SetVector("_MainTex_ST", new Vector4(1f, 1f, _offset, 0f));
                renderer.SetPropertyBlock(_block);
            }
        }
    }

    internal static class JsonValue
    {
        public static object Parse(string text)
        {
            var parser = new Parser(text);
            return parser.Read();
        }

        private sealed class Parser
        {
            private readonly string _text;
            private int _index;

            public Parser(string text)
            {
                _text = text;
            }

            public object Read()
            {
                Skip();
                var current = _text[_index];
                if (current == '{')
                    return ReadObject();
                if (current == '[')
                    return ReadArray();
                if (current == '"')
                    return ReadString();
                if (current == 't' || current == 'f')
                    return ReadBool();
                if (current == 'n')
                {
                    _index += 4;
                    return null;
                }

                return ReadNumber();
            }

            private Dictionary<string, object> ReadObject()
            {
                var result = new Dictionary<string, object>();
                _index++;
                Skip();
                if (_text[_index] == '}')
                {
                    _index++;
                    return result;
                }

                while (true)
                {
                    Skip();
                    var key = ReadString();
                    Skip();
                    _index++;
                    result[key] = Read();
                    Skip();
                    if (_text[_index] == '}')
                    {
                        _index++;
                        return result;
                    }

                    _index++;
                }
            }

            private List<object> ReadArray()
            {
                var result = new List<object>();
                _index++;
                Skip();
                if (_text[_index] == ']')
                {
                    _index++;
                    return result;
                }

                while (true)
                {
                    result.Add(Read());
                    Skip();
                    if (_text[_index] == ']')
                    {
                        _index++;
                        return result;
                    }

                    _index++;
                }
            }

            private string ReadString()
            {
                _index++;
                var builder = new StringBuilder();
                while (_text[_index] != '"')
                {
                    var current = _text[_index++];
                    if (current != '\\')
                    {
                        builder.Append(current);
                        continue;
                    }

                    var escaped = _text[_index++];
                    if (escaped == 'n')
                        builder.Append('\n');
                    else if (escaped == 'r')
                        builder.Append('\r');
                    else if (escaped == 't')
                        builder.Append('\t');
                    else
                        builder.Append(escaped);
                }

                _index++;
                return builder.ToString();
            }

            private bool ReadBool()
            {
                if (_text[_index] == 't')
                {
                    _index += 4;
                    return true;
                }

                _index += 5;
                return false;
            }

            private double ReadNumber()
            {
                var start = _index;
                if (_text[_index] == '-')
                    _index++;
                while (_index < _text.Length && ("0123456789.eE+-".IndexOf(_text[_index]) >= 0))
                    _index++;
                return double.Parse(_text.Substring(start, _index - start), CultureInfo.InvariantCulture);
            }

            private void Skip()
            {
                while (_index < _text.Length && char.IsWhiteSpace(_text[_index]))
                    _index++;
            }
        }
    }
}
