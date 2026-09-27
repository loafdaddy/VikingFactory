"""Shared mesh helpers for the polished pack. Coordinates are Y-up metres."""
import bpy
import bmesh
from math import cos, sin, pi
from mathutils import Matrix, Vector
from pathlib import Path

TILE = 0.5
OUT = Path(__file__).resolve().parents[1]

def G(x, y, z):
    return Vector((x, -z, y))

def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)

def material(name, color):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    node = mat.node_tree.nodes.get("Principled BSDF")
    if node:
        node.inputs["Base Color"].default_value = color
        node.inputs["Roughness"].default_value = 0.85
        node.inputs["Metallic"].default_value = 0.0
    return mat

def uv_face(face, grain, layer):
    normal = face.normal.normalized()
    direction = grain.normalized()
    if direction.length < 1e-5:
        direction = Vector((0, 1, 0))
    if abs(normal.dot(direction)) > 0.85:
        side = direction.cross(Vector((0, 1, 0)))
        if side.length < 0.2:
            side = direction.cross(Vector((1, 0, 0)))
        side.normalize()
        other = direction.cross(side)
        axes = (side, other)
    else:
        side = normal.cross(direction)
        if side.length < 1e-5:
            side = Vector((1, 0, 0))
        side.normalize()
        axes = (direction, side)
    for loop in face.loops:
        co = loop.vert.co
        loop[layer].uv = (co.dot(axes[0]) / TILE, co.dot(axes[1]) / TILE)

def finish(bm, grain):
    layer = bm.loops.layers.uv.new("UVMap")
    for face in bm.faces:
        uv_face(face, grain, layer)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)

def _mesh(name, bm, mat):
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    obj.data.materials.append(mat)
    bpy.context.collection.objects.link(obj)
    return obj

def box(name, center, size, grain, mat):
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    sx, sy, sz = size
    cx, cy, cz = center
    for vert in bm.verts:
        vert.co.x = vert.co.x * sx + cx
        vert.co.y = vert.co.y * sy + cy
        vert.co.z = vert.co.z * sz + cz
    finish(bm, Vector(grain))
    for vert in bm.verts:
        vert.co = G(vert.co.x, vert.co.y, vert.co.z)
    return _mesh(name, bm, mat)

def beam(name, a, b, width, depth, mat):
    start = Vector(a)
    end = Vector(b)
    direction = end - start
    length = direction.length
    if length < 1e-5:
        raise ValueError(name)
    y = direction.normalized()
    ref = Vector((1, 0, 0)) if abs(y.x) < 0.9 else Vector((0, 0, 1))
    z = ref.cross(y).normalized()
    x = y.cross(z)
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    for vert in bm.verts:
        local = Vector((vert.co.x * width, vert.co.y * length, vert.co.z * depth))
        vert.co = start.lerp(end, 0.5) + x * local.x + y * local.y + z * local.z
    finish(bm, y)
    for vert in bm.verts:
        vert.co = G(vert.co.x, vert.co.y, vert.co.z)
    return _mesh(name, bm, mat)

def oriented(name, center, size, x_axis, y_axis, mat):
    x = Vector(x_axis).normalized()
    y = Vector(y_axis).normalized()
    z = x.cross(y).normalized()
    y = z.cross(x)
    origin = Vector(center)
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    for vert in bm.verts:
        local = Vector((vert.co.x * size[0], vert.co.y * size[1], vert.co.z * size[2]))
        vert.co = origin + x * local.x + y * local.y + z * local.z
    finish(bm, y)
    for vert in bm.verts:
        vert.co = G(vert.co.x, vert.co.y, vert.co.z)
    return _mesh(name, bm, mat)

