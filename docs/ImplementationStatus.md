# Implementation status

Updated 27 September 2026 for `0.3.0`. This is the living cross-off list. The master prompt and the Thunderstore roadmap stay the target design. A checked box means the work exists in this repository or was observed on this machine. Code that compiles is not a gate that passed.

`0.3.0` wrote the code for every milestone, M0 through M8. The core has 66 passing tests. A headless launch registered all 39 hammer pieces, and a second headless launch did the same from the packaged ZIP. On 27 September 2026 a windowed `0.3.0` client entered the isolated world `VIKINGFACTORY`, placed the pieces listed under M0, and quit. The world saved. No plugin error was logged. Placing a piece is not a milestone gate. Every gate that says "in a world", "save and reload", "second client", or "dedicated server" is still open. `docs/QA-Checklist.md` is the script for closing them. The public summary is `ROADMAP.md`. Setup is `docs/Development.md`.

## Milestone gates

Two columns per milestone: the code exists, and the gate was observed.

| Milestone | Code in 0.3.0 | Gate observed |
|---|---|---|
| M0 audit and scaffold | [x] | [x] load. [x] pieces placed in a world. [ ] feet, hover, and a reload checked |
| M1 power and logistics | [x] | [ ] two feeders stall a crank in a world, items survive save and reload, direction visible |
| M2 native production line | [x] | [ ] native rates and smoke, full output never duplicates, authority change |
| M3 recipe mill | [x] | [ ] bronze nails and one food recipe in a world, station removal, recipe change, cancellation |
| M4 renewable wood and stone | [x] | [ ] repeatable wood and stone loop, no overlapping plots, no refund duplication |
| M5 fields and kitchens | [x] | [ ] root-crop two-field loop, one food or mead chain, burn risk kept |
| M6 steam, sail, governor, ratios, flywheel | [x] | [x] energy accounting, contradictory cycles, split and merge, fuel stall, in core tests. [ ] in a world |
| M7 later extraction | [x] | [x] progression fails closed, commissioning not refundable, in core tests. [ ] in a world |
| M8 livestock, coverage, soak, package | [x] | [x] coverage report generated. [x] package ZIP loaded headless. [ ] dedicated-server soak. [ ] livestock in a world |

The original gate list, kept for continuity:

- [x] M0 scaffold is in the repository: versions, core tests, build, deploy, and an inert piece in code
- [x] M0 load: `0.3.0` headless on 27 September 2026 logged `VikingFactory 0.3.0 loaded` and `Registered 39 of 39 workshop pieces`, each `Valid=True`
- [x] M0 world, partial: on 27 September 2026 the windowed `0.3.0` client loaded `VIKINGFACTORY` (save 2) and placed `vf_farm_gantry`, `vf_splitter`, `vf_water_wheel`, three `vf_shaft`, four `vf_rope`, `vf_recipe_mill`, `vf_reinforced_steam`, `vf_sail_wheel`, `vf_assembler`, `vf_eitr_motor`, `vf_quarry`, two `vf_belt`, four `vf_marker`, and `vf_crank`. Quit exit code 0. Three dirty chunks saved. No VikingFactory error. The `0.2.0` wheel that floated was not confirmed removed.
- [x] M1 machines are registered in code: crank, shaft, clutch, water wheel, belt, feeder, basket
- [ ] M1 gate: two feeders stall a crank in a world, items survive save and reload, direction is visible
- [ ] M2 gate: native kiln and smelter line, splitter, and merger in a world. Code exists.
- [ ] M3 gate: recipe mill in a world. Code exists.
- [ ] M4 gate: renewable wood and stone in a world. Code exists.
- [ ] M5 gate: fields and kitchens in a world. Code exists.
- [ ] M6 gate: steam, sail, governor, ratios, and flywheel in a world. Code and core tests exist.
- [ ] M7 gate: later extraction in a world. Code and core tests exist.
- [ ] M8 gate: livestock in a world and a dedicated-server soak. Coverage report and a validated package exist.

## M0 — audit and scaffold

