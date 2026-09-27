# vf_livestock_feed_gate
Livestock feeding gate • 264 triangles

Bounds: (-0.915, 0.000, -0.915) to (0.915, 1.270, 0.115) metres.
Right-handed, Y-up, unit scale. Empty root: material_profile=valheim.

## Parts
- Static: pivot (0.000, 0.000, 0.000)
- Gate: pivot (-0.720, 0.580, 0.000) • rotation_axis=y, motion=swing

## Markers
- Item_In: position (0.000, 0.480, -0.950); forward (0.000, 0.000, -1.000); item_in.
- Kinetic_In: position (-0.950, 0.500, 0.000); forward (-1.000, 0.000, 0.000); kinetic.
- Interact: position (0.650, 0.800, -0.160); forward (0.000, 0.000, -1.000); service.

## Box colliders (Static-local)
- Box_0: centre (-0.800, 0.600, 0.000); size (0.150, 1.200, 0.150).
- Box_1: centre (0.800, 0.600, 0.000); size (0.150, 1.200, 0.150).
- Box_2: centre (0.000, 0.120, -0.600); size (1.400, 0.080, 0.500).
- Box_3: centre (0.000, 0.300, -0.880); size (1.500, 0.300, 0.070).
- Box_4: centre (0.000, 0.300, -0.320); size (1.500, 0.300, 0.070).
- Box_5: centre (-0.740, 0.300, -0.600); size (0.070, 0.300, 0.600).
- Box_6: centre (0.740, 0.300, -0.600); size (0.070, 0.300, 0.600).

## Notes
- Static collider data covers posts/fence/trough only. Add a moving gate collider attached to Gate if gameplay requires collision during its swing.

Not represented: native material rendering, executable animation/production and Unity physics components.
