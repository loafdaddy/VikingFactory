# vf_steam_engine
Stone boiler beam engine • 1152 triangles

Bounds: (-1.000, 0.000, -1.075) to (1.080, 2.690, 1.160) metres.
Right-handed, Y-up, unit scale. Empty root: material_profile=valheim.

## Parts
- Static: pivot (0.000, 0.000, 0.000)
- Beam: pivot (0.800, 1.270, 0.050) • rotation_axis=x, motion=swing
- Rotor: pivot (0.880, 0.500, 0.730) • rotation_axis=x

## Markers
- Kinetic_Out: position (1.100, 0.500, 0.730); forward (1.000, 0.000, 0.000); kinetic.
- Water_Probe: position (-0.670, 0.400, 0.250); forward (-1.000, 0.000, 0.000); water_connection.
- Smoke_Out: position (0.000, 2.720, -0.200); forward (0.000, 1.000, 0.000); environment.
- Item_In: position (0.000, 0.380, -0.950); forward (0.000, 0.000, -1.000); fuel.
- Interact: position (0.650, 0.900, -0.980); forward (0.000, 0.000, -1.000); service.

## Box colliders (Static-local)
- Box_0: centre (0.000, 0.060, 0.000); size (2.000, 0.120, 2.150).
- Box_1: centre (-0.480, 0.360, -0.200); size (0.250, 0.480, 1.300).
- Box_2: centre (0.480, 0.360, -0.200); size (0.250, 0.480, 1.300).
- Box_3: centre (0.000, 0.700, -0.200); size (1.220, 0.180, 1.300).
- Box_4: centre (0.000, 0.360, 0.420); size (1.180, 0.480, 0.180).
- Box_5: centre (0.800, 0.550, -0.700); size (0.170, 1.100, 0.170).
- Box_6: centre (0.800, 0.550, 0.800); size (0.170, 1.100, 0.170).
- Box_7: centre (0.800, 0.220, 0.730); size (0.200, 0.440, 0.200).

## Notes
- Smoke outlet is above and separated from beam/rotor. Firebox opening is clear. Beam rocks; wheel rotates. No explosion, gauge or pressure dial geometry.

Not represented: native material rendering, executable animation/production and Unity physics components.
