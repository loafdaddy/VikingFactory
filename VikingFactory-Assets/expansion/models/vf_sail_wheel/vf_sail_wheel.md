# vf_sail_wheel
Linen sail wheel • 408 triangles

Bounds: (-1.900, 0.000, -0.725) to (1.900, 4.300, 1.740) metres.
Right-handed, Y-up, unit scale. Empty root: material_profile=valheim.

## Parts
- Static: pivot (0.000, 0.000, 0.000)
- Rotor: pivot (0.000, 2.400, -0.280) • rotation_axis=z

## Markers
- Kinetic_Out: position (0.000, 2.400, 0.250); forward (0.000, 0.000, 1.000); kinetic.
- Wind_Probe: position (0.000, 3.500, -0.450); forward (0.000, 0.000, -1.000); environment.

## Box colliders (Static-local)
- Box_0: centre (0.000, 0.060, 0.000); size (1.250, 0.120, 1.450).
- Box_1: centre (-0.400, 1.200, 0.000); size (0.170, 2.400, 0.170).
- Box_2: centre (0.400, 1.200, 0.000); size (0.170, 2.400, 0.170).

## Notes
- Axle height exception: 2.4 m, required by sail clearance. Hide is the mandated sail slot; runtime should select a linen-compatible native surface by this asset ID.

Not represented: native material rendering, executable animation/production and Unity physics components.
