# vf_livestock_culling_gate
Livestock sorting gate • 348 triangles

Bounds: (-1.515, 0.000, -0.115) to (1.515, 1.270, 1.340) metres.
Right-handed, Y-up, unit scale. Empty root: material_profile=valheim.

## Parts
- Static: pivot (0.000, 0.000, 0.000)
- Gate: pivot (-0.720, 0.580, 0.000) • rotation_axis=y, motion=swing

## Markers
- Dropoff: position (0.000, 0.150, 1.300); forward (0.000, 0.000, 1.000); sorting_exit.
- Kinetic_In: position (-0.950, 0.500, 0.000); forward (-1.000, 0.000, 0.000); kinetic.
- Interact: position (0.650, 0.800, -0.160); forward (0.000, 0.000, -1.000); service.

## Box colliders (Static-local)
- Box_0: centre (-0.800, 0.600, 0.000); size (0.150, 1.200, 0.150).
- Box_1: centre (0.800, 0.600, 0.000); size (0.150, 1.200, 0.150).
- Box_2: centre (-1.075, 0.475, 0.550); size (0.100, 0.950, 0.100).
- Box_3: centre (-1.425, 0.475, 1.250); size (0.100, 0.950, 0.100).
- Box_4: centre (1.075, 0.475, 0.550); size (0.100, 0.950, 0.100).
- Box_5: centre (1.425, 0.475, 1.250); size (0.100, 0.950, 0.100).

## Notes
- Sorting gate with diverging lanes, no blade or gore. Gameplay culling is not represented visually.
- Static collider data covers posts/fence/trough only. Add a moving gate collider attached to Gate if gameplay requires collision during its swing.

Not represented: native material rendering, executable animation/production and Unity physics components.
