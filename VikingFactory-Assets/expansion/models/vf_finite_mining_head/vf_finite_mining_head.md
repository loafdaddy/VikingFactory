# vf_finite_mining_head
Finite mining head • 568 triangles

Bounds: (-0.800, 0.000, -0.865) to (0.800, 2.200, 1.300) metres.
Right-handed, Y-up, unit scale. Empty root: material_profile=valheim.

## Parts
- Static: pivot (0.000, 0.000, 0.000)
- Drill: pivot (0.000, 1.050, 0.000) • rotation_axis=y

## Markers
- Kinetic_In: position (0.800, 0.500, 0.000); forward (1.000, 0.000, 0.000); kinetic.
- Item_Out: position (0.000, 0.400, 1.300); forward (0.000, 0.000, 1.000); item_out.
- Ground_Probe: position (0.000, 0.000, 0.000); forward (0.000, -1.000, 0.000); environment.

## Box colliders (Static-local)
- Box_0: centre (-0.650, 1.050, -0.750); size (0.150, 2.100, 0.150).
- Box_1: centre (-0.650, 1.050, 0.750); size (0.150, 2.100, 0.150).
- Box_2: centre (0.650, 1.050, -0.750); size (0.150, 2.100, 0.150).
- Box_3: centre (0.650, 1.050, 0.750); size (0.150, 2.100, 0.150).

## Notes
- Drill is vertical; perimeter-only colliders keep bore and output clear.

Not represented: native material rendering, executable animation/production and Unity physics components.
