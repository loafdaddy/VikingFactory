# Development

Early development. These steps compile and load the current source. They do not produce a player-ready mod.

## Prerequisites

| Piece | Used for |
|---|---|
| .NET SDK that can build `net472`, `netstandard2.0`, and `net8.0` | Plugin, core library, and tests |
| Valheim, current player Unity `6000.0.75f1` | Plugin compile and in-game checks |
| BepInExPack Valheim 5.4.2351 | Loading the plugin |
| Jötunn 2.30.2 | Piece registration |
| Blender 5.1, optional | Rebuilding original models |
| Unity `6000.0.75f1`, optional | Prefabs and AssetBundles, after a license exists |

The plugin references the game's `valheim_Data/Managed` assemblies and the BepInEx and Jötunn DLLs next to that install. Those files stay on your machine. Do not commit them, and do not put them in a package.

This tree was checked against Steam app 892970, build id 25527674. Another build may need a fresh look at `docs/Compatibility.md`.

## Local paths

```bash
cp Environment.props.example Environment.props
```

Set `VALHEIM_INSTALL` to your game directory. `Environment.props` is gitignored. Do not paste that path into docs or issues.

`VF_DEPLOY_PLUGINS` is the folder `scripts/deploy-dev.sh` copies into. Point it at an isolated `BepInEx/plugins/VikingFactory` directory. The script refuses an empty path and any `Managed` folder.

A dedicated server, when you have `valheim_server.x86_64`, reads its world password from `VF_DEV_SERVER_PASSWORD`. The script will not start without it. This install has no server binary, so that script has not created a world.

## Build and test

```bash
./scripts/test.sh
./scripts/build.sh
VF_DEPLOY_PLUGINS=/path/to/BepInEx/plugins/VikingFactory ./scripts/deploy-dev.sh
```

`scripts/test.sh` runs the core tests. They do not load Valheim. Twelve tests were passing on 27 September 2026: crank stall, clutch, water-wheel probe, and item escrow.

`scripts/build.sh` builds the solution in Release. The plugin output is `src/VikingFactory.Plugin/bin/Release/VikingFactory.dll` plus `VikingFactory.Core.dll`. Deploy copies those and the polished GLBs for the registered machines.

To look at the game, set Steam's launch options to:

```
./start_game_bepinex.sh %command%
```

Then `scripts/launch-dev.sh` starts the client with an isolated save directory. `scripts/launch-dev.sh client2` uses a second directory. Neither directory had a world in it when this note was written.

`scripts/validate-package.sh` checks `packaging/` for a manifest, README, and a 256×256 icon, and rejects game DLLs. It does not upload.

## Layout

| Path | Responsibility |
|---|---|
| `src/VikingFactory.Core` | Drive, load, and item transfers. No game references. |
| `src/VikingFactory.Plugin` | BepInEx plugin, piece registration, GLB loading, material clones, feeder and basket calls into the game. |
| `tests/VikingFactory.Core.Tests` | xUnit tests for the core. |
| `tools/VikingFactory.Diagnostics` | Prints the core version and whether `assembly_valheim.dll` is present. It does not write the live catalogue. |
| `VikingFactory-Assets` | Original models and the scripts that rebuild them. |
| `packaging` | Thunderstore template. Not a release. |
| `branding` | README lockup. |

Plugin id: `com.vikingfactory`. Version: `0.1.0`. There are no Harmony patches. Jötunn's `CustomPiece` creates the hammer pieces. The catalogue and material probe write JSON under the game's BepInEx config folder when vanilla prefabs become available. Those files are local output, not source.

## Assets

GLB is the source of truth. Coordinates are right-handed, Y up, metres. Material slots are only `Oak`, `OakLight`, `Iron`, `Bronze`, `Hide`, and `Stone`. A root extra `material_profile = valheim` tells the plugin to clone vanilla materials. Without that extra, the piece keeps a flat development colour.

The clone map is in `src/VikingFactory.Plugin/NativeMaterials.cs` and `VikingFactory-Assets/polished/material-map.json`. Timber uses a worn plank shader, iron uses an iron beam, bronze uses cauldron metal, hide uses the tanning rack, and stone uses an interior stone floor. The plugin copies the material and does not edit the shared original. Wood texture scale on the clone is reset to 1,1 because the wall's own scale belongs to that wall. UVs are authored at about 0.5 m per repeat. No Valheim texture is stored in the GLBs.

| Tree | Role |
|---|---|
| `VikingFactory-Assets/models` | Prototype pack. Left as the baseline. |
| `VikingFactory-Assets/polished` | Models for the registered pieces, plus cog, corner, splitter, trough, recipe mill, and quarry. The last six are not hammer pieces. Rebuild with Blender: `polished/source/build_water_wheel.py` and `polished/source/build_pack.py`. |
| `VikingFactory-Assets/expansion` | Twenty-six later machines. Files only. Rebuild with `expansion/source/build_expansion.py`. `audit_blender.py` checks import, scale, and UV density. |
| `VikingFactory-Assets/converted` | Blender conversion of the prototypes. Not the files the plugin loads. |

Moving parts spin or swing only while the machine state says the line is turning. A scrolling surface uses a property block on its own renderer. Animation does not create items.

`VikingFactory-Unity/` is scratch from an editor attempt and is gitignored. Bundles need Unity `6000.0.75f1` with a license. Do not build them with a newer editor.

## Multiplayer and saves

Not verified. The design, and the current code's intent:

- The ZNetView owner runs the machine.
- A feeder moves one item only on that owner, about once every two seconds, and only while the line is turning.
- The basket stores up to eight stacks on ZDO key `vf_items`. The clutch stores open or closed on `vf_clutch`.
- Item copy keeps durability, quality, variant, crafter, world level, custom data, and the cheated flag.
- A vanilla chest within 1.25 m of a feeder port can be a dock. That distance has not been tried in a world.
- Factories are not meant to keep running in unloaded areas.

No second client has connected. No dedicated server has been started. Save and reload of a placed piece has not been done. Do not describe the mod as multiplayer-ready.

## Packaging

When a tested build exists, the package will contain the plugin DLLs, original assets, a 256×256 `icon.png`, `manifest.json`, and a player README. It will not contain Valheim assemblies. Server and every client will need the same build.

Until then, `packaging/manifest.json` has an empty website and empty dependencies on purpose. Do not publish it.
