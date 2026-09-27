"""Run in a NEW Blender process, not an unsaved working scene:
blender --background --python tools/convert_in_blender.py -- /absolute/path/VikingFactory-Assets
Imports generated GLBs, creates .blend sources + FBX exports. No extra Python modules.
Not executed in the generation environment (Blender was unavailable).
"""
import bpy, sys, json
from pathlib import Path
args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
root=Path(args[0]).resolve() if args else Path(__file__).resolve().parents[1]
out=root/'converted';out.mkdir(exist_ok=True)
report=[]
for glb in sorted((root/'models').glob('*/*.glb')):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(glb))
    mesh_objects=[o for o in bpy.context.scene.objects if o.type=='MESH']
    if not mesh_objects:raise RuntimeError('No meshes imported: '+str(glb))
    # Keep hierarchy/pivots; create usable UV unwrap for later texture painting.
    # The GLBs already contain placeholder face UVs, overwritten here with an unwrap.
    for obj in mesh_objects:
        bpy.ops.object.select_all(action='DESELECT')
        obj.select_set(True);bpy.context.view_layer.objects.active=obj
        bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT')
        bpy.ops.uv.smart_project(island_margin=0.02)
        bpy.ops.object.mode_set(mode='OBJECT')
    bpy.ops.object.select_all(action='SELECT')
    # Blender 5.1.1's FBX exporter crashes on vector custom properties (glTF extras such as
    # marker forward). Keep the values, stored as JSON text, so the export can finish.
    for obj in list(bpy.context.scene.objects):
        for key in list(obj.keys()):
            value = obj[key]
            if isinstance(value, (int, float, str, bool)):
                continue
            try:
                obj[key] = ",".join(str(float(item)) for item in value)
            except TypeError:
                del obj[key]
    blend=out/(glb.stem+'.blend');fbx=out/(glb.stem+'.fbx')
    bpy.ops.wm.save_as_mainfile(filepath=str(blend))
    bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,object_types={'MESH','EMPTY'},
        global_scale=1.0,apply_unit_scale=True,axis_forward='-Z',axis_up='Y',
        add_leaf_bones=False,bake_anim=False,use_custom_props=True,path_mode='AUTO')
    report.append({'asset':glb.stem,'meshes':len(mesh_objects),'blend':blend.name,'fbx':fbx.name})
(root/'converted'/'conversion-report.json').write_text(json.dumps(report,indent=2))
print('Converted',len(report),'models. Check imported scale, axes and materials in Unity.')
