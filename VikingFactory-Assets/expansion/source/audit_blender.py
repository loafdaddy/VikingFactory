"""Import every expansion GLB in Blender and measure size, UVs, and materials."""
import bpy
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "blender-audit.json"
ALLOWED = {"Oak", "OakLight", "Iron", "Bronze", "Hide", "Stone"}
TARGET = 0.5


def plain(value):
    if isinstance(value, (str, int, float, bool)) or value is None:
        return value
    if hasattr(value, "keys"):
        return {str(k): plain(value[k]) for k in value.keys()}
    try:
        return [plain(v) for v in list(value)]
    except TypeError:
        return str(value)


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def edge_ratios(mesh):
    uv_layer = mesh.uv_layers.active
    if uv_layer is None or len(mesh.polygons) == 0:
        return None
    ratios = []
    stretched = 0
    mesh.calc_loop_triangles()
    uv = uv_layer.data
    for poly in mesh.polygons:
        loops = poly.loop_indices
        samples = []
        count = len(loops)
        for i in range(count):
            a = loops[i]
            b = loops[(i + 1) % count]
            va = mesh.vertices[mesh.loops[a].vertex_index].co
            vb = mesh.vertices[mesh.loops[b].vertex_index].co
            world = (va - vb).length
            uva = uv[a].uv
            uvb = uv[b].uv
            span = (uva - uvb).length
            if world < 0.02 or span < 1e-6:
                continue
            ratio = world / span
            samples.append(ratio)
            ratios.append(ratio)
        if len(samples) >= 2:
            lo = min(samples)
            hi = max(samples)
            if lo > 1e-6 and hi / lo > 3.0:
                stretched += 1
    return ratios, stretched, len(mesh.polygons)


def summarize(values):
    if not values:
        return None
    ordered = sorted(values)
    def pick(p):
        return ordered[min(len(ordered) - 1, int(p * (len(ordered) - 1)))]
    return {
        "edges": len(ordered),
        "p10": round(pick(0.10), 3),
        "median": round(pick(0.50), 3),
        "p90": round(pick(0.90), 3),
    }


def audit(path):
    reset()
    result = {"id": path.stem, "opened": False}
    try:
        bpy.ops.import_scene.gltf(filepath=str(path))
    except Exception as error:
        result["error"] = str(error)
        return result
    result["opened"] = True
    result["images"] = len(bpy.data.images)
    mats = sorted({m.name.split(".")[0] for m in bpy.data.materials if m.users})
    result["materials"] = mats
    result["material_ok"] = set(mats).issubset(ALLOWED) and len(mats) > 0
    mins = [1e9, 1e9, 1e9]
    maxs = [-1e9, -1e9, -1e9]
    scales = []
    extras = {}
    by_mat = {}
    stretched = 0
    faces = 0
    missing_uv = []
    for obj in bpy.data.objects:
        if obj.type == "EMPTY":
            keys = [k for k in obj.keys() if not str(k).startswith("_")]
            if keys:
                extras[obj.name] = {k: plain(obj[k]) for k in keys}
        if obj.type != "MESH":
            continue
        for i in range(3):
            scales.append(round(obj.scale[i], 4))
        for corner in obj.bound_box:
            world = obj.matrix_world @ __import__("mathutils").Vector(corner)
            for i in range(3):
                mins[i] = min(mins[i], world[i])
                maxs[i] = max(maxs[i], world[i])
        if obj.data.uv_layers.active is None:
            missing_uv.append(obj.name)
            continue
        ratios, stretch, poly_count = edge_ratios(obj.data)
        stretched += stretch
        faces += poly_count
        name = obj.material_slots[0].material.name.split(".")[0] if obj.material_slots else "?"
        by_mat.setdefault(name, []).extend(ratios or [])
    # Blender is Z-up: glTF (x, y, z) -> (x, -z, y), so height is Blender Z.
    size = [round(maxs[i] - mins[i], 3) for i in range(3)]
    result["blender_size_xyz"] = size
    result["gltf_size_xyz"] = [size[0], size[2], size[1]]
    result["feet_z"] = round(mins[2], 4)
    result["non_unit_scale"] = any(abs(s - 1) > 0.001 for s in scales)
    result["missing_uv"] = missing_uv
    result["stretched_faces"] = stretched
    result["faces"] = faces
    result["uv_metres"] = {name: summarize(values) for name, values in sorted(by_mat.items())}
    result["extras_present"] = "material_profile" in json.dumps(extras)
    result["profile"] = extras.get(path.stem, {}).get("material_profile")
    return result


def main():
    rows = []
    for folder in sorted((ROOT / "models").iterdir()):
        glb = folder / f"{folder.name}.glb"
        if glb.exists():
            rows.append(audit(glb))
            print(rows[-1]["id"], "opened" if rows[-1].get("opened") else rows[-1].get("error"))
    combined = ROOT / "VikingFactory-Expansion-All.glb"
    rows.append(audit(combined))
    print("combined", "opened" if rows[-1].get("opened") else rows[-1].get("error"))
    OUT.write_text(json.dumps(rows, indent=2))
    print("wrote", OUT)


if __name__ == "__main__":
    main()
