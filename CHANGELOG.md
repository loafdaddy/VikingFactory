# Versions

Patch notes for each VikingFactory update. This is not a player release. A version number means the source on that branch, not a Thunderstore package.

`Version.props` is the number. The plugin constant and `packaging/manifest.json` match it. `main` keeps the last snapshot that was pushed. The next update gets the next number and its own branch.

## 0.3.0

27 September 2026.

The code for every milestone, M0 through M8. Nothing in this update has been placed in a world yet. A headless launch registered all 39 pieces, and a second headless launch did the same from the packaged ZIP. `docs/QA-Checklist.md` is the in-world test script.

Power
- Direction and speed per shaft segment. Spinning parts show which way they turn, and hover says clockwise or counter-clockwise.
- Rope drive, bronze cog (1:1, 2:1, 1:2), and reversing cog. A contradictory ratio loop or sources turning against each other stall the line as a rotation conflict.
- The hand crank costs 3 stamina a second.
- The water wheel now needs its paddles in real water at least 0.5 m deep, no other wheel within 6 m, and nothing built in its way.
- Steam engine, reinforced steam engine, eitr motor, and water intake. Fuel buys running time, and a stall or reload does not burn a second item.
- Sail wheel from wind and exposure, governor, and flywheel.

Items
- Belts, corners, iron belts, and gravity troughs carry a real queue of items and back up when full.
- Iron feeder, splitter (fair, priority, filter), and merger. Feeders take ghost filters and stock targets.
- Feeders load and unload kilns, smelters, blast furnaces, windmills, spinning wheels, and eitr refineries through the station's own RPCs. An insert only counts if the station's queue or fuel rose.
- Fixed: the feeder stored `$item_wood` instead of `Wood`, so a basket-to-chest move always failed.
- Fixed: basket text with a backslash could come back wrong after a reload.
- A destroyed machine drops what it held, once.

Crafting
- Recipe mill and advanced assembler. Teach a known recipe by using its product on the mill. The real station must be in range at the right level, with its roof and fire. Ingredients wait in escrow and return once on cancel.
- The assembler's upgrade mode raises one existing item one quality level.

Land and livestock
- Bedrock quarry, managed coppice with resin tapping, and a timber saw for coppice and staked plantations.
- Planter, harvester, and farm gantry with seed reserves. Cooking tender and oven extension. Fermenter, beehive, and sap collection.
- Mining head, deep extractor for older ores after the right boss, and forage beds.
- Livestock feed gate and an opt-in culling gate for surplus tame boars.

Tools
- `vf status`, `vf network`, `vf exportcatalog`, `vf coverage`, `vf validate`, and admin-only `vf recover`.
- Server-synced preset (Balanced Industry or Vanilla Supply), quarry switch, tool tiers, and a modded-recipe allowlist.
- `docs/AutomationCoverage.csv` classifies all 969 items.
- `scripts/package.sh` stages a test ZIP. The manifest now lists BepInExPack 5.4.2351 and Jötunn 2.30.2. It is still not uploaded.
- Hammer pictures for every piece.

Still open: every in-world gate, two clients, a dedicated server, and a license file.

## 0.2.0

27 September 2026.

A windowed client entered an isolated world and placed one water wheel. The wheel floated, and the hammer grid looked empty the next time it was opened. This update is the correction for those two findings, plus pictures on the hammer buttons.

- The water wheel no longer uses Valheim’s water-piece flag. That flag rejects dry ground unless Shift is held, and it forces the pivot 3 m above the surface. The placed wheel was 2.9 m up. The feet should now sit on the ground. That new place has not been repeated in a world.
- Hammer pieces use the Crafting tag. A custom Workshop category left the build menu on a blank page.
- The hammer only lists pieces the character has discovered. A new character sees Repair, so the grid looks empty. `debugmode` shows every piece and resets every time the game starts.
- Each registered piece has a 256×256 picture rendered from its model. The marker is a timber block. Those pictures have not been seen in the menu yet.
- `Version.props` is the single version number. A test fails if the plugin constant or the package manifest drifts from it.

Still open: spacing and immersion for the wheel, rain and torchlight, a feeder moving an item, save and reload of a placed line, and a second client.

## 0.1.0

27 September 2026. First public snapshot.

- Eight hammer pieces: workshop marker, hand crank, wooden shaft, clutch, water wheel, timber belt, bronze feeder, and catch basket.
- Polished models for those machines, loaded from GLB files. Material names are swapped at runtime for cloned Valheim shaders. The game’s textures are not in the repository.
- A core simulation, covered by tests that do not start Valheim: one crank runs one feeder and stalls on two, a closed clutch drops the forward branch, and item moves keep durability, quality, and the cheated flag.
- Twenty-six later machines exist as files only. They are not hammer pieces.
- Public README, roadmap, and contribution templates. No license file, and no player package.
