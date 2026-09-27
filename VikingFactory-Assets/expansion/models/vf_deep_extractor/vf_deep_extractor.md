# vf_deep_extractor
Deep extraction derrick • 640 triangles

Bounds: (-1.050, 0.000, -1.135) to (1.050, 3.400, 1.550) metres.
Right-handed, Y-up, unit scale. Empty root: material_profile=valheim.

## Parts
- Static: pivot (0.000, 0.000, 0.000)
- Drill: pivot (0.000, 1.650, 0.000) • rotation_axis=y

## Markers
- Kinetic_In: position (1.050, 0.500, 0.000); forward (1.000, 0.000, 0.000); kinetic.
- Item_Out: position (0.000, 0.400, 1.550); forward (0.000, 0.000, 1.000); item_out.
- Ground_Probe: position (0.000, 0.000, 0.000); forward (0.000, -1.000, 0.000); environment.

## Box colliders (Static-local)
- Box_0: centre (-0.900, 1.650, -1.000); size (0.190, 3.300, 0.190).
- Box_1: centre (-0.900, 1.650, 1.000); size (0.190, 3.300, 0.190).
- Box_2: centre (0.900, 1.650, -1.000); size (0.190, 3.300, 0.190).
- Box_3: centre (0.900, 1.650, 1.000); size (0.190, 3.300, 0.190).

## Notes
- Drill is vertical; perimeter-only colliders keep bore and output clear.
- Iron is the named metal slot; black-metal variant must be selected by runtime using vf_deep_extractor.

Not represented: native material rendering, executable animation/production and Unity physics components.
