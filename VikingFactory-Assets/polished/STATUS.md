# Asset status

Updated 27 September 2026. Prototype files under `VikingFactory-Assets/models/` were not overwritten.

## Executed

Blender 5.1.1 ran `polished/source/build_water_wheel.py` and `polished/source/build_pack.py`. Each machine has a `.blend`, `.glb`, and `.fbx` under `VikingFactory-Assets/polished/`. None of the GLBs contain images.

A headless Valheim load attached these and resolved native material clones (`Custom/Piece` for timber, iron, bronze, and hide; `Custom/StaticRock` for stone):

| Placed piece | GLB | Checked pivot |
|---|---|---|
| `vf_crank` | `vf_hand_crank` | Crank `(0, 1.05, 0)`, axis x |
| `vf_shaft` | `vf_shaft_2m` | Rotor `(0, 0.5, 0)`, axis z |
| `vf_clutch` | `vf_clutch` | Ports on the axle at `y = 0.5` |
| `vf_water_wheel` | `vf_water_wheel` | Rotor `(0, 2, 0)`, axis x |
| `vf_belt` | `vf_conveyor_2m` | Hide deck at `y = 0.77`, rollers on local x |
| `vf_feeder` | `vf_feeder` | Arm at `(0, 0.8, 0)`, swing on y |
| `vf_basket` | `vf_catch_basket` | Open front, wall and floor colliders |

The belt scrolls a property block on its own renderer while the line is turning. It does not change the shared vanilla material and it does not create items. The feeder arm swings the same way.

Polished, not registered: `vf_cog`, `vf_conveyor_corner`, `vf_splitter`, `vf_gravity_trough`, `vf_recipe_mill`, `vf_quarry`.

## Unity

Game player: `6000.0.75f1` (`26349cd2a5c8`). A matching editor install printed that version and included Linux IL2CPP support. A Unity Personal license is active in the Flatpak Hub for the editor window. Host batchmode on 27 September 2026 still exited 198, so no prefab and no AssetBundle exist. An older `6000.6.3f1` editor is not the one to use for bundles.

## Hammer pictures

`polished/source/render_icons.py` rendered a 256×256 transparent picture of each registered piece into `polished/icons/`. The marker is a timber block. The plugin loads those PNGs for the hammer buttons. They have not been seen in the build menu. Valheim’s water-piece flag is not set: it lifts the pivot 3 m and rejects dry ground unless Shift is held. One wheel placed before that change floated.

## Unverified

Daylight, rain, torchlight, indoor light, grain, metal response, spin without wobble, belt scroll, save/load, and a second client. The headless launch has no camera.