def cylinder(name, center, radius, length, axis, mat, n=10):
    bm = bmesh.new()
    verts = []
    for end in (-length / 2, length / 2):
        for i in range(n):
            a = 2 * pi * i / n
            p = [center[0], center[1], center[2]]
            if axis == "x":
                p[0] += end
                p[1] += radius * cos(a)
                p[2] += radius * sin(a)
            elif axis == "y":
                p[1] += end
                p[0] += radius * cos(a)
                p[2] += radius * sin(a)
            else:
                p[2] += end
                p[0] += radius * cos(a)
                p[1] += radius * sin(a)
            verts.append(bm.verts.new(p))
    bm.verts.ensure_lookup_table()
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new((verts[i], verts[j], verts[n + j], verts[n + i]))
    bm.faces.new([verts[i] for i in range(n)])
    bm.faces.new([verts[n + i] for i in range(n - 1, -1, -1)])
    grain = {"x": (1, 0, 0), "y": (0, 1, 0), "z": (0, 0, 1)}[axis]
    finish(bm, Vector(grain))
    for vert in bm.verts:
        vert.co = G(vert.co.x, vert.co.y, vert.co.z)
    return _mesh(name, bm, mat)

def empty(name, location):
    obj = bpy.data.objects.new(name, None)
    obj.empty_display_size = 0.12
    obj.location = G(*location)
    bpy.context.collection.objects.link(obj)
    return obj

def parent(child, parent_obj):
    child.parent = parent_obj
    child.matrix_parent_inverse = parent_obj.matrix_world.inverted()

def parent_local(child, parent_obj):
    child.parent = parent_obj
    child.matrix_parent_inverse = Matrix.Identity(4)
    child.matrix_basis = Matrix.Identity(4)

def join_children(parent_obj):
    groups = {}
    for child in list(parent_obj.children):
        if child.type != "MESH" or not child.data.materials:
            continue
        groups.setdefault(child.data.materials[0].name, []).append(child)
    for objs in groups.values():
        bpy.ops.object.select_all(action="DESELECT")
        for obj in objs:
            obj.select_set(True)
        bpy.context.view_layer.objects.active = objs[0]
        if len(objs) > 1:
            bpy.ops.object.join()
        objs[0].name = parent_obj.name + "_" + objs[0].data.materials[0].name

class Scene:
    def __init__(self, asset_id):
        reset()
        self.asset_id = asset_id
        self.oak = material("Oak", (0.32, 0.17, 0.075, 1))
        self.oak_light = material("OakLight", (0.45, 0.28, 0.14, 1))
        self.iron = material("Iron", (0.16, 0.16, 0.15, 1))
        self.bronze = material("Bronze", (0.45, 0.28, 0.12, 1))
        self.hide = material("Hide", (0.35, 0.22, 0.12, 1))
        self.stone = material("Stone", (0.34, 0.33, 0.30, 1))
        self.root = empty(asset_id, (0, 0, 0))
        self.root["material_profile"] = "valheim"
        self.groups = {}

    def group(self, name, location, axis=None, motion=None, scroll=None):
        obj = empty(name, location)
        if axis:
            obj["rotation_axis"] = axis
        if motion:
            obj["motion"] = motion
        if scroll:
            obj["scroll"] = scroll
        parent(obj, self.root)
        self.groups[name] = obj
        return obj

    def marker(self, name, location):
        parent(empty(name, location), self.root)

    def add(self, group_name, obj):
        parent_local(obj, self.groups[group_name])

    def finish(self):
        for obj in self.groups.values():
            join_children(obj)
        folder = OUT / self.asset_id
        folder.mkdir(parents=True, exist_ok=True)
        bpy.ops.wm.save_as_mainfile(filepath=str(folder / (self.asset_id + ".blend")))
        bpy.ops.export_scene.gltf(
            filepath=str(folder / (self.asset_id + ".glb")),
            export_format="GLB",
            use_selection=False,
            export_yup=True,
            export_apply=False,
            export_extras=True,
        )
        bpy.ops.export_scene.fbx(
            filepath=str(folder / (self.asset_id + ".fbx")),
            use_selection=False,
            object_types={"MESH", "EMPTY"},
            global_scale=1.0,
            apply_unit_scale=True,
            axis_forward="-Z",
            axis_up="Y",
            add_leaf_bones=False,
            bake_anim=False,
            use_custom_props=False,
            path_mode="AUTO",
        )
        print("Wrote", folder / (self.asset_id + ".glb"))
