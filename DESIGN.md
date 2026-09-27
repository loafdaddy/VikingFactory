# VikingFactory

Earlier outline, kept as a design note. It is not a status page.

Where this file disagrees with `docs/VikingFactory-Master-Prompt.md`, follow the master prompt. Renewable wood and stone, including a managed coppice and a quarry, are in scope there. What is built is `docs/ImplementationStatus.md`. The public summary is `ROADMAP.md`.

Valheim workshop mod. Buildables that move and tend items the game already has. No new ores, foods, or gear. Vanilla stations keep their timers.

Research snapshot: September 2026. Valheim 1.0 is current. Stone portals exist (Ashlands). Smelter and cooking RPCs take a `cheated` flag.

## Thesis

Create contributes the kinetic network: generators, shafts, strain, and a visible stall when load exceeds drive.

Satisfactory contributes logistics: directed belts, input and output faces, and manifolds. One line along a row of machines. A full machine stops accepting, and the rest overflows downstream.

Factorio contributes the inserter: one item per swing, a filter, and no move unless the target has room and the machine has power.

Valheim contributes the world rules those machines have to respect: wind, smoke, cover, roofs, biome crops, portal metal limits, and the fact that a base only runs while a player is nearby.

Invisible radius automation already exists (LazyVikings, Automatics, ServersideQoL AutoProcess). Physical hoppers without a power cost already exist (ItemHopper, ValheimPipes). The gap is a strain network. Feeders only move while a shaft is turning.

## Hard rules

1. Never change vanilla processing time, fuel cost, crop growth, fermentation, or bee happiness.
2. Auto-crafting runs vanilla recipes only, on a buildable recipe mill with belt ports. The mill crafts only while its shaft turns, only from items in its input buffers, and only when a real crafting station of the required type and level is in range. The player still has to know the recipe. Repair stays a manual use of the station.
3. Never chop trees, mine ore, or carry ore through a wooden portal.
4. RPM changes how often a feeder swings. It does not change how long a smelter takes to make a bar.
5. Routing pieces (shafts, rope, cogs) cost no load. Create learned that punishing pretty routing feels bad.
6. Two networks. Turn and items stay separate. A belt does not also transmit rotation.

## Turn network

Drive comes from generators. Load comes from feeders, belts, recipe mills, the reaper, and the spit tender. If load exceeds drive, the whole connected network stalls.

Speed is a separate number from strain, and strain does not scale with RPM. That scaling is the part of Create players bounce off. Show one line on interact: wind percent, drive, load, turning or stalled.

Sources:

- Hand crank. Burst power for testing one machine. Wood, workbench.
- Water wheel. Spins while it touches water. Valheim has no flow vector, so there is no "wrong direction current." Placement against water is the rule. Slower or stopped when frozen, if the game treats the spot as cold.
- Sail wheel. Unlocked with the materials you have after Moder, when windmills exist. Reads the same wind the ship and the windmill use. Several sources on one network add drive. A source that would spin against the network does not connect.

Conveyors of rotation: wooden shaft, rope drive between two shafts, bronze cog (including one 2:1), clutch on a lever. Iron shafts later for length, not for a new power tier fantasy. One gearbox puzzle is enough. Sequenced gearshifts can wait forever.

Networks are rebuilt from snap adjacency when pieces load. Do not persist a graph. Only the ZNetView owner simulates. Clients see rotation as a visual driven by a replicated "turning" and RPM on the generator.

## Item network

- Gravity trough. Early, slow, downhill.
- Iron belt. Direction and a rate. Items are visible.
- Splitter and merger. Splitter overflows when an output is full, which is what makes a manifold work without ratio math.
- Feeder. The inserter. Filter remembers the last item. One item per cycle. Cycle time shortens with RPM down to a cap. Will not pull an item it cannot place.
- Catch basket, or any vanilla chest the feeder is snapped to. Prefer snapping over a search radius. Radius search is the mod we are not making.

A smelter row is a manifold: belt, splitter or side feeder at each ore port, another for coal, catch at the chute. First smelters fill first. That warm-up is fine. Satisfactory's buffers are why manifolds work, and Valheim stations already have buffers (10 ore, 20 coal).

## Recipe mill

One hammer piece, not a new crafting tree. It is the Satisfactory constructor shape: dedicated input faces, one output face, one selected recipe.

Ports use the same snap contract as troughs, belts, and feeders. A belt snapped to an input face fills that buffer. A belt snapped to the output face takes the product. A feeder can do either job if a direct belt run does not fit the hut. A full output belt pauses the mill, so a manifold of mills behaves like a manifold of smelters.

The mill reads `Recipe` from `ObjectDB`. The player sets one known recipe (`Player.IsRecipeKnown`). Each cycle checks a placed crafting station of `m_craftingStation` within station range, at `m_minStationLevel` or better, the same check the player gets when standing there. Workbench recipes need a workbench. Forge recipes need a forge at that level. Cauldron, mead ketill, food preparation table, stonecutter, artisan table, black forge, and galdr table are the same rule. No station in range, no craft.

Four input faces cover vanilla recipes, which are almost always four ingredients or fewer. Each face can filter to one item. A row of arrow mills is a wood belt and a resin belt, splitters tapping each mill, and one output belt of arrows.

Vanilla crafting is instant, so the mill's cycle is the cost of automating it. Base 4 seconds plus 1 second per ingredient type. RPM shortens the cycle down to a 2 second floor. The mill costs more load than a feeder, so a line of them needs a real water wheel or sail, not one crank.

