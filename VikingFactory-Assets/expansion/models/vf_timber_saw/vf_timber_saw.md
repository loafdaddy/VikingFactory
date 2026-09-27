# vf_timber_saw
Reciprocating frame saw • 396 triangles

Bounds: (-0.830, 0.000, -0.850) to (0.830, 2.230, 0.850) metres.
Right-handed, Y-up, unit scale. Empty root: material_profile=valheim.

## Parts
- Static: pivot (0.000, 0.000, 0.000)
- Roller_-0.75: pivot (0.000, 0.630, -0.750) • rotation_axis=x
- Roller_0.75: pivot (0.000, 0.630, 0.750) • rotation_axis=x
- Saw_Frame: pivot (0.000, 1.280, 0.000) • rotation_axis=y, motion=slide, travel=0.28

## Markers
- Item_In: position (0.000, 0.790, -1.000); forward (0.000, 0.000, -1.000); item_in.
- Item_Out: position (0.000, 0.790, 1.000); forward (0.000, 0.000, 1.000); item_out.
- Kinetic_In: position (0.850, 0.500, 0.000); forward (1.000, 0.000, 0.000); kinetic.

## Box colliders (Static-local)
- Box_0: centre (-0.700, 1.075, -0.720); size (0.180, 2.150, 0.180).
- Box_1: centre (-0.700, 1.075, 0.720); size (0.180, 2.150, 0.180).
- Box_2: centre (-0.700, 2.130, 0.000); size (0.200, 0.200, 1.700).
- Box_3: centre (0.700, 1.075, -0.720); size (0.180, 2.150, 0.180).
- Box_4: centre (0.700, 1.075, 0.720); size (0.180, 2.150, 0.180).
- Box_5: centre (0.700, 2.130, 0.000); size (0.200, 0.200, 1.700).
- Box_6: centre (0.000, 2.130, -0.720); size (1.600, 0.200, 0.180).
- Box_7: centre (0.000, 2.130, 0.720); size (1.600, 0.200, 0.180).
- Box_8: centre (0.000, 0.490, -0.750); size (1.400, 0.140, 0.170).
- Box_9: centre (0.000, 0.490, 0.750); size (1.400, 0.140, 0.170).

## Notes
- Saw_Frame translates vertically ±0.14 m. Log axis is Z, supported at y=0.72. Only the cutting blade enters the open log path; no static collider spans the mouth.

Not represented: native material rendering, executable animation/production and Unity physics components.