- [x] Valheim is installed. Unity `6000.0.75f1`, Steam build `25527674`. See `docs/Compatibility.md`.
- [x] BepInEx pack 5.4.2351 and Jötunn 2.30.2 are in that game folder.
- [x] Local paths live in gitignored `Environment.props`. Game DLLs are not in the repository.
- [x] `scripts/build.sh`, `test.sh`, `deploy-dev.sh`, `validate-package.sh`, and `package.sh` exist.
- [x] `VikingFactory.Core` is `netstandard2.0` and has no game references.
- [x] Station, inventory, item-drop, recipe, crafting-station, plant, pickable, taming, and damage signatures were read from the local `assembly_valheim.dll` with ILSpy before any adapter was written. The decompiled text is not in the repository.
- [x] Startup line and 39 registrations confirmed headless, twice: once from `deploy-dev.sh`, once from the packaged ZIP.
- [x] Boss keys resolved from the boss prefabs in this game: `defeated_eikthyr`, `defeated_gdking`, `defeated_bonemass`, `defeated_dragon`, `defeated_goblinking`, `defeated_queen`, `defeated_fader`.
- [x] Live catalogue export now includes creature drops, trader stock, and the any-one-ingredient and upgrade-only recipe flags.
- [x] `vf status`, `vf network`, `vf exportcatalog`, `vf coverage`, `vf validate`, and admin-only `vf recover` console commands. Only the catalogue export has run, at startup. The commands have not been typed in a world.
- [x] Server-synced configuration through Jötunn: preset, bedrock quarry, mining and saw tool tier, modded recipe allowlist. `NetworkCompatibility` requires the mod on server and every client, minor version strict.
- [x] Per-machine save schema key `vf_schema`. Older saves migrate; a newer schema pauses the machine instead of overwriting it.
- [x] Pieces placed in a world. See the M0 world line for which ones, and what the height log said.
- [ ] Checklist step 1 still open: feet on the ground, no Shift, hover text, hammer pictures, and the old floating wheel gone. The new water wheel logged no terrain hit under its pivot. The recipe mill and reinforced steam engine did, at about 0 m. Markers logged 0.30 m, the center of the 0.6 m block. Shafts and ropes shared world height 30.92 m with the ground 1.2–2.0 m below.

## M1 — power and logistics

- [x] Hand crank, 8 DU while held. Holding costs 3 stamina a second through `Player.UseStamina`; a tired player cannot turn it. The held state is written to the ZDO as world time, so every peer solves the same line.
- [x] Shaft, rope drive (4 m), bronze cog (1:1, 2:1, 1:2), reversing cog, clutch.
- [x] Kinetic links are made between `Kinetic*` markers within 0.45 m, found through a 1 m spatial hash. Hammer snap points sit on every kinetic and item port.
- [x] Speed and direction per segment. Spinning parts turn at the signed RPM, so a reversed segment visibly turns the other way. Hover shows RPM and clockwise or counter-clockwise.
- [x] Water wheel: 48 DU only when the lower paddles are at least 0.4 m into water, the water is at least 0.5 m deep, no other wheel is within 6 m, and no built piece blocks the wheel's volume. There is no current vector and no freeze rule.
- [x] Timber belt, corner, and iron belt carry a real item queue: four items, 30 or 60 a minute, only while the line turns. The front item moves into the next belt, splitter, merger, machine input, or a docked chest. A full front blocks the belt. Items are saved on the belt's ZDO at every change.
- [x] Gravity trough: no power, 15 a minute, and only when its output is at least 10 cm lower than its input.
- [x] Bronze feeder 6 DU, 30 a minute; iron feeder 8 DU, 60 a minute. Ghost filter by using an item on it. Stock target with hysteresis by interacting.
- [x] Feeder transfer order: the destination is asked first, the item is removed from the source, the item is saved "in hand" on the feeder's ZDO, then inserted. A refused insert keeps the item in hand and retries. It is never discarded or sent twice.
- [x] Catch basket: eight stacks on ZDO key `vf_items`, the same key as `0.1` and `0.2`. Interact takes a stack.
- [x] Chest dock: a vanilla chest within 1.25 m of a port. Tombstones are excluded. A private chest works only for a machine built by the chest's owner. A ward that denies access pauses the dock.
- [x] Fixed bug: `0.2.0` stored the localisation token (`$item_wood`) as the prefab id, so a basket-to-chest move could never succeed. The prefab name is now read from `m_dropPrefab`.
- [x] Fixed bug: basket text unescaping could corrupt a crafter name or custom data containing a backslash. The codec now escapes character by character and is tested.
- [x] Destroying a machine drops everything it holds once, through `WearNTear.m_onDestroyed`, on the owner only. Unloading a machine does not.
- [ ] Two feeders stalling one crank inside a world.
- [ ] Save and reload of a placed line with items on belts, in a basket, and in a feeder's hand.
- [ ] A second client, or a dedicated server.

