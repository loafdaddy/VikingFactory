# vf_fishing_winch
Waterside fishing winch • 1620 triangles

Bounds: (-0.525, 0.000, -0.650) to (0.525, 1.200, 1.701) metres.
Right-handed, Y-up, unit scale. Empty root: material_profile=valheim.

## Parts
- Static: pivot (0.000, 0.000, 0.000)
- Rotor: pivot (0.000, 0.500, 0.000) • rotation_axis=x
- Line_Surface: pivot (0.000, 0.000, 0.000) • rotation_axis=x, scroll=u

## Markers
- Kinetic_Left: position (-0.450, 0.500, 0.000); forward (-1.000, 0.000, 0.000); kinetic.
- Kinetic_Right: position (0.450, 0.500, 0.000); forward (1.000, 0.000, 0.000); kinetic.
- Water_Probe: position (0.000, 0.120, 1.650); forward (0.000, -1.000, 0.000); environment.
- Item_Out: position (0.000, 0.400, -0.700); forward (0.000, 0.000, -1.000); item_out.

## Box colliders (Static-local)
- Box_0: centre (0.000, 0.060, 0.000); size (1.050, 0.120, 1.300).
- Box_1: centre (-0.360, 0.250, 0.000); size (0.150, 0.500, 0.150).
- Box_2: centre (0.360, 0.250, 0.000); size (0.150, 0.500, 0.150).

## Notes
- Rotor is drum/spool; Line_Surface uses longitudinal UVs, no physical rope simulation.

Not represented: native material rendering, executable animation/production and Unity physics components.
