# vf_farm_gantry
Twin-head field gantry • 492 triangles

Bounds: (-2.110, 0.000, -2.000) to (2.110, 2.190, 2.000) metres.
Right-handed, Y-up, unit scale. Empty root: material_profile=valheim.

## Parts
- Static: pivot (0.000, 0.000, 0.000)
- Carriage: pivot (0.000, 0.000, 0.000) • rotation_axis=z, motion=slide, travel=3.0
- Tool_Head_0: pivot (-0.900, 1.080, 0.000) • rotation_axis=y, motion=slide, follow_group=Carriage
- Tool_Head_1: pivot (0.900, 1.080, 0.000) • rotation_axis=y, motion=slide, follow_group=Carriage

## Markers
- Kinetic_In: position (-2.200, 0.500, -2.000); forward (-1.000, 0.000, 0.000); kinetic.
- Item_In: position (0.000, 0.600, -2.000); forward (0.000, 0.000, -1.000); item_in.
- Item_Out: position (0.000, 0.600, 2.000); forward (0.000, 0.000, 1.000); item_out.
- Ground_Probe: position (0.000, 0.000, 0.000); forward (0.000, -1.000, 0.000); environment.

## Box colliders (Static-local)
- Box_0: centre (-2.000, 0.100, 0.000); size (0.220, 0.200, 4.000).
- Box_1: centre (2.000, 0.100, 0.000); size (0.220, 0.200, 4.000).

## Notes
- Carriage travels along Z. Root-level tool heads follow Carriage translation and retain independent strokes. No collision slab through growing area.

Not represented: native material rendering, executable animation/production and Unity physics components.
