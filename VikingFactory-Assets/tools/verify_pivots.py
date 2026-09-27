"""Print Blender-space pivots for the two prototype machines. Run with Blender --background --python."""
import bpy
from pathlib import Path
root = Path(__file__).resolve().parents[1] / "converted"
for name in ("vf_water_wheel", "vf_shaft_2m"):
    bpy.ops.wm.open_mainfile(filepath=str(root / (name + ".blend")))
    print("BLEND", name)
    for obj in bpy.context.scene.objects:
        parent = obj.parent.name if obj.parent else "-"
        loc = tuple(round(v, 4) for v in obj.location)
        print(f"  {obj.type} {obj.name} parent={parent} loc={loc}")
