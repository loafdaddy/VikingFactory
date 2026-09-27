# vf_eitr_motor
Contained eitr motor • 568 triangles

Bounds: (-0.750, 0.000, -0.650) to (0.750, 1.190, 0.650) metres.
Right-handed, Y-up, unit scale. Empty root: material_profile=valheim.

## Parts
- Static: pivot (0.000, 0.000, 0.000)
- Rotor: pivot (0.000, 0.500, 0.000) • rotation_axis=x

## Markers
- Kinetic_Left: position (-0.750, 0.500, 0.000); forward (-1.000, 0.000, 0.000); kinetic.
- Kinetic_Right: position (0.750, 0.500, 0.000); forward (1.000, 0.000, 0.000); kinetic.
- Item_In: position (0.000, 0.450, -0.700); forward (0.000, 0.000, -1.000); fuel.

## Box colliders (Static-local)
- Box_0: centre (0.000, 0.060, 0.000); size (1.300, 0.120, 1.300).
- Box_1: centre (-0.470, 0.480, -0.470); size (0.230, 0.720, 0.230).
- Box_2: centre (-0.470, 0.480, 0.470); size (0.230, 0.720, 0.230).
- Box_3: centre (0.470, 0.480, -0.470); size (0.230, 0.720, 0.230).
- Box_4: centre (0.470, 0.480, 0.470); size (0.230, 0.720, 0.230).
- Box_5: centre (-0.520, 0.310, 0.000); size (0.180, 0.380, 0.220).
- Box_6: centre (0.520, 0.310, 0.000); size (0.180, 0.380, 0.220).

## Notes
- Contained faceted core suggests energy geometrically; no emission, neon or UI runes. Black marble/black metal must be selected by asset-ID-aware runtime material binding.

Not represented: native material rendering, executable animation/production and Unity physics components.