## M2 — native production line

- [x] Smelter-family adapter for `smelter`, `charcoal_kiln`, `blastfurnace`, `windmill`, `piece_spinningwheel`, and `eitrrefinery`. Fuel and input come from the station's own `m_fuelItem` and `m_conversion`. Other `Smelter` prefabs, including the Deep North ones, report "Unsupported adapter".
- [x] Insert calls `RPC_AddOre(prefab, cheated)` or `RPC_AddFuel` only while this peer owns the station, so the RPC runs locally and at once. The insert counts only if the station's queue or fuel on its ZDO actually rose. The item's cheated flag is passed through, not forced false.
- [x] Output collection takes only the station's own products within 1.2 m of its `m_outputPoint`, only while this peer owns the drop, and calls `RPC_EmptyProcessed` for windmills and spinning wheels. Timers, smoke, roof, and capacity stay native.
- [x] Splitter with fair, priority (manifold), and filter modes; merger with fair rotation. Both need a turning line.
- [x] Ownership epoch: a machine acts only from its second consecutive observation as owner, after reloading state the previous owner wrote.
- [ ] A kiln and a smelter run from belts and feeders in a world, at native rates, with smoke.
- [ ] A full output belt does not duplicate station output.
- [ ] Ownership handoff observed between two clients.

## M3 — recipe mill

- [x] Teaching: use a crafted item on the mill. The player must know that recipe (`Player.IsRecipeKnown`) and have ward access. Teacher id and name are saved and become the crafter of the output.
- [x] Recipes come from `ObjectDB` in the running game. Any-one-ingredient and upgrade-only recipes are refused with a reason. Recipes added by other mods through Jötunn are refused unless the server lists them in `ModdedRecipeAllowlist`.
- [x] Dynamic ingredients: 8 types on the mill, 12 on the assembler. Five-ingredient recipes are covered by a core test.
- [x] Station check each tick: the recipe's station within build range, at the required level, with the station's own roof and fire rules.
- [x] Persistent escrow: inputs move into escrow only when output room exists. Station loss, power loss, or reload pauses with escrow held. Recipe change and shift-interact cancel return escrow once, through the output.
- [x] Cycle `max(4, 4 + 2 × ingredient types)` seconds at 16 RPM, up to 2× faster on the mill and 4× on the assembler, never under 2 s.
- [x] Stock quotas with hysteresis count the output buffer plus the chest or buffer at the output port.
- [ ] Bronze nails and one cauldron or food-table recipe crafted in a world.
- [ ] Station removal, recipe change, full output, and cancellation observed in a world.

## M4 — renewable wood and stone

- [x] Bedrock quarry: 32 DU, one stone per 10 working seconds, holds 50, natural terrain only, not underwater, not in a dungeon, 12 m from another quarry. Built at a stonecutter. The server preset can turn it off.
- [x] Managed coppice: five beech seeds, pine cones, or birch seeds or acorns found it once. The species is fixed by the first seed. The cycle is the longer of the master prompt's minimum and the sapling's native `m_growTimeMax`. It needs open sky, natural ground, the species' biome, and 8 m from another bed. Common coppice can switch to resin tapping between cycles.
- [x] Timber saw: harvests mature beds within 6 m as a whole batch, or not at all. Inside a plot marked by two or more of the saw builder's stakes, it cuts trees and logs with native chop damage and tool tier, collects the native drops near what it hit, and replants felled trees from seeds in its input.
- [x] Deterministic spacing: when two sites are too close, the one with the lower ZDO id works and the other is blocked.
- [ ] A wood and stone loop run for an hour in a world.
- [ ] Rebuilding a bed or quarry does not refund founding stock or reset a site.

