# Implementation status

Updated 27 September 2026. This is the living cross-off list. The master prompt and the Thunderstore roadmap stay the target design. A checked box means the work exists in this repository or was observed on this machine.

A windowed client on 27 September 2026 loaded `VikingFactory 0.2.0`, entered an isolated world, and placed one water wheel. That place used Valheim's water-piece flag, so the wheel sat about 3 m above the ground and dry ground required Shift. That flag is no longer set. The hammer menu lists only pieces the character has discovered, so a new character with `debugmode` off sees Repair and an empty grid. `debugmode` does not persist between launches. Hammer buttons now use rendered pictures of the machines. The corrected placement and those pictures have not been checked in a world. Appearance, rain, torchlight, save/load of a corrected piece, and a second client are unverified. The public summary is `ROADMAP.md`. Setup is `docs/Development.md`.

## Milestone gates

- [x] M0 scaffold is in the repository: versions, core tests, build, deploy, and an inert piece in code
- [x] M0 load: `BepInEx/LogOutput.log` shows `VikingFactory 0.1.0 loaded` and `Registered vf_marker. Valid=True` through `Registered vf_basket. Valid=True`
- [ ] M0 world: an isolated world exists. One water wheel was placed on 27 September 2026, before the placement fix, and it floated. The corrected place has not been repeated, and save/load of that piece was not confirmed beyond the quit-time world write.
- [x] M1 machines are registered in code: crank, shaft, clutch, water wheel, belt, feeder, basket
- [ ] M1 gate: two feeders stall a crank in a world, items survive save and reload, direction is visible
- [ ] M2 native kiln and smelter line, splitter, and merger
- [ ] M3 recipe mill
- [ ] M4 renewable wood and stone
- [ ] M5 fields and kitchens
- [ ] M6 steam, sail, governor, ratios, and flywheel
- [ ] M7 later extraction
- [ ] M8 livestock, coverage report, dedicated-server soak, and a tested package

## M0 — audit and scaffold

- [x] Valheim is installed. Unity `6000.0.75f1`, Steam build `25527674`. See `docs/Compatibility.md`.
- [x] BepInEx pack 5.4.2351 and Jötunn 2.30.2 are in that game folder.
- [x] Local paths live in gitignored `Environment.props`. Game DLLs are not in the repository.
- [x] `scripts/build.sh`, `scripts/test.sh`, `scripts/deploy-dev.sh`, and `scripts/validate-package.sh` exist.
- [x] Deploy refuses an empty path and any `Managed` folder, and copies `VikingFactory.dll` plus `VikingFactory.Core.dll`.
- [x] `VikingFactory.Core` is `netstandard2.0` and has no game references.
- [x] The plugin targets `net472` and compiles against the local assemblies, including `assembly_utils` for `Vector2i`.
- [x] `vf_marker` is registered: hammer, workbench, Crafting category and Crafting usage tag, 2 Wood, a ZNetView, and a rendered picture in `VikingFactory-Assets/polished/icons/vf_marker.png`.
- [x] Diagnostics print the core version and whether `assembly_valheim.dll` is present. The live catalogue is written by the plugin, not by that tool.
- [x] Startup line confirmed. A headless launch on 27 September 2026 wrote `VikingFactory 0.1.0 loaded` and `Registered vf_marker. Valid=True` in `BepInEx/LogOutput.log`. The same launch registered the crank, shaft, clutch, water wheel, belt, feeder, and basket, each `Valid=True`.
- [x] Live catalogue export. That launch wrote `BepInEx/config/VikingFactory/catalogue.json`: 1522 items, 481 recipes, 9 smelters, 4 cooking stations, 1 fermenter, 22 plants, 2 beehives, 1 sap collector, 84 pickables, and 662 pieces. `ZNetScene` was not created, because no world was loaded. The lists came from the prefab cache and `ObjectDB`.
- [x] Isolated test world. `scripts/launch-dev.sh` starts the client with its own save directory, and `client2` is a second directory. The primary directory has a character and a world. `client2` was still empty. This install has no `valheim_server.x86_64`, so `scripts/launch-dev-server.sh` cannot create a server world. Existing play worlds were not modified. The server script reads `VF_DEV_SERVER_PASSWORD` from the environment.
- [x] `scripts/validate-package.sh` passes. `packaging/icon.png` is a flat 256×256 mark. The manifest is still not an upload: empty website, empty dependencies, and no tested ZIP.

## M1 — power and logistics

The placed workshop pieces load polished GLBs and cloned vanilla materials. They use the hammer's Crafting tag. Cog, corner conveyor, splitter, trough, recipe mill, and quarry have polished files and are not hammer pieces. One water wheel has been placed. It floated. The rest has not been looked at in a world.

