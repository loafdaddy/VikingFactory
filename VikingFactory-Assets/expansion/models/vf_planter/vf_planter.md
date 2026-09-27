# vf_planter
Rail planter • 504 triangles

Bounds: (-1.330, 0.000, -1.500) to (1.330, 1.330, 1.500) metres.
Right-handed, Y-up, unit scale. Empty root: material_profile=valheim.

## Parts
- Static: pivot (0.000, 0.000, 0.000)
- Tool_Head: pivot (0.000, 0.000, 0.000) • rotation_axis=z, motion=slide, travel=2.2

## Markers
- Kinetic_In: position (-1.400, 0.500, -1.500); forward (-1.000, 0.000, 0.000); kinetic.
- Item_In: position (0.000, 0.700, -1.500); forward (0.000, 0.000, -1.000); item_in.
- Ground_Probe: position (0.000, 0.000, 0.000); forward (0.000, -1.000, 0.000); environment.

## Box colliders (Static-local)
- Box_0: centre (-1.250, 0.080, 0.000); size (0.160, 0.160, 3.000).
- Box_1: centre (1.250, 0.080, 0.000); size (0.160, 0.160, 3.000).

## Notes
- Tool_Head is a complete travelling carriage along Z. Root marker is the parked service dock; moving operation points must be derived from the carriage.

Not represented: native material rendering, executable animation/production and Unity physics components.
