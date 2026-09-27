# vf_reversing_cog
Reversing cog pair • 1236 triangles

Bounds: (-0.400, 0.000, -0.750) to (0.434, 0.860, 0.750) metres.
Right-handed, Y-up, unit scale. Empty root: material_profile=valheim.

## Parts
- Static: pivot (0.000, 0.000, 0.000)
- Rotor: pivot (0.000, 0.500, -0.360) • rotation_axis=x
- Rotor_Output: pivot (0.000, 0.500, 0.360) • rotation_axis=x

## Markers
- Kinetic_In: position (-0.370, 0.500, -0.360); forward (-1.000, 0.000, 0.000); kinetic.
- Kinetic_Out: position (0.370, 0.500, 0.360); forward (1.000, 0.000, 0.000); kinetic.

## Box colliders (Static-local)
- Box_0: centre (0.000, 0.060, 0.000); size (0.800, 0.120, 1.500).
- Box_1: centre (-0.250, 0.240, -0.360); size (0.120, 0.480, 0.120).
- Box_2: centre (0.250, 0.240, -0.360); size (0.120, 0.480, 0.120).
- Box_3: centre (-0.250, 0.240, 0.360); size (0.120, 0.480, 0.120).
- Box_4: centre (0.250, 0.240, 0.360); size (0.120, 0.480, 0.120).

## Notes
- Two meshing external gears; Rotor_Output must run at the opposite angular velocity. Fixed timber chevrons indicate opposing rotation.

Not represented: native material rendering, executable animation/production and Unity physics components.
