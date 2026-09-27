# vf_rope_4m
Rope transmission · 4 m • 2200 triangles

Bounds: (-0.510, 0.000, -2.004) to (0.113, 0.720, 2.004) metres.
Right-handed, Y-up, unit scale. Empty root: material_profile=valheim.

## Parts
- Static: pivot (0.000, 0.000, 0.000)
- Rotor: pivot (0.040, 0.500, -1.800) • rotation_axis=x
- Rotor_Output: pivot (0.040, 0.500, 1.800) • rotation_axis=x
- Belt_Surface: pivot (0.000, 0.000, 0.000) • rotation_axis=x, scroll=u

## Markers
- Kinetic_In: position (-0.510, 0.500, -1.800); forward (-1.000, 0.000, 0.000); kinetic.
- Kinetic_Out: position (-0.510, 0.500, 1.800); forward (-1.000, 0.000, 0.000); kinetic.
- Interact: position (-0.400, 0.700, -1.800); forward (-1.000, 0.000, 0.000); service.

## Box colliders (Static-local)
- Box_0: centre (-0.200, 0.050, -1.800); size (0.550, 0.100, 0.400).
- Box_1: centre (-0.250, 0.360, -1.800); size (0.150, 0.720, 0.150).
- Box_2: centre (-0.200, 0.050, 1.800); size (0.550, 0.100, 0.400).
- Box_3: centre (-0.250, 0.360, 1.800); size (0.150, 0.720, 0.150).

## Notes
- Nominal 4 m module; pulley centres 3.6 m apart. UV U follows the closed rope loop and advances by an integer at its seam.
- Belt_Surface scrolls only; do not rotate its transform. Runtime scales/rebuilds sag for shorter spans.

Not represented: native material rendering, executable animation/production and Unity physics components.
