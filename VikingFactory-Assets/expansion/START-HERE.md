# VikingFactory — all missing machines

26 original machines, packaged together. The 13 already-built IDs in your brief are deliberately excluded.

## Open the complete file

**VikingFactory-Expansion-All.glb** contains all 26 assets as separate named root empties, arranged on a 7 m display grid. This is one combined file, not a welded mesh. Each root has `material_profile = valheim` in glTF extras and identity scale. Reset the display translation when extracting a machine as a prefab.

For individual import, `models/<id>/<id>.glb` has its root at the origin. Each directory also has JSON and Markdown sidecars. `catalog.json` contains the complete machine data, including combined-scene offsets. `Expansion-Contact-Sheet.png` shows every machine. `views/` has front, side and three-quarter views for each one on neutral ground under simulated daylight, rendered directly from the supplied geometry.

## Exact structure

- Each machine is an empty root named with its requested vf_ ID.
- `Static` is an empty child at the origin. Static geometry is joined by material below it.
- Each moving assembly is an empty child of the machine root. It contains one mesh per used material. `rotation_axis` is x, y or z.
- `motion = swing` is used only for rocking beams/arms and gates. `motion = slide` describes saw/tool carriage translation; use the axis as the direction of translation in those cases. It does not instruct a tool head to spin.
- `scroll = u` marks the rope/belt/line surface. Scroll its material coordinates, not its whole transform. Longitudinal U is distance-based; closed loop seams end at an integer repeat.
- Markers are root-level empties, position only. Forward vectors are in the sidecars, not baked into rotations.
- Collider placeholders are non-rendering empty nodes under `Static` with box size metadata. They are **not** Unity physics components. Instantiate BoxColliders from metadata after import.

All files are right-handed, Y-up, in metres. Transforms have unit scale. Each individual piece touches y=0. The sail-wheel axle is the documented exception to the 0.5 m connection height. Some items use service/coupling markers rather than kinetic ports where no mechanical power is appropriate.

## Materials and UVs

Only these six material names occur: Oak, OakLight, Iron, Bronze, Hide, Stone. There are no images, texture objects, embedded shaders, game meshes, logos, Draco or other compression extensions. Base colours are only neutral authoring previews; your native-material cloning script determines their game appearance.

Each render mesh has exactly one material. Timber beams use longitudinal U on sides, cross-axis mapping on cut ends and approximately one UV repeat per 0.5 m. Cyclic rope mapping adjusts the repeat length slightly to close on an integer. Texture density must still be visually verified against the actual native material's UV convention; no Valheim texture was accessed to fake that check.

**Material-name limitation:** the same six slots cannot independently name linen, black marble, black metal, grausten and flametal. Keep these names as requested and choose the native material variant by `(asset ID, material name)` at runtime. Relevant IDs and notes appear in sidecars. The eitr core is a contained faceted shape with no emissive light or shader in this file.

## Import and build instructions for Cursor

1. Read this file, `catalog.json`, `Validation-Report.json` and the chosen machine's note.
2. Preserve existing project identities. Do not replace or redesign the already-built machines.
3. Import individual GLBs with a verified importer, or convert through Blender to the project's supported format. Do not assume Unity has a GLB importer installed.
4. Preserve the root, Static, moving-group and marker hierarchy. Resolve imported transforms instead of blindly applying authoring-axis labels after coordinate conversion.
5. Read glTF extras or use sidecars if the importer drops extras. In the combined scene, reset only the display root offset when extracting a piece.
6. Clone native materials using the six names and asset-specific variants. Do not embed copied game textures/shaders in the distributable bundle.
7. Create box colliders from the Static metadata. Keep marked item/log passages and bed interiors open. Add moving gate collision to the Gate group separately if desired; no fixed collider spans a gate opening.
8. Wire rotation/swing/slide/UV motion to the authoritative machine state. Root-level farm tool heads follow the named Carriage translation as documented. No animation clips or executable logic are supplied.
9. Rope is rotational transmission only. Its sagged closed loop is a separate surface; varying its endpoint distance requires runtime regeneration or scaling that preserves appropriate rope thickness and axle locations.
10. Iron conveyor has an iron ladder chassis, hide driving loop and timber wear treads. Treads are currently static; use the loop for UV motion and pooled item travel, or add a bounded cyclic tread rig. Do not translate the entire deck endlessly.
11. Reversing cog pair must have opposite angular velocities. The meshing gears and opposing timber arrow inlays communicate reversal physically.
12. Add the project's actual game components, recipes, placement checks, network registration and persistence. The geometry does not implement production or authority.
13. Verify openings, shaft alignments, imported native material texture scale, supports, shadows and save/load in game before describing assets as game-tested.

## Special behavior notes

- Coppice and forage beds have real open terrain interiors; no ground slab or vegetation is embedded. Coppice includes an openable gate and a gap in the stone boundary.
- Rail planter/harvester and farm gantry have visible travelling support systems. Moving parts do not imply simulation of a field radius.
- Frame saw uses a vertical reciprocating blade in a timber frame, not a circular saw table. Supports and static collider boxes stay outside the log aperture.
- Both boilers have open firebox mouths and separate raised smoke outlets. Reinforced engine has larger masonry footprint and external straps/corner armor, not simply a different colour.
- The livestock culling model depicts a sorting junction without a blade or gore.
- The oven extension and tender contain no vanilla station mesh.
- Governor balls are low-poly rounded iron masses. Their centrifugal linkage has a fixed mesh pose and needs articulation if dynamic opening is desired.
- Marker arrows in the notes are direction vectors; no floating UI icons are included.

## Validation

All 26 individual GLBs and the combined scene passed the included custom structural checks: IDs, hierarchy, material allowlist, identity scale, geometry/normal integrity, marker vectors, collider metadata, no embedded textures, triangle counts and feet on y=0. All models are below 3,000 triangles. See the report for exact counts and hashes.

These are checked geometry assets, not compiled Valheim prefabs. Blender, Unity, the official Khronos validator and Valheim integration were unavailable here. Views were produced with a software rasterizer from the generated meshes. Native-material look, runtime motions, collision behavior and network operation must be verified in the project.

## Editable source

`source/build_expansion.py` recreates geometry, GLBs, sidecar JSON, combined scene and views using Python, NumPy and Pillow. `source/validate_expansion.py` checks the files. Keep hand-edited models in a separate folder or update the source before rebuilding, since generation overwrites outputs. Original meshes were constructed for this request; no third-party model or game art was extracted or embedded.

**Not represented in the GLB:** native Valheim material rendering, actual glow, executable machine logic, physics components and procedural animation; these require runtime integration.