- [x] Hand crank, `vf_crank`. 6 Wood, 2 Leather Scraps. 8 DU only while interact is held. The mesh is `vf_hand_crank`. A headless load put Crank at `(0, 1.05, 0)` and Kinetic_Out at `(-0.375, 1.05, 0)`. That output is the forward port. The old cube ports are gone.
- [x] Wooden shaft, `vf_shaft`. The placed prefab id stays `vf_shaft`. The mesh is `vf_shaft_2m`. 2 Wood. Zero load. The same load put the Rotor at `(0, 0.5, 0)`, Kinetic_In at `(0, 0.5, 1)`, and Kinetic_Out at `(0, 0.5, -1)`. That Z order is the loader's mirror of the authored ends.
- [x] Clutch, `vf_clutch`. 4 Wood, 1 Bronze. Interact stores closed state on ZDO key `vf_clutch`. Closed means the forward branch is disconnected. The pack had no clutch mesh; this one was built for the placed piece. Its ports are on the shaft axle, `y = 0.5`, `0.35` m from the centre. The old cube ports were at `y = 0`.
- [x] Water wheel, `vf_water_wheel`. 30 Wood, 10 Round Log (`RoundLog`), 4 Bronze, 4 Deer Hide. The headless load attached the polished GLB: Rotor `(0, 2, 0)`, kinetic markers at `x = -1.25` and `x = 1.25`, both at axle height `y = 2`. Oak and OakLight cloned `woodwall_worn` (`Custom/Piece`), Iron cloned `Ironbeam_mat`, Bronze cloned `PotsNpans_mat`, Stone cloned `stonekit_floor_interior` (`Custom/StaticRock`). The support colliders stay on Static. A placed copy on 27 September 2026 used `m_waterPiece`. Valheim then refuses dry ground unless Shift is held, and it forces the pivot 3 m above the surface. The log recorded that wheel 2.9 m above the ground. `m_waterPiece` is no longer set, so the feet should sit on the hit surface. That corrected place has not been repeated. Grain, lighting, rain, and motion are unverified.
- [x] Timber belt, `vf_belt`. 4 Wood, 2 Leather Scraps, 2 Bronze Nails. Reserves 1 DU. The mesh is `vf_conveyor_2m`. Power ports are 1 m from the centre at axle height `y = 0.5`, matching the shaft. The hide deck is a separate surface that scrolls only while the line is turning, and it does not move items. Rollers spin on local X.
- [x] Bronze feeder, `vf_feeder`. 6 Wood, 2 Bronze, 2 Leather Scraps. 6 DU. One item every 2 seconds while the line is turning, and only if this peer owns it. Back is Pickup and forward is Dropoff, both at `y = 0.68` and about 1.08 m from the centre after the loader's Z mirror. The arm swings on Y and does not move items by itself.
- [x] Catch basket, `vf_basket`. 6 Wood, 2 Leather Scraps. Up to 8 stacks on ZDO key `vf_items`. Colliders are the floor and walls, with the front left open.
- [x] Chest dock: a vanilla container within 1.25 m of the feeder's back or forward port.
- [x] Item copy keeps durability, quality, variant, crafter, world level, custom data, and `m_cheated`.
- [x] Hover text shows supply, reserved load, and why the line is stopped.
- [x] Fixed 16 RPM in `BalanceDefaults`. Ratios are not implemented.
- [ ] Crank stamina, 3 per second. No public stamina method was used.
- [ ] Water-wheel spacing, immersion, and terrain volume. Placement no longer uses the water-piece flag. The simulator still treats every placed wheel as a valid water site.
- [ ] The Rotor spin has not been watched in a world. The code turns it at 16 RPM while the line is turning.
- [ ] Hammer pieces for the expansion machines. `VikingFactory-Assets/expansion/` has 26 individual GLBs. Blender 5.1.1 opens each one. UVs are about 0.5 m per repeat so the cloned Valheim shaders tile with the timber and iron already used by the polished pieces. They are not registered, and they have not been loaded in a world. Polished GLBs for the cog, trough, splitter, corner belt, recipe mill, and quarry remain in `VikingFactory-Assets/polished/` and are also unregistered.
- [ ] Belts that move a queue of items.
- [ ] Two feeders stalling one crank inside a world.
- [ ] Save and reload of a placed line.
- [ ] A second client, or a dedicated server.

Both DLLs, `vf_shaft_2m.glb`, and `vf_water_wheel.glb` are in `BepInEx/plugins/VikingFactory/`. The headless load attached those two models. No piece was placed.

## Prototype asset pack

The prototype pack stays in `VikingFactory-Assets/models/` and `VikingFactory-Assets/converted/`. Polished sources are in `VikingFactory-Assets/polished/`. Blender 5.1.1 executed `build_water_wheel.py` and `build_pack.py`. A later headless load attached the crank, shaft, clutch, water wheel, belt, feeder, and basket, and resolved `woodwall_worn`, `Ironbeam_mat`, `PotsNpans_mat`, `Tanningrack_mat`, and `stonekit_floor_interior`. The GLBs contain no images.

