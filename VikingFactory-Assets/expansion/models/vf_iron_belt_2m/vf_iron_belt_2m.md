# vf_iron_belt_2m
Iron chain-deck conveyor • 1116 triangles

Bounds: (-0.570, 0.000, -1.000) to (0.570, 0.915, 1.000) metres.
Right-handed, Y-up, unit scale. Empty root: material_profile=valheim.

## Parts
- Static: pivot (0.000, 0.000, 0.000)
- Belt_Surface: pivot (0.000, 0.770, 0.000) • rotation_axis=x, scroll=u
- Roller_Rear: pivot (0.000, 0.700, -0.850) • rotation_axis=x
- Roller_Front: pivot (0.000, 0.700, 0.850) • rotation_axis=x

## Markers
- Item_In: position (0.000, 0.850, -1.000); forward (0.000, 0.000, -1.000); item_in.
- Item_Out: position (0.000, 0.850, 1.000); forward (0.000, 0.000, 1.000); item_out.
- Kinetic_In: position (0.570, 0.500, -0.750); forward (1.000, 0.000, 0.000); kinetic.

## Box colliders (Static-local)
- Box_0: centre (-0.460, 0.310, -0.740); size (0.140, 0.620, 0.140).
- Box_1: centre (-0.460, 0.310, 0.740); size (0.140, 0.620, 0.140).
- Box_2: centre (0.460, 0.310, -0.740); size (0.140, 0.620, 0.140).
- Box_3: centre (0.460, 0.310, 0.740); size (0.140, 0.620, 0.140).
- Deck: centre (0.000, 0.690, 0.000); size (0.860, 0.150, 2.000).

## Notes
- Iron ladder chassis, overlapping timber treads, forged wear caps and braced feet distinguish it from the timber tier. Decorative treads stay static; scroll only the loop or add runtime cyclic treads.

Not represented: native material rendering, executable animation/production and Unity physics components.