## M5 — fields and kitchens

- [x] Planter (4 × 4 m), harvester (4 × 4 m), farm gantry (8 × 8 m). Planting instantiates the cultivator's own sapling prefabs after checking cultivated ground, biome, and grow space. Harvesting calls the pickable's own `RPC_Pick` and collects its drops.
- [x] Field policies: replant, seed field, food field. The gantry reserves the next planting plus 10 % before exporting.
- [x] Cooking tender for `piece_cookingstation`, `piece_cookingstation_iron`, and `piece_oven`. It unloads done or burnt food through `RPC_RemoveDoneItem`, and loads raw food only with fire or fuel, a free slot, and room in storage for the cooked result. A stalled tender leaves food on the heat, so it can burn. An oven extension within 3 m extends reach.
- [x] Fermenter: loads one mead base when empty and taps when the native timer is past `m_fermentationDuration`.
- [x] Beehive and bird nest: extract only the native level. Sap collector: the same.
- [ ] A carrot seed field feeding a carrot food field in a world.
- [ ] A mead base crafted by a mill, fermented, and tapped in a world.
- [ ] Harvesting waits for a local player object because `Pickable.RPC_Pick` reads `Player.m_localPlayer`. On a dedicated server the harvester pauses with that reason.

## M6 — steam, sail, governor, ratios, flywheel, filters

- [x] Steam engine 160 DU at 32 RPM, one coal per 30 running seconds. Reinforced steam engine 320 DU at 64 RPM, one coal per 15 s. Both need a water intake whose probe is in real water within 4 m, and a clear chimney.
- [x] Eitr motor 320 DU at 64 RPM, one refined eitr per 120 running seconds.
- [x] Paid running time is on the engine's ZDO. A stall or reload does not take a second fuel item. Manual fuelling by using fuel on the engine always works.
- [x] Sail wheel 32–256 DU at 32 RPM from `EnvMan.GetWindIntensity()` and the share of eight 6 m rays from the sail that are clear. Below 10 % effective wind it gives nothing.
- [x] Governor fixes the line at 8, 16, 32, or 64 RPM. Without one, sources at different speeds stall the line as a rotation conflict.
- [x] Gear ratios and reversal on edges. A contradictory loop stalls the whole line as a rotation conflict.
- [x] Flywheel: 2400 DU·s, 80 DU charge and discharge, 80 % charging efficiency, energy stored on the wheel.
- [x] Filters on feeders and splitters, stock targets on feeders and mills.
- [ ] Any of these in a world.

## M7 — later extraction

- [x] Mining head: pickaxe damage at the configured tier (default 2) on the `MineRock` or `MineRock5` in front of it. Depletion and drops stay native. Dungeon interiors are excluded.
- [x] Deep extractor: allowlist of copper ore and tin ore after Moder in the Black Forest, scrap iron after Yagluth in the Swamp, silver ore after Fader in the Mountains. Ten samples commission it once, not refunded. 32 m spacing, capacity 20, no RPM speed-up, no offline catch-up. Unknown outputs are denied.
- [x] Sap and eitr: sap collector extraction and the eitr refinery through the smelter-family adapter.
- [x] Advanced assembler with upgrade mode: one existing item plus the next level's resources, identity kept, one level at a time, station level checked.
- [ ] Tin extraction is checked for Black Forest biome, not for coastline.
- [ ] Any of these in a world.

## M8 — broader supply and release hardening