Quality is a setting on the mill. Ingredient counts follow the recipe: base amount plus `amountPerLevel` for each quality above 1. Default quality is 1. Repair is still the player at the station.

A full line the belts make possible: ore and coal into smelters, bars onto a belt, recipe mill turns bars and wood into nails, nails onto another belt. Mead bases crafted at a mill go by belt into a fermenter feeder. The fermenter timer stays two in-game days.

## Vanilla stations

Smelter, kiln, blast furnace, windmill, spinning wheel, and eitr refinery are all the `Smelter` class. Input is the add-ore path, fuel is add-fuel, output is `m_outputPoint`. Call those RPCs the way the player does. Pass `cheated: false`. On 1.0, `Smelter.RPC_AddOre` and `CookingStation.RPC_AddItem` both grew that parameter.

| Station | Prefab | Time | Capacity | Do not ignore |
| --- | --- | --- | --- | --- |
| Charcoal kiln | `charcoal_kiln` | ~15s | 25 wood | Smoke |
| Smelter | `smelter` | 30s, 2 coal | 10 ore, 20 coal | Ore left, coal right, chute front, smoke, stone or terrain |
| Blast furnace | `blastfurnace` | 30s, 2 coal | 10 ore, 20 coal | Both ports on the left. Black metal, flametal, petrified tissue |
| Windmill | `windmill` | 10s base, ~13s because its own blades cover 24–29% | 50 in, drops stacks of 50 | Wind 10–100% and cover from the top. Plains wind is stronger. Blades hit for 20 blunt |
| Spinning wheel | `piece_spinningwheel` | 30s | 40 flax | Drops linen as it finishes |
| Eitr refinery | `eitrrefinery` | 40s per sap | 20 sap, 20 soft tissue | Poison puddles. 20 tissue consumes about 19 sap |

Cooking station (`piece_cookingstation`): most meat 25s, hare 60s, needs a fire underneath, becomes coal if left. Iron cooking station adds the later meats. A spit tender pulls the cooked item. If the network stalls, the food burns. That is the failure state, and it is good.

Fermenter: two in-game days, roof and 70% cover, breaking it deletes the batch. Load and unload only.

Beehive: about one honey per 20 real minutes, holds 4, sleeps at night and in rain, wants under 60% cover, Meadows, Black Forest, or Plains. Pull honey. Do not override happiness.

Sap extractor (`piece_sapcollector`): on ancient roots, 1 sap / 60s, holds 10.

Crops from the cultivator, roughly 4000–5000 seconds, biome and cultivated soil enforced by `Plant`:

- Replant from the yield: flax, barley, jotun puffs, magecaps.
- Need a seed chest: carrot, turnip, onion, and their seed crops. Harvesting a carrot does not give a carrot seed.

Cauldron, mead ketill, food preparation table, forge, workbench, stonecutter, artisan table, black forge, and galdr table stay the stations that unlock a recipe. The recipe mill, sitting in range of that station, is what runs it from belts.

## World rules

Loaded area. Nothing ticks far from players. Do not keep zones awake in v1. Sleep already accelerates loaded smelters. Feeders must advance on game time (`ZNet` time), not only `Time.deltaTime`, or sleep empties the station and the feeder never refills it until morning.

Wind ownership. On a server the windmill uses the environment of the client that owns the zone. The sail wheel uses that same read. Document it.

Smoke. Active smelters, kilns, and blast furnaces stop after about 4 seconds without being able to spawn smoke. Smoke wants about a meter of clearance. Feeders sit beside ports and leave the chimney alone.

Snap. 1 meter modules. Shaft ends are snap points. Jötunn `CustomPiece` from a vanilla piece or an empty primitive until real meshes exist. Hammer category `Workshop`.

Portals. Wood portals block ore, bars, and a short list (dragon egg, dvergr extractor, mechanical spring, Hildir's chests, charred cogwheel). Stone portals allow them, only when you enter the stone portal. Belts do not cross portals. Long hauls stay carts (18 slots) and ships until Ashlands.

Multiplayer. Owner simulates. Item transfer is an RPC, inventory removed on the source before the station add, matching the ItemHopper 1.0 fix. Config via ServerSync.

## Build order

1. Strain. Crank, shaft, rope, one feeder, one trough. A second feeder can stall the crank. Wood only enters a kiln while the shaft turns.
2. Smelter row. Ore feeder, coal feeder, catch, splitter. Timers and smoke unchanged.
3. Recipe mill. One known recipe, four belt inputs, one belt output, station-in-range check. First recipe to prove it: bronze nails, fed by a bar belt.
4. Water wheel and sail wheel. A row of mills should stall a lone crank.
5. Field reaper and seed chest. Harvest goes through the plant's own drops.
6. Adapters: bees, sap, fermenter, cooking pull, eitr, blast furnace. Fermenter input can be a mill's mead-base belt.

## Stack

BepInExPack Valheim, Jötunn, Harmony at the edges, publicized `assembly_valheim`. Register pieces on `PrefabManager.OnVanillaPrefabsAvailable`.

Adapters, not one patch class: `SmelterAdapter`, `CookingAdapter`, `FermenterAdapter`, `BeehiveAdapter`, `SapAdapter`, `PlantAdapter`. A new station is a new adapter. The recipe mill is not an adapter. It holds buffers and calls the same item consume and produce the player craft uses, after the station-range check passes.

## Out of scope

Create contraptions and pistons. They fight `WearNTear` support. Circuit logic and runestones. Cart winches. Portal item teleport. Changing comfort, skills, or boss progression. Chopping wild trees or mining a deposit by deleting it is out of scope. A managed coppice and a bedrock quarry are in the master prompt, and they are not built.