Unity `6000.0.75f1` (`26349cd2a5c8`) matches the game player. A local install of that editor, including Linux IL2CPP build support, printed `6000.0.75f1` from `Unity -version`. A Unity Personal license is active inside the Flatpak Hub and grants the editor window. Host batchmode on 27 September 2026 still exited 198: that launch does not see the Hub license, and Personal does not include `com.unity.editor.headless`. Prefabs and AssetBundles are still blocked. An older `6000.6.3f1` editor is not the one to use for Valheim bundles. The placed pieces do not wait on a bundle. They load GLBs. Hammer pictures are PNGs rendered from those GLBs, not a Unity import.

## M2 through M8

- [ ] M2 kiln and smelter adapters, fuel versus ore, output collection, splitter and merger
- [ ] M2 gate: native rates, smoke, no duplicated output, authority change
- [ ] M3 recipe teaching, dynamic ingredients, station in range, escrow, quality 1, quotas
- [ ] M3 bronze nails and one food recipe, including a five-ingredient recipe
- [ ] M4 bedrock quarry, plantation harvest, managed coppice, reserve routing
- [ ] M5 planter, harvester, hives, fermenter, cooking rack, oven
- [ ] M6 steam, sail, governor, gear ratios, flywheel, filters
- [ ] M7 finite mining, deep extraction, sap, eitr, advanced assembler
- [ ] M8 forage, livestock, upgrades, compatibility profiles, coverage report
- [ ] Harmony patches into smelters, recipes, or inventories
- [ ] Sounds, a Unity prefab, or an AssetBundle. The placed machines have polished GLBs and 256×256 hammer pictures in `VikingFactory-Assets/polished/icons/`. Those pictures have not been seen in the build menu.

## Tests

`dotnet test` on 27 September 2026: 13 passed, 0 failed. These do not load Valheim. One test checks that `Version.props`, the plugin constant, and the package manifest are the same number.

- [x] One crank runs one feeder and stalls on two
- [x] A released crank provides no drive
- [x] Shafts add no load
- [x] A closed clutch drops the forward branch
- [x] A disabled consumer releases its reservation
- [x] A water wheel with no placement probe supplies nothing
- [x] A verified water wheel supplies 48 DU and reserves 22 DU on the documented kiln line
- [x] Two feeders do not duplicate the last item
- [x] Removing the item after planning blocks the transfer
- [x] A full destination after escrow keeps the item for one recovery, including the cheated flag
- [x] Save and reload of the in-memory snapshot keeps escrow, quality, custom data, and crafter
- [x] Different qualities do not merge
- [ ] Delayed, duplicated, or out-of-order messages
- [ ] Disconnect and reconnect
- [ ] Save and reload inside the game
- [ ] Machine destruction while holding items
- [ ] Station input that was not acknowledged
- [ ] Ratio loops, flywheel energy, or fuelled generators
- [ ] 500-piece factory, two clients, dedicated server

## Thunderstore roadmap

- [x] Phase 0, partial: versions, scripts, and compatibility notes
- [ ] Phase 0 gate: the plugin logs its version in the game
- [x] Phase 1, partial: primitive pieces copied into the plugins folder
- [ ] Phase 1 gate: one machine seen by another client and restored after save
- [x] Phase 2, partial: power graph and feeder code, covered by the core tests above
- [ ] Phase 2 gate: two-client item races
- [ ] Phase 3 native processing and recipe mill
- [ ] Phase 4 renewable wood and stone
- [ ] Phase 5 private alpha package
- [ ] Phase 6 fields and food
- [ ] Phase 7 public alpha
- [ ] Phases 8–12 industry, art, beta, 1.0, and maintenance

Release checklist, still open until a candidate exists:

- [x] Build succeeds with the local SDK and `Environment.props`
- [x] Automated core tests pass
- [ ] No known in-game loss or duplication. One wheel was placed and the world was saved on quit. Item transfer was not tried.
- [ ] Save, owner handoff, and destruction tested
- [ ] Dedicated server and two clients
- [ ] Native processing checked
- [ ] Large factory profiled
- [ ] Asset bundles
- [ ] Install and upgrade of a package
- [ ] 256×256 `icon.png`, matching versions, and a ZIP that passed `scripts/validate-package.sh`

## Exact next task

Do not point bundles at a newer editor than `6000.0.75f1`. The world check does not need a bundle. Open that editor from the Flatpak Hub if a project is required. Host batchmode still has no headless entitlement.

The isolated world already exists. `debugmode` resets every launch. In the world, press F5 and run `devcommands`, then `debugmode`, or the hammer grid stays on Repair. Remove the floating wheel and place a new one from the Crafting tag. Its feet should meet the ground without holding Shift. Confirm the hammer button shows the rendered picture. Then place the shaft, belt, feeder, and basket beside a wood wall and a workbench. Compare daylight, rain, torchlight, and indoor light. Confirm the wheel frame stays still, the belt deck scrolls only while the line turns, and a save still contains the pieces.

Do not publish.
