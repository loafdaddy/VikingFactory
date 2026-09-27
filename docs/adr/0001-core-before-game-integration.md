# ADR 0001: Test the simulation before referencing the game

## Status

Accepted, 27 September 2026.

## Context

The master prompt requires a repository audit, then a playable milestone. When this decision was accepted, this machine had no Valheim install, so compiling against `assembly_valheim.dll` would have meant inventing method signatures. The update below records that the game was installed later.

## Decision

`VikingFactory.Core` is a `netstandard2.0` library with no game references. It owns drive-unit reservation, clutch splits, and chest-to-chest transfers that keep item provenance. `dotnet test` covers that core.

The BepInEx plugin is not in the solution. It will be added only after local assemblies confirm the target framework and the real piece, inventory, and station methods.

Water-wheel drive stays off unless a placement probe says the site is valid. The default probe says it is not.

`DESIGN.md` remains the earlier outline. Where it disagrees with `docs/VikingFactory-Master-Prompt.md`, the master prompt is the target. In particular: renewable wood and stone are in scope, ingredient buffers are dynamic rather than a hard four, cheat flags are preserved rather than forced false, and quality upgrades are a later operation on an existing item.

## Consequences

The repository builds and tests without loading the game. Nothing in that build has been placed, saved, or run inside Valheim.

## Update, 27 September 2026

Valheim is now installed, and the plugin is in the solution. A headless load registered the pieces. The core still has no game references. The cross-off list is `docs/ImplementationStatus.md`.

- [x] Core simulation and its tests
- [x] Plugin compiled against the local assemblies, copied into the game folder, and loaded headless
- [ ] A placed piece or a save inside a world

## Update, 0.3.0

The same split holds for every milestone. Graph solving, fuel accounting, belt queues, routing, the recipe mill's escrow, upgrades, production, field reserves, culling, scheduling, ownership epochs, schema gating, and coverage are in the core with 66 tests. The plugin adapts them to live game objects and calls the stations' own RPCs.

- [x] Core logic for M0–M8 and its tests
- [x] Plugin compiled, packaged, and loaded headless with 39 pieces
- [ ] A placed piece or a save inside a world
