# Roadmap

Early development. Nothing below is a release date, a percentage, or a promise that a model already works in game.

A GLB, a class, or a design note is not a finished machine. Visual asset work and gameplay are listed separately. The engineering checklist is [`docs/ImplementationStatus.md`](docs/ImplementationStatus.md). The long design proposal is [`docs/VikingFactory-Master-Prompt.md`](docs/VikingFactory-Master-Prompt.md).

## Current focus

Taken from the repository checklist, not from a guess about what is being typed today.

**See the first workshop line in a world, and prove the power rule there.**

The plugin already registers a marker, hand crank, shaft, clutch, water wheel, timber belt, feeder, and catch basket. Polished models load in a headless launch. Core tests cover the stall rule without starting the game.

| | |
|---|---|
| Player outcome | Place a crank, shaft, and feeder, see the line turn or stall, and still have the pieces after a save. |
| Developer outcome | Confirm materials, pivots, and colliders beside vanilla wood and a workbench. |
| Status | Code and models exist. One isolated world was entered and one water wheel was placed. It floated. The placement fix and hammer pictures are in `0.2.0` and have not been looked at in a world. |
| Blockers | Unity `6000.0.75f1` batchmode still has no license, so there is no AssetBundle. The current pieces load GLBs without one. Steam must launch through BepInEx. No second client and no dedicated server have been run. |
| Done when | One placed line stalls when load exceeds drive, items survive a save, and the turning direction is visible. Rain, torchlight, and indoor light have been looked at. The result is written into the status doc without calling it a player release. |

## Next

**Make the first logistics honest, then touch a vanilla station.**

Belts currently reserve power and can scroll a surface. They do not move a queue of items. Polished models exist for a cog, corner belt, splitter, trough, recipe mill, and quarry, and they are not hammer pieces. Expansion models for rope and later machines are the same: files, not features.

| | |
|---|---|
| Player outcome | A feeder moves one item into a basket or chest while the line turns, and a belt or trough is only claimed once it actually carries goods. |
| Status | Feeder and basket code exists and is untested in a world. Carrying belts, splitters, mergers, kilns, and smelters are not built. |
| Blockers | The world test above. Station adapters must use the live game methods, including item provenance. No Harmony patch exists yet. |
| Done when | One item is removed from the source before it is added to the destination, a failed add does not delete it, and a kiln or smelter still uses its own timer and fuel. |

## Later

Design targets. Not scheduled.

- **Power.** Rope between shafts, a reversing cog, sail, steam, a flywheel, and a governor. Speed stays separate from load. Routing pieces should stay cheap to place.
- **Processing.** A recipe mill that runs a recipe the player already knows, in range of the real station, from its own buffers. Quality upgrades only through the game’s own rules.
- **Land.** Managed coppice, a bedrock quarry, planters and harvesters on a visible field, forage, hives, sap, and fermenters. Vanilla growth times stay.
- **Food.** A spit or oven tender that can burn food if the line stalls. Cooking time stays the game’s.
- **Later industry.** Finite mining, deep extraction, eitr, an assembler, livestock gates, and a fishing winch. Models for several of these already exist and are not registered.

Each of those is done when a player can build it with legal materials, see it work, reload the world, and join with a second client without losing or duplicating items. A finished mesh is not that test.

## Release readiness

There is no public package. `packaging/` is a template. `scripts/validate-package.sh` checks that template’s files. It does not upload.

| Milestone | Outcome | Status | Blockers | Done when |
|---|---|---|---|---|
| Dev build | A contributor can compile and load the plugin | Plugin `0.2.0` has loaded in a window. `0.1.0` loaded headless | Local game path stays in gitignored `Environment.props` | Documented setup still matches the scripts |
| World proof | The current pieces survive play | One wheel placed, then it floated | The corrected place, hammer pictures, save, and a stall have not been checked | Save, reload, and a visible stall |
| Multiplayer proof | Server and clients agree | Not implemented as a tested feature | No dedicated server has been run. No second client. | Same build on server and clients. One item transfer, no duplicate, owner handoff checked |
| Package | Someone can install a zip | Not a release | Empty manifest website and dependencies. No license. Icon is a flat mark, not the logo | Version, icon, README, and dependencies match a build that passed the world and multiplayer checks |
| Thunderstore | A public alpha | Not started | Everything above | A separate, explicit publish. Game files are not in the zip |

## What this roadmap will not do

It will not add new ores, foods, or gear, change vanilla timers, or keep factories running in chunks where nobody is nearby.
