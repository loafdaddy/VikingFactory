# VikingFactory Workshop Asset Kit — prototype 0.1

The notes below describe the original prototype export. Later work lives elsewhere and should be preferred:

- `polished/` has the models the plugin loads for the early machines, including the cog, corner, splitter, trough, recipe mill, and quarry. See `polished/STATUS.md`.
- `expansion/` has the later machines. `0.3.0` registers them as hammer pieces, except the fishing winch. A windowed session placed several of them. See `expansion/START-HERE.md`.
- `converted/` is a Blender conversion of these prototypes. The plugin does not load it.

The prototype GLBs remain the baseline. Do not overwrite them. The sentence further down about Blender being unavailable describes this original export only.

Twelve original low-poly 3D assets generated for the VikingFactory project.

This folder is an editable prototype art pack, not an installable Valheim mod, finished Unity prefabs, or compiled AssetBundles. `CURSOR-HANDOFF.md` was an agent brief for that import. It is not part of the public docs.

## Included

| Model | Moving group(s) | Intended use |
|---|---|---|
| vf_shaft_2m | Rotor | 2 m transmission shaft with bearings |
| vf_cog | Rotor | Wooden cog with bronze hub and stand |
| vf_hand_crank | Crank | Manual drive source |
| vf_water_wheel | Rotor | Large timber wheel and fixed frame |
| vf_conveyor_2m | Belt_Surface, rollers | Straight 2 m transport module |
| vf_conveyor_corner | None | Quarter-turn transport deck |
| vf_splitter | Selector | One input / three output junction |
| vf_feeder | Arm_Yaw | Pivoting bronze/timber handling arm |
| vf_catch_basket | None | Open collection/storage basket |
| vf_gravity_trough | None | Sloping gravity chute |
| vf_recipe_mill | Millstone | Basic workshop processing machine |
| vf_quarry | Drill | Bedrock drilling machine |

All pieces use a shared six-material palette: oak, lighter oak, iron, bronze, hide and stone. Surfaces are flat-color materials, not finished painted wood/metal textures. GLB face UVs are placeholders; the Blender conversion script can generate a basic smart unwrap for a later texture pass.

## Formats

Each `models/<id>/` directory contains:

- `<id>.glb`: preferred editable interchange file, preserving named moving groups, their pivots, material assignments and marker empties.
- `<id>.obj` and `materials.mtl`: static fallback geometry/materials. OBJ does not preserve the animation pivots, empty markers or parent hierarchy; do not use it as the animated master.
- `asset.json`: bounds, triangle count, part pivots, port directions and suggested collision boxes.

`catalog.json` collects all metadata. `source/generate_assets.py` rebuilds geometry from primitives without any extracted game artwork. `tools/validate_pack.py` checks the generated files. `tools/convert_in_blender.py` imports GLBs and exports `.blend` and `.fbx` files locally.

**No FBX or BLEND files are pre-generated:** Blender was unavailable in the creation environment. The conversion script is supplied but has not been executed here. Unity and Valheim were also unavailable.

## Open/edit in Blender

Use File → Import → glTF 2.0 and select a GLB. Inspect the hierarchy: the machine root contains Static, moving parts and named marker empties. The wheel rotates around its Rotor group's local X axis in the original Y-up coordinate system; Blender's importer converts the coordinate system.

For batch conversion, run in a fresh Blender process (do not run in an unsaved Blender session):

```sh
blender --background --python tools/convert_in_blender.py -- "/absolute/path/to/VikingFactory-Assets"
```

This writes files to `converted/`. It resets the Blender scene for every asset, imports the GLB, unwraps each mesh, saves a BLEND and exports FBX. Check the results in your installed Blender version. If FBX export is unavailable, install/enable the relevant Blender exporter or save the imported BLEND and export manually.

## Unity route

Recommended: GLB → Blender → FBX → your Unity asset project. Alternatively, use a verified glTF importer appropriate to the project. Do not assume every Unity project natively imports `.glb`.

