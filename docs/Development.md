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

## Versions

`Version.props` is the number for the update on the current branch. The core assembly, `PluginVersion` in `VikingFactoryPlugin.cs`, and `packaging/manifest.json` must all use that number. `VersionTests` fails if they drift. What changed in each number is [CHANGELOG.md](../CHANGELOG.md).

`main` keeps the last snapshot that was pushed. This branch is `0.3.0`. Start a new update from `main` by branching to the next number and setting `Version.props` to it. If you are already on a version branch and the work is a further update, branch again to the next number instead of continuing on the old one.

## Build and test

```bash
./scripts/test.sh
./scripts/build.sh
VF_DEPLOY_PLUGINS=/path/to/BepInEx/plugins/VikingFactory ./scripts/deploy-dev.sh
./scripts/package.sh
dotnet run --project tools/VikingFactory.Diagnostics -c Release -- coverage
```

`scripts/test.sh` runs the core tests. They do not load Valheim. 66 tests were passing on 27 September 2026: power, ratios, flywheel, fuel, items, belts, routing, the recipe mill, production, fields, livestock, scheduling, ownership, schema, presets, coverage, and the version match.

`scripts/build.sh` builds the solution in Release. The plugin output is `src/VikingFactory.Plugin/bin/Release/VikingFactory.dll` plus `VikingFactory.Core.dll`. Deploy copies those, every GLB `MachineCatalog.cs` names (polished first, then expansion), and the hammer pictures in `polished/icons/`. `scripts/package.sh` stages the same files in Thunderstore layout under `artifacts/`, runs `validate-package.sh`, and zips it. It does not upload. The diagnostics `coverage` mode turns the exported catalogue into `docs/AutomationCoverage.csv`.

To look at the game, set Steam's launch options to:

```
./start_game_bepinex.sh %command%
```

Then `scripts/launch-dev.sh` starts the client with an isolated save directory. `scripts/launch-dev.sh client2` uses a second directory. The primary directory has a character and a world from 27 September 2026. `client2` was still empty.

The hammer lists only pieces that character has discovered. A new character sees Repair, and the grid looks empty. `debugmode` shows every piece and resets every launch. In the world, press F5 and run `devcommands`, then `debugmode`. The workshop pieces are on the Crafting tag. Patch notes are [CHANGELOG.md](../CHANGELOG.md).

`scripts/validate-package.sh` checks `packaging/` for a manifest, README, and a 256×256 icon, and rejects game DLLs. It does not upload.

## Layout

| Path | Responsibility |
|---|---|
| `src/VikingFactory.Core` | Kinetics, fuel, items and codec, belts and routing, recipe mill and upgrades, production, fields, livestock, scheduling, ownership, schema, presets, coverage. No game references. |
| `src/VikingFactory.Plugin` | BepInEx plugin. `Machines/MachineCatalog.cs` lists every piece. `WorkshopMachine` and the behaviour files hold per-piece logic. `Endpoints.cs` holds chest, machine-port, and native-station adapters. `WorkshopSimulator` runs the 5 Hz step. `Commands.cs` is the `vf` console command. |
| `tests/VikingFactory.Core.Tests` | xUnit tests for the core. |
| `tools/VikingFactory.Diagnostics` | Prints the core version and whether `assembly_valheim.dll` is present. It does not write the live catalogue. |
| `VikingFactory-Assets` | Original models and the scripts that rebuild them. |
| `packaging` | Thunderstore template. Not a release. |
| `branding` | README lockup. |

Plugin id: `com.vikingfactory`. The version is `Version.props`, currently `0.3.0`. There are no Harmony patches; station adapters call the stations' own RPCs and read their ZDO fields, and the mill reads one private field (`CraftingStation.m_haveFire`) by reflection. Jötunn's `CustomPiece` creates the hammer pieces, and configuration syncs through Jötunn's admin-only entries. The catalogue and material probe write JSON under the game's BepInEx config folder when vanilla prefabs become available. Those files are local output, not source.

## Assets

GLB is the source of truth. Coordinates are right-handed, Y up, metres. Material slots are only `Oak`, `OakLight`, `Iron`, `Bronze`, `Hide`, and `Stone`. A root extra `material_profile = valheim` tells the plugin to clone vanilla materials. Without that extra, the piece keeps a flat development colour.

The clone map is in `src/VikingFactory.Plugin/NativeMaterials.cs` and `VikingFactory-Assets/polished/material-map.json`. Timber uses a worn plank shader, iron uses an iron beam, bronze uses cauldron metal, hide uses the tanning rack, and stone uses an interior stone floor. The plugin copies the material and does not edit the shared original. Wood texture scale on the clone is reset to 1,1 because the wall's own scale belongs to that wall. UVs are authored at about 0.5 m per repeat. No Valheim texture is stored in the GLBs.

| Tree | Role |
|---|---|
| `VikingFactory-Assets/models` | Prototype pack. Left as the baseline. |
| `VikingFactory-Assets/polished` | Models for the registered pieces, plus cog, corner, splitter, trough, recipe mill, and quarry. The last six are not hammer pieces. Rebuild with Blender: `polished/source/build_water_wheel.py` and `polished/source/build_pack.py`. |
| `VikingFactory-Assets/expansion` | Twenty-six later machines. Twenty-five are hammer pieces in `0.3.0`; the fishing winch is not. Rebuild with `expansion/source/build_expansion.py`. `audit_blender.py` checks import, scale, and UV density. Collider boxes come from the `Collider_Box` metadata nodes. |
| `VikingFactory-Assets/converted` | Blender conversion of the prototypes. Not the files the plugin loads. |

Moving parts spin or swing only while the machine state says the line is turning. A scrolling surface uses a property block on its own renderer. Animation does not create items.

`VikingFactory-Unity/` is scratch from an editor attempt and is gitignored. Bundles need Unity `6000.0.75f1` with a license. Do not build them with a newer editor.

## Hammer pictures

`polished/source/render_icons.py` renders any missing 256×256 picture from the model each piece names. Pass `--all` to redo them. With the Flatpak Blender:

```
flatpak run --filesystem=$PWD org.blender.Blender -b --python VikingFactory-Assets/polished/source/render_icons.py
```

## Multiplayer and saves

Not verified in a world. The code's rules:

- The ZNetView owner runs each machine. A machine acts only from its second consecutive step as owner, after rereading state the previous owner saved.
- Every peer solves the kinetic graph for visuals. Only owners move items, spend fuel, or write ZDOs.
- Native station inserts happen only while this peer owns the station, so the station's RPC runs locally and its queue can be checked before and after.
- Items live on ZDOs: belts `vf_queue`, baskets `vf_items`, feeders `vf_hand`, mills `vf_in`, `vf_escrow`, `vf_out`, `vf_recovery`. A belt saves the moment an item leaves it.
- Destruction drops held items once, on the owner, through `WearNTear.m_onDestroyed`. Unloading drops nothing.
- The simulator steps at 5 Hz. A frame gap over 2 s, such as sleep, is dropped, not replayed.
- Factories do not run where nobody is nearby.

No second client has connected. No dedicated server has been started. Do not describe the mod as multiplayer-ready.

## Packaging

When a tested build exists, the package will contain the plugin DLLs, original assets, a 256×256 `icon.png`, `manifest.json`, and a player README. It will not contain Valheim assemblies. Server and every client will need the same build.

`packaging/manifest.json` lists BepInExPack 5.4.2351 and Jötunn 2.30.2. `packaging/PLAYER-README.md` becomes the package readme. There is no license file yet. Do not publish.
