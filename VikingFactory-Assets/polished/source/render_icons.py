"""Renders a square picture of each hammer piece from its polished model."""
import bpy
from mathutils import Vector
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "icons"
EXPANSION = ROOT.parent / "expansion" / "models"
# Placed piece id -> model name. Found under polished/ first, then expansion/models/.
MODELS = {
    "vf_crank": "vf_hand_crank",
    "vf_shaft": "vf_shaft_2m",
    "vf_rope": "vf_rope_4m",
    "vf_cog": "vf_cog",
    "vf_reversing_cog": "vf_reversing_cog",
    "vf_clutch": "vf_clutch",
    "vf_water_wheel": "vf_water_wheel",
    "vf_belt": "vf_conveyor_2m",
    "vf_belt_corner": "vf_conveyor_corner",
    "vf_iron_belt": "vf_iron_belt_2m",
    "vf_trough": "vf_gravity_trough",
    "vf_feeder": "vf_feeder",
    "vf_iron_feeder": "vf_iron_feeder",
    "vf_basket": "vf_catch_basket",
    "vf_splitter": "vf_splitter",
    "vf_merger": "vf_merger",
    "vf_recipe_mill": "vf_recipe_mill",
    "vf_assembler": "vf_advanced_assembler",
    "vf_quarry": "vf_quarry",
    "vf_coppice_bed": "vf_coppice_bed",
    "vf_timber_saw": "vf_timber_saw",
    "vf_planter": "vf_planter",
    "vf_harvester": "vf_harvester",
    "vf_farm_gantry": "vf_farm_gantry",
    "vf_forage_bed": "vf_forage_bed",
    "vf_mining_head": "vf_finite_mining_head",
    "vf_deep_extractor": "vf_deep_extractor",
    "vf_steam_engine": "vf_steam_engine",
    "vf_reinforced_steam": "vf_reinforced_steam_engine",
    "vf_water_intake": "vf_water_intake",
    "vf_sail_wheel": "vf_sail_wheel",
    "vf_governor": "vf_governor",
    "vf_flywheel": "vf_flywheel",
    "vf_eitr_motor": "vf_eitr_motor",
    "vf_cooking_tender": "vf_cooking_tender",
    "vf_oven_extension": "vf_oven_extension",
    "vf_feed_gate": "vf_livestock_feed_gate",
    "vf_culling_gate": "vf_livestock_culling_gate",
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
    import sys
    redo = "--all" in sys.argv
    if redo or not (OUT / "vf_marker.png").is_file():
        render_marker(OUT / "vf_marker.png")
    for name, model in MODELS.items():
        target = OUT / (name + ".png")
        if target.is_file() and not redo:
            continue
        source = ROOT / model / (model + ".glb")
        if not source.is_file():
            source = EXPANSION / model / (model + ".glb")
        if not source.is_file():
            raise SystemExit("Missing model: " + model)
        render_model(target, source)
        print("rendered", name)


if __name__ == "__main__":
    main()