Use the editor version verified for your installed Valheim build. Import the models, create real prefabs, assign suitable materials/shaders, configure collision, connect native/mod components and build the appropriate platform AssetBundle. The pack does not contain proprietary game scripts, BepInEx, Jötunn or Valheim assemblies.

## Coordinates and dimensions

- Source geometry and JSON use right-handed coordinates, Y up, in metres.
- Static root is the placement anchor at floor level, generally the machine's footprint centre.
- Straight belts run from negative Z input to positive Z output.
- Shafts and gearbox/cog axes vary by piece; consult `parts` in `asset.json`.
- A part's vertices are local to its pivot. Do not recenter individual meshes during import.
- Marker positions are asset-root-local. Their `forward` direction is stored in JSON and GLB extras; marker transforms do **not** encode orientation as a rotation.
- Importers convert coordinate conventions. Resolve marker positions from imported transforms and explicitly convert JSON vectors; do not apply a second Y/Z swap blindly.
- Use imported named markers as snap anchors. Connection points are not all on an integer grid. Snap compatibility is decided by matching opposing port directions and positions.
- Corner-belt endpoints differ from the straight belt's centred layout. Snap to the markers, not its bounding-box centre.

## Animation

There are no baked animation clips or game logic. Rotors, crank, millstone, drill and selector are ready for transform-driven animation. The feeder has a rigid shaped arm on one yaw pivot; it is not a fully articulated three-joint robot.

The straight conveyor's slats are one named group for convenient editing. **Do not translate the entire group indefinitely** to simulate a looping belt. For a prototype, keep the deck static and move pooled item visuals. For a later art pass, create a proper looping surface/UV animation or cyclic slat rig.

Drive visual movement from replicated machine state. Do not trigger production directly from an animation callback and assume it is authoritative.

## Collision and performance

Collision boxes in metadata are starting suggestions. They are not imported physics objects or tested placement rules. Some broad boxes, especially a conveyor deck or trough, must be refined to preserve openings and avoid interference with interactive ports. Ground/water probes are sample positions only, not full placement validation.

Meshes are merged by material within each semantic part. They intentionally contain intersecting primitive components and are not manufactured watertight CAD solids. Primitive face normals and triangle winding are checked; UV overlaps are intentional at this prototype stage. Material submeshes and draw calls still need profiling even though triangle counts are small.

Inspect scale beside a Valheim player, collision, support, fire/weather behavior, item-path height, material shaders, LOD strategy and headless prefab registration before release. Never ship this as already game-tested.

## Validation performed

- Twelve GLB exports generated, with matching static OBJ files.
- GLB headers, chunk sizes and data offsets checked.
- Finite vertex data, nonzero triangle areas, unit normals and normal/winding agreement checked.
- Unique node names, marker presence/direction length and metadata triangle counts checked.
- Preview rendered directly from the actual source mesh geometry and visually inspected.
- Python scripts syntax-checked.

This is a custom structural check, not the official Khronos validator. Blender import/FBX export, Unity import and game behavior remain untested. See `validation-report.json`.

## Rebuild

With Python, numpy and matplotlib installed:

```sh
python source/generate_assets.py
python tools/validate_pack.py
```

Matplotlib is only needed for the contact sheet. The generator overwrites generated model files and catalog, so edit the source or keep manual changes in a separate folder.

## Scope

This is the first workshop kit, not all art for the full master design. Hammer pictures for the eight registered pieces are in `polished/icons/`. Still needed later: steam/sail/eitr generators, clutches, extra shaft/rope layouts, farm and livestock machinery, station tenders, biome-tier variants, finished textures, and sounds. Reuse this modular language while implementing the core.

## Provenance

Geometry, palette and scripts were generated for this task, without downloaded third-party models or extracted Valheim/Create/Satisfactory assets. Keep this provenance note with your development files. Set your project's distribution licence deliberately before publishing; this pack does not assign licences to Valheim assets or other dependencies. Disclose generated code/assets according to the publishing platform's current rules.

Technical references consulted: Khronos glTF 2.0 specification, https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html ; Blender import/export API, https://docs.blender.org/api/5.2/bpy.ops.import_scene.html and https://docs.blender.org/api/main/bpy.ops.export_scene.html .
