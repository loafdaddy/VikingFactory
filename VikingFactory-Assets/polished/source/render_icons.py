"""Renders a square picture of each hammer piece from its polished model."""
import bpy
from mathutils import Vector
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "icons"
MODELS = {
    "vf_crank": ROOT / "vf_hand_crank" / "vf_hand_crank.glb",
    "vf_shaft": ROOT / "vf_shaft_2m" / "vf_shaft_2m.glb",
    "vf_clutch": ROOT / "vf_clutch" / "vf_clutch.glb",
    "vf_water_wheel": ROOT / "vf_water_wheel" / "vf_water_wheel.glb",
    "vf_belt": ROOT / "vf_conveyor_2m" / "vf_conveyor_2m.glb",
    "vf_feeder": ROOT / "vf_feeder" / "vf_feeder.glb",
    "vf_basket": ROOT / "vf_catch_basket" / "vf_catch_basket.glb",
}


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def paint_viewport_colors():
    for mat in bpy.data.materials:
        if not mat.use_nodes or mat.node_tree is None:
            continue
        for node in mat.node_tree.nodes:
            if node.type != "BSDF_PRINCIPLED":
                continue
            color = node.inputs["Base Color"].default_value
            mat.diffuse_color = color


def mesh_bounds():
    low = Vector((1e9, 1e9, 1e9))
    high = Vector((-1e9, -1e9, -1e9))
    found = False
    for obj in bpy.context.scene.objects:
        if obj.type != "MESH":
            continue
        found = True
        for corner in obj.bound_box:
            world = obj.matrix_world @ Vector(corner)
            low.x = min(low.x, world.x)
            low.y = min(low.y, world.y)
            low.z = min(low.z, world.z)
            high.x = max(high.x, world.x)
            high.y = max(high.y, world.y)
            high.z = max(high.z, world.z)
    if not found:
        raise RuntimeError("The imported model has no mesh.")
    return low, high


def frame(low, high):
    center = (low + high) * 0.5
    direction = Vector((1.15, -1.35, 0.82)).normalized()
    scene = bpy.context.scene
    camera_data = bpy.data.cameras.new("icon")
    camera_data.type = "ORTHO"
    camera = bpy.data.objects.new("icon", camera_data)
    scene.collection.objects.link(camera)
    scene.camera = camera
    camera.location = center + direction * 10
    camera.rotation_euler = (center - camera.location).to_track_quat("-Z", "Y").to_euler()

    inverse = camera.matrix_world.inverted()
    xs = []
    ys = []
    for x in (low.x, high.x):
        for y in (low.y, high.y):
            for z in (low.z, high.z):
                point = inverse @ Vector((x, y, z))
                xs.append(point.x)
                ys.append(point.y)
    camera_data.ortho_scale = max(max(xs) - min(xs), max(ys) - min(ys)) * 1.22


def render(path):
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.light = "STUDIO"
    scene.display.shading.color_type = "MATERIAL"
    scene.display.shading.show_shadows = True
    scene.render.film_transparent = True
    scene.render.resolution_x = 256
    scene.render.resolution_y = 256
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)


def render_marker(path):
    reset()
    bpy.ops.mesh.primitive_cube_add(size=0.6, location=(0, 0, 0.3))
    mat = bpy.data.materials.new("Oak")
    mat.diffuse_color = (0.45, 0.30, 0.16, 1)
    bpy.context.object.data.materials.append(mat)
    low, high = mesh_bounds()
    frame(low, high)
    render(path)


def render_model(path, source):
    reset()
    bpy.ops.import_scene.gltf(filepath=str(source))
    paint_viewport_colors()
    low, high = mesh_bounds()
    frame(low, high)
    render(path)


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    render_marker(OUT / "vf_marker.png")
    for name, source in MODELS.items():
        if not source.is_file():
            raise SystemExit("Missing model: " + str(source))
        render_model(OUT / (name + ".png"), source)
        print("rendered", name)


if __name__ == "__main__":
    main()
