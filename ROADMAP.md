# Roadmap

Early development. Nothing below is a release date, a percentage, or a promise that a model already works in game.

A GLB, a class, or a design note is not a finished machine. Visual asset work and gameplay are listed separately. The engineering checklist is [`docs/ImplementationStatus.md`](docs/ImplementationStatus.md). The long design proposal is [`docs/VikingFactory-Master-Prompt.md`](docs/VikingFactory-Master-Prompt.md).

## Current focus

**Prove `0.3.0` in a world.**

`0.3.0` has code for every milestone: power, belts, native stations, the recipe mill, wood and stone, fields and kitchens, steam and sail, extraction, livestock, and a coverage report. Core tests cover the rules. A headless launch loaded all 39 pieces. A windowed session placed fourteen kinds of piece in `VIKINGFACTORY` and saved. None of those lines was watched working, and the world was not reloaded.

| | |
|---|---|
| Player outcome | Build the lines in `docs/QA-Checklist.md` and have them behave as described, including after a save. |
| Developer outcome | Confirm pivots, colliders, snap points, and rates beside vanilla pieces. Fix what the world shows. |
| Status | Code and tests done. Pieces can be placed. The checklist has not been run. |
| Blockers | Someone has to play the checklist. No dedicated server binary on this machine. No second client has joined. |
| Done when | Each checklist step is ticked in `docs/ImplementationStatus.md` with what was observed. |

## Next

**Two clients and a dedicated server.**

| | |
|---|---|
| Player outcome | Two players share a workshop without items being lost or duplicated when one leaves. |
| Status | Owner-only simulation, ownership epochs, and guarded station inserts exist in code. Not tried with two peers. |
| Blockers | The in-world pass above. A server install. |
| Done when | Checklist step 8.6 passes with an hour-long soak. |

## Later

Not scheduled.

- **Fishing winch.** Not registered. No safe native capture path was found, so it stays a model.
- **Upkeep of the rules the world shows are wrong.** Rates in `docs/Balance.md` are proposals until play says otherwise.
- **Ashlands and Deep North stations.** The frost kiln, frost foundry, and kiln engine report "Unsupported adapter" until each is reviewed.
- **Asset bundles.** Not needed while pieces load GLBs.
- **A license file**, before any public package.

Everything in `0.3.0` is done only when a player can build it with legal materials, see it work, reload the world, and join with a second client without losing or duplicating items. Code and tests are not that proof.

## Release readiness

There is no public package. `packaging/` is a template. `scripts/validate-package.sh` checks that template’s files. It does not upload.

| Milestone | Outcome | Status | Blockers | Done when |
|---|---|---|---|---|
| Dev build | A contributor can compile and load the plugin | `0.3.0` loaded headless and in a window, 39 pieces | Local game path stays in gitignored `Environment.props` | Documented setup still matches the scripts |
| World proof | The current pieces survive play | Fourteen kinds placed once, then the world saved on quit | Nobody has played the QA checklist or reloaded | Save, reload, and a visible stall |
| Multiplayer proof | Server and clients agree | Not implemented as a tested feature | No dedicated server has been run. No second client. | Same build on server and clients. One item transfer, no duplicate, owner handoff checked |
| Package | Someone can install a zip | A test ZIP stages, validates, and loads headless | No license. Not tested in a world or with a server | Version, icon, README, and dependencies match a build that passed the world and multiplayer checks |
| Thunderstore | A public alpha | Not started | Everything above | A separate, explicit publish. Game files are not in the zip |

## What this roadmap will not do

It will not add new ores, foods, or gear, change vanilla timers, or keep factories running in chunks where nobody is nearby.