- [x] Forage beds for raspberries, blueberries, mushrooms, thistle, and cloudberries (after Moder). Five specimens found a bed. One item per 10 minutes or the native respawn, whichever is slower. Holds five for a feeder or harvester.
- [x] Livestock feed gate: releases one real food item when a tame animal within 10 m is hungry and none is on the pad.
- [x] Culling gate: off by default. Only unnamed tame adult boars, keeping 2–8 adults. Native damage and drops, no attacker, so no skill gain. Pregnancy is not read, because the game keeps it private.
- [x] Fishing winch is not registered. No safe native capture path was found. The model stays a file.
- [x] `docs/AutomationCoverage.csv`: 969 items. Renewable chain 28, native renewable 71, finite supply with automated processing 181, creature supply 434, trader supply 118, manual or unique 92, unsupported pending adapter 45. The in-game `vf coverage` builds the same report from live objects.
- [x] Package: `scripts/package.sh` stages `artifacts/VikingFactory-0.3.0.zip` with real dependencies, passes `validate-package.sh`, and loaded 39 of 39 pieces headless when installed into the game.
- [x] Other automation mods are logged by name at startup. Nothing is patched out.
- [ ] Dedicated-server soak. `valheim_server.x86_64` is still not installed here.
- [ ] 500-piece factory profiled with two clients. `vf status` reports step time.
- [ ] A license file. There is still none.

## Tests

`dotnet test` on 27 September 2026: 66 passed, 0 failed. These do not load Valheim.

- [x] Crank stall, clutch, disabled consumer, water probe, version match (from `0.2.0`)
- [x] Supply equals demand; one DU short stalls
- [x] Direction, reversal, 2:1 ratio, contradictory ratio loop, source against the network, mismatched source speeds, governor
- [x] Flywheel charge, discharge, cap, stall keeps energy, energy conserved on split
- [x] Fuel taken only when paid time is spent; stall and reload keep paid time; empty fuel gives nothing
- [x] Sail capacity curve
- [x] Item codec round-trip with awkward text; no merge across quality or cheated flag; full store refuses
- [x] Belt order, rate, power, backpressure, save and load
- [x] Splitter fair, priority, filter; merger rotation; stock hysteresis
- [x] Recipe mill: nails, five ingredients, bounded buffers, station loss, power loss, full output, recipe change, cancel, quota, speed cap, cheated output, save and reload in escrow, missing recipe on load, destruction drain
- [x] Quality upgrade keeps identity and checks level
- [x] Quarry rate and cap, spacing, progression fail-closed, extraction allowlist, commissioning, coppice growth, mode switch, forage allowlist, seed reserve, culling, feed, clock drops sleep jumps, ownership handoff, schema, presets, coverage classifier
- [ ] Delayed, duplicated, or out-of-order messages across peers
- [ ] Disconnect and reconnect mid-transfer
- [ ] 500-piece factory, two clients, dedicated server

## Thunderstore roadmap

- [x] Phase 0: versions, scripts, compatibility notes, and the plugin logs its version in the game
- [x] Phase 1, partial: pieces load from the packaged ZIP
- [ ] Phase 1 gate: one machine seen by another client and restored after save
- [x] Phase 2 to 4, partial: code and core tests for power, items, native processing, recipe mill, wood, and stone
- [ ] Phase 2 gate: two-client item races
- [ ] Phase 5 private alpha package: a ZIP exists and loads; it has not passed a world or multiplayer check
- [ ] Phases 6–12

Release checklist:

- [x] Build succeeds with the local SDK and `Environment.props`
- [x] Automated core tests pass
- [ ] No known in-game loss or duplication. Not tried in a world.
- [ ] Save, owner handoff, and destruction tested in a world
- [ ] Dedicated server and two clients
- [ ] Native processing checked in a world
- [ ] Large factory profiled
- [ ] Asset bundles. Not needed while pieces load GLBs.
- [x] Install of a package: the ZIP installed and loaded headless. [ ] Upgrade from `0.2.0` over a saved world.
- [x] 256×256 `icon.png`, matching versions, and a ZIP that passed `scripts/validate-package.sh`

## Exact next task

Work through `docs/QA-Checklist.md` in the isolated world, in order. A placement pass already happened. Next is step 1 as a look, not another drop: stand at the new water wheel and record whether its feet touch the ground, whether the old floating wheel is still there, and whether the hammer buttons are the rendered pictures. Then build the M1 crank line, save, quit, reload, and confirm the items on belts and in baskets are where they were.

Do not publish.
