# VikingFactory: research-backed Cursor master prompt

Prepared for Tyler, 25 September 2026. Based on the supplied VikingFactory outline.

## How to use this

Add this file to your VikingFactory repository as `docs/VikingFactory-Master-Prompt.md`. In Cursor Agent, say:

> Read @docs/VikingFactory-Master-Prompt.md and inspect the existing repository. Treat this as the target design, preserving working implementations wherever possible. Start with the repository audit and compatibility probe, then implement the first playable milestone. Make actual code changes, build and test what you can, and document anything that requires my installed game or an in-game test. Do not attempt the entire roadmap in one unverified rewrite. Keep a persistent implementation checklist so later sessions continue from the current state.

Everything under “BEGIN CURSOR BUILD PROMPT” is addressed to Cursor. The research notes distinguish externally supported facts from proposed mechanics. Numerical balance values are starting targets, not established Valheim statistics or playtested conclusions.

## Living checklist

This file is the target design, not the implementation record. Progress is `docs/ImplementationStatus.md`. The public summary is `ROADMAP.md`. Patch notes are `CHANGELOG.md`. A headless load registered the current pieces. A later windowed session placed one water wheel. The registered machines load polished GLBs, not the prototype pack.

## Research findings that change the original outline

1. **Target the installed game, not “1.0” in the abstract.** Iron Gate's current news page identifies patch 1.0.15, released 18 September 2026. That patch also discusses item metadata and the changed save format. Record the exact local assemblies and supported dependency versions before implementation. [S1]
2. **Do not hard-code `cheated: false`.** The ItemHopper compatibility maintainer reports changed smelter/cooking RPC parameters and preserves the input item's `m_cheated` value. The same report describes item loss from incompatible calls and checks source removal before sending. This is evidence for a compatibility probe, not proof that raw RPC forwarding is transaction-safe. [S2]
3. **Four ingredient slots are insufficient.** Jötunn's generated 1.0.7 recipe catalogue includes five-resource equipment recipes. It is also older than the current patch. Generate a catalogue from the running game and size logical buffers dynamically. Physical ports and ingredient slots are separate concepts. [S3]
4. **Build menus changed.** Jötunn documents usage tags in 1.0 and custom-category compatibility. Use the installed API to expose a VikingFactory tag instead of assuming legacy category behavior. [S4]
5. **Quality upgrades are a separate operation.** The recipe catalogue distinguishes initial and subsequent quality costs. Never create a higher-quality item by guessing a base-plus-per-level formula; consume an existing item and use verified upgrade semantics. [S3]
6. **Create is inspiration, not the specification.** Its published stress model scales with speed and leaves shafts/cogs without stress cost. VikingFactory deliberately keeps a simpler fixed-load model, with explicit speed limits to avoid free unlimited acceleration. [S5]
7. **Physical logistics already have precedents.** ValheimPipes has hoppers, pipes and configurable transfers; Automatics demonstrates broader automation. VikingFactory's distinguishing work is visible powered production, renewable supply chains, progression, and reliable multiplayer. Do not assume existing mods are current or reuse their code/assets without checking licenses. [S6, S7]
8. **Optimisation belongs in the architecture.** Factorio's developer discussion shows how much complexity arises from connected transport lines, sleeping systems, and topology changes. Use bounded work and event-driven invalidation, but do not attempt to port Factorio's engine. [S8]
9. **Configuration synchronisation is already available.** Jötunn documents server-enforced configuration. Pick one synchronisation mechanism rather than combining two competing systems. [S9]

The uploaded outline is a design input, not a verified API reference. Its specific environmental thresholds, station rates, capacities, freeze assumptions, sap/tissue orientation and sleep behavior must be checked locally. This prompt intentionally replaces blanket bans on forestry and mining to satisfy the expanded brief.

---

# BEGIN CURSOR BUILD PROMPT

## 1. Your role and required outcome

You are implementing **VikingFactory**, a Valheim automation mod inspired by Create, Satisfactory and Factorio. Work in this existing repository. Inspect before editing. Preserve useful existing code and public prefab identities. Produce a playable, maintainable mod through tested vertical slices.

The player fantasy is a believable Norse workshop becoming a working industrial settlement: turning water wheels, timber line shafts, clacking wooden arms, belts carrying ore, cultivated fields, managed forests, smoke from a furnace hall, and eventually expensive rune-driven machinery. It should look as though Valheim's inhabitants invented machinery using their own materials.

This is not just automatic chest access. Players design actual production lines with physical inputs, outputs, power distribution, storage and bottlenecks.

Primary goals:

- Automate wood, stone, crops, cooking, fuel, metallurgy, brewing, textiles, crafting and most sensible recurring supplies.
- Preserve the need to discover biomes, defeat progression bosses, find resources, purchase initial trader goods and build proper stations.
- Make throughput scale through construction and layout, rather than instant resource multipliers.
- Make renewable bulk materials satisfying without making rare exploration rewards meaningless.
- Support solo, hosted multiplayer and dedicated servers with the mod installed on all participating clients.
- Make item integrity, save compatibility and understandable failure states first-class requirements.

Do not describe unimplemented systems as completed. When a game installation, Unity editor or private assembly is missing, build and test the independent core, isolate the missing integration, and give an exact next action. Never invent game methods merely to make a code sample look complete.

## 2. First action: repository audit and compatibility probe

Before adding machines:

1. Read repository instructions, solution/project files, dependency declarations, asset pipeline, existing prefabs, patches, tests and docs.
2. Identify implemented, partial, conflicting and absent features against this document.
3. Record game version, assembly hashes, Unity version, framework target, BepInEx/Jötunn versions and supported platforms. Retain the working project's target unless verified compatibility requires a change.
4. Locate local game references using a configurable path. Do not commit/distribute proprietary game DLLs.
5. Inspect exact game signatures through permitted local assemblies and existing integration code. Verify station insertion, output collection, inventories, recipe creation/upgrades, environmental checks, item serialization and network ownership.
6. Create `docs/Compatibility.md`, `docs/ImplementationStatus.md`, `docs/Balance.md` and a short architecture decision record. Mark observations versus assumptions.
7. Add a diagnostic command to export live items, recipes, station conversions, plant relationships, relevant piece requirements and supported capability checks.
8. Compile a minimal piece and one safe container transfer before expanding.

Do not freeze development on missing optional capabilities. Disable an unsupported adapter with a specific reason while continuing independent work. However, do not enable any resource-consuming operation whose safe mutation path is unverified.

## 3. Product rules

### Preserve vanilla systems

Native smelting, burning, fermentation, refining, growth, breeding and respawn timers remain native. Their environment, fuel, health, space and biome checks remain native too. Automation operates these systems rather than multiplying their speed.

New VikingFactory machines have their own documented production times. Those machines may supply vanilla items, but must be visibly identified as mod-added production, not presented as vanilla behavior.

No automatic boss spawning or killing. No recipe discoveries from thin air. No ordinary belt crossing a portal. No replacement of an entire biome's progression with stone-to-everything transmutation. No passive skill farming from machines. Repair remains manual in the initial release.

### Separate three kinds of production

| Kind | What it does | Examples |
|---|---|---|
| Native operation | Handles existing world objects using their rules | Fill a smelter, harvest a mature crop, empty a hive |
| Recipe crafting | Consumes verified recipe inputs at the correct station | Nails, arrows, meals, mead bases, gear |
| New renewable industry | Deliberately adds a balanced supply mechanic | Coppice beds, bedrock stone quarry, late old-tier ore extraction |

Do not hide category three in patches to vanilla drop tables. Keep it inspectable, separately configurable and testable.

### Material policy

Use vanilla inventory materials for machine construction and outputs. Avoid a second inventory full of new gears, plates, rods, dusts and duplicate foods. Gears, rope and bearings are primarily buildable machine parts, paid for with existing materials.

Internal state such as rootstock, vein permits, stored mechanical energy and a transaction record is allowed. It is not a new tradable item unless a later design explicitly requires one.

### Two balance presets

- **Balanced Industry, default:** renewable wood/stone/agriculture and conservative late renewable copper, tin, iron and silver; other resources retain their natural sources.
- **Vanilla Supply:** identical automation, but disable new renewable ore extraction and artificial forestry/forage plots. Real planted-tree harvesting, native farming and finite mining remain available. Also allow disabling renewable bedrock stone independently.

Do not create an “everything from stone” preset as part of the main scope.

## 4. Progression and unlocks

Use actual material discovery, actual required stations and a server-side VikingFactory unlock ledger. Boss gates apply to mod machinery, not to unrelated vanilla recipes. Map boss milestones to verified runtime progression keys; do not invent key names.

Default unlock scope is the world/co-op settlement. A player who knows a recipe may teach it to an authorised recipe mill through a validated interaction. Store that taught recipe persistently so the mill can run when they log off. Never depend on `Player.m_localPlayer` in dedicated-server production. Existing taught recipes can be shared through a workshop ledger after validation. Individual progression servers may select per-owner permission mode.

| Stage | What the player gains | Why it belongs here |
|---|---|---|
| Meadows | Gravity trough, catch basket, hand crank, wooden shafts, manual clutch | Learn routing; no unattended industrial engine |
| Bronze / Black Forest | Water wheel, timber conveyor, bronze feeder, basic recipe mill, crop planter/harvester, timber saw | First useful complete production lines after acquiring metal |
| Iron / Swamp | Iron belt, stronger feeder, quarry, managed coppice, finite mining head, forage plots, steam engine | Bulk building resources and reliable powered workshops require infrastructure |
| Mountains | Precision gearbox, mechanical governor, flywheel, cold-region field hardware | Reliability and control rather than arbitrary new ore output |
| Plains / after Moder | Sail wheel, advanced assembler, farm gantry, livestock handling, old-tier copper/tin extraction | Large fields and factory scale after artisan progression |
| Mistlands | Eitr motor, advanced filters/sensors, sap handling, old-tier iron extraction | Expensive compact power and better control, not free eitr |
| Ashlands | Heat-resistant machinery, old-tier silver extraction, reviewed Ashlands adapters | Dangerous outposts and high-cost industry |
| Deep North / verified installed endgame | Adapter coverage for new stations, recipes and materials; optional capacity upgrades after audit | Do not invent northern systems or bypass unique processing |

Early machines remain useful. Higher tiers improve handling, flexibility and compactness; they do not turn a native smelter into a faster smelter.

## 5. Mechanical power

### 5.1 Core model

Two separate graphs: **kinetic connections** and **item connections**. A transport belt consumes power but does not itself count as a shaft. A rope transmission carries rotation but no inventory items.

Use **Drive Units, DU**, for capacity and load. These are game balance units, not real torque or watts.

- Each enabled connected consumer reserves its nameplate DU even if input-starved or output-blocked. This makes a network's requirement predictable and avoids load oscillation.
- A closed clutch disconnects a branch logically. A manually disabled machine releases its reservation.
- If available drive is below reserved load, all production on that kinetic component stops. No arbitrary silent priority cutting.
- Pure shafts, cogs and transmission rope have zero load.
- Direction and speed belong to each shaft segment, not one global RPM field on a generator.
- Multiple compatible generators add capacity; they do not add RPM.
- A reversing/ratio loop that imposes contradictory speed constraints is invalid and stalls with a highlighted conflict.
- Split and merge networks deterministically after placement, removal, clutch changes, loading or destruction.

Use a deliberately simplified fixed-load model. Increasing RPM is bounded by device tier and the transmission governor. Renewable sources have absolute output caps independent of RPM. A free gear ratio can improve handling up to the installed machine's cap but cannot multiply quarry output without limit.

For the first milestone use one supported speed, 16 RPM. Add ratios only after ownership, save/load and graph behavior pass their tests.

### 5.2 Sources: proposed initial tuning

All fuel values below are new VikingFactory balance proposals. Read native station fuel independently.

| Source | Unlock | Capacity | Speed | Operating rules |
|---|---|---:|---:|---|
| Hand crank | Meadows | 8 DU | 16 RPM | Hold interaction, spend 3 stamina/second; no unattended power |
| Water wheel | Bronze | 48 DU | 16 RPM | Fixed wheel in valid water, 6 m spacing, meaningful immersion and terrain checks |
| Steam engine | Iron | 160 DU | 32 RPM | 1 coal per 30 running seconds, valid water intake, native-style smoke clearance |
| Sail wheel | After Moder | 32–256 DU | 32 governed RPM | Capacity follows validated wind/exposure; low wind limits load, not magic constant output |
| Eitr motor | Mistlands | 320 DU | 64 RPM | 1 refined eitr per 120 running seconds; high input-chain cost |
| Reinforced steam engine | Ashlands | 320 DU | 64 RPM | 1 coal per 15 running seconds, twice the basic throughput/cost, heat-resistant construction |

Water wheel is openly an environmental abstraction: do not invent a river-current vector. Require valid world water, sensible mounting, submerged paddles and an unobstructed wheel volume. It must not work in a one-pixel puddle or accept overlapping wheels. Do not apply a fictional “freezes when Cold” rule. If the installed game exposes frozen-water behavior, integrate it explicitly; otherwise no freeze simulation.

Wind sampling belongs to the established environment/authority model. Diagnose discrepancies between host/client ownership. Smooth visual animation, not resource availability into free power. Choose a governing source deterministically. For v1, connect sources at compatible governed speeds; mismatched sources require a gearbox or are visibly isolated.

Steam water initially uses one short dedicated intake linkage, not a full fluid simulator. It cannot draw from decorative water or work without a real intake. No boiler explosions in v1. Exhaust blockage stops new engine progress and fuel consumption after verified in-flight accounting.

Fuelled sources consume only accrued running time. Persist the remaining paid fuel interval. Power loss does not repeatedly consume a new fuel item on restart. Reserve/validate fuel before providing paid drive.

### 5.3 Flywheel

Optional Mountains machine: stores 2,400 DU-seconds, maximum discharge 80 DU, charging efficiency 80%, maximum charge 80 DU. It bridges short wind drops and is not an overnight generator.

Charge only from capacity remaining after reservations. On a deficit, discharge `min(deficit, 80, storedEnergy/dt)`. If available sources plus discharge cannot meet the load, stall consumers. Never charge and discharge the same wheel in one step. Multiple wheels conserve energy across network splits and merges. Separate stored energy from nameplate DU.

### 5.4 Consumer loads

| Consumer | DU | Maximum useful rate / behavior |
|---|---:|---|
| Gravity trough | 0 | Downhill only, 15 items/minute |
| Timber belt, per 2 m | 1 | 30 items/minute |
| Iron belt, per 2 m | 1 | 60 items/minute |
| Reinforced belt, per 2 m | 1 | 120 items/minute |
| Bronze feeder | 6 | One item per swing, 30 items/minute maximum |
| Iron feeder | 8 | One item per swing, 60 items/minute maximum |
| Splitter/merger | 0 additional | Part of powered transport; cannot pump uphill alone |
| Recipe mill | 16 | Recipe-dependent; limited by machine tier |
| Advanced assembler | 32 | More buffers, conditions, complex approved recipes |
| Crop planter | 8 | One planting action per 4 seconds at baseline |
| Crop harvester | 12 | One mature plant per 4 seconds at baseline |
| Coppice cutting head | 24 | Harvest actions only; tree growth cannot accelerate |
| Bedrock quarry | 32 | 1 stone per 10 seconds, hard cap |
| Finite mining head | 48 | One validated mining stroke per 4 seconds |
| Deep extractor | 96 | Resource-specific long cycles |
| Cooking/oven tender | 8 | One interaction per 2 seconds baseline; native cook timers |
| Fermenter/hive/sap collection arm | 6 | One native interaction per cycle |
| Livestock feed gate | 6 | Controlled food release, no breeding acceleration |
| Livestock culling gate | 16 | Explicitly marked surplus adults only |
| Fishing winch | 24 | Physical catch rules; fallback remains disabled until verified |

Load numbers are configuration defaults. Display “48 DU supply / 42 DU reserved” and the reason for stopped motion. A hand crank can run one feeder but not two.

## 6. Construction catalogue and proposed build recipes

These are custom piece costs expressed in vanilla display names. Resolve each to a confirmed prefab at startup; do not guess internal IDs from English names. A missing material disables the affected piece with a diagnostic.

Baseline workbench pieces require workbench level 1; metal mechanical pieces require forge level 1 unless specified. More advanced pieces need their corresponding progression station as well as the gate in section 4. Track build and operating station requirements separately.

| Piece | Proposed construction cost | Notes |
|---|---|---|
| Wooden shaft, 2 m | 2 wood | 1 m snapping subdivision |
| Rope transmission | 4 wood, 2 leather scraps | Up to 4 m span, visible endpoints |
| Bronze cog / reversing cog | 2 wood, 1 bronze | Direction indicated in placement preview |
| Clutch | 4 wood, 1 bronze | Manual disconnect, later sensor-controlled |
| Hand crank | 6 wood, 2 leather scraps | Workbench |
| Catch basket | 6 wood, 2 leather scraps | Small explicit buffer |
| Gravity trough, 2 m | 4 wood | No horizontal/uphill transport |
| Timber belt, 2 m | 4 wood, 2 leather scraps, 2 bronze nails | Bronze handling tier |
| Bronze feeder | 6 wood, 2 bronze, 2 leather scraps | Two clearly marked endpoints |
| Splitter or merger | 4 wood, 4 bronze nails | At least three physical ports |
| Water wheel | 30 wood, 10 core wood, 4 bronze, 4 deer hide | Large footprint |
| Recipe mill | 20 wood, 6 bronze, 5 stone | Operating station depends on recipe |
| Planter | 10 wood, 3 bronze, 5 stone | Requires cultivator-era access |
| Harvester | 10 wood, 4 bronze, 2 leather scraps | Same field-link system |
| Iron belt, 2 m | 4 wood, 2 leather scraps, 2 iron nails | Distinct visual upgrade |
| Iron feeder | 6 fine wood, 3 iron, 4 iron nails | Retains exact item metadata |
| Timber saw | 15 core wood, 6 iron, 5 stone | Actual trees/logs; authorisation required |
| Coppice bed | 20 wood, 10 stone, species planting investment | See forestry rules |
| Bedrock quarry | 30 stone, 20 core wood, 8 iron, 10 iron nails | Requires stonecutter |
| Finite mining head | 20 core wood, 10 iron, 10 stone | Separate gate for each tool tier |
| Steam engine | 30 stone, 10 iron, 10 copper, 2 surtling cores | Water intake built separately |
| Water intake | 6 wood, 2 iron | Short linked connection |
| Forage bed | 10 wood, 10 stone, 5 cultivated specimen items | Consumed founding stock |
| Flywheel | 20 fine wood, 8 iron, 4 silver | Mountains gate |
| Governor | 5 fine wood, 2 iron, 2 silver | Explicit speed settings |
| Sail wheel | 30 wood, 20 fine wood, 10 linen thread, 10 iron nails | Requires artisan progression; first linen made natively |
| Advanced assembler | 20 fine wood, 10 black metal, 5 iron | Artisan progression |
| Farm gantry | 30 wood, 10 black metal, 10 linen thread | Multiple visible heads, no radius magic |
| Deep extractor | 30 stone, 20 iron, 10 black metal, 10 linen thread | Recipe unlocked only after Moder |
| Rune control upgrade | 5 black metal, 2 refined eitr | Mistlands controls |
| Eitr motor | 20 black marble, 10 black metal, 5 refined eitr, 1 black core | Black forge-era access |
| Cooking tender | 10 wood, 2 bronze, 2 leather scraps | Iron extension adds larger-station reach |
| Oven tender extension | 4 iron, 5 stone | Installed on tender |
| Livestock feed gate | 10 wood, 2 bronze | Feeds real animals |
| Livestock culling gate | 10 core wood, 6 iron | Surplus policy mandatory |
| Fishing winch | 15 fine wood, 4 iron, 2 bronze | Rod/bait discovery also required |
| Reinforced steam engine | 40 grausten, 15 flametal, 10 iron, 4 surtling cores | Ashlands gate |

Provide dismantle refunds through the game's piece system. Construction materials refund normally; consumed operating fuel, founding stock and historical production do not refund. Buffered items and machine-held gear return exactly once into a recovery container or controlled drops.

Do not add recurring replacement tool bills to every machine. Power, construction, footprint, ingredients and progression are the main costs. Keep maintenance optional until actual playtests show it improves the game.

## 7. Logistics players can understand

### Belts and ports

- Start with straight, corner, incline and short vertical lift modules. Add long lifts only after the core works.
- A port defines position, forward vector, accepted direction, category and throughput. Adjacency needs both distance and orientation.
- Port categories include general item, fuel, ore/input, recipe ingredient, output, seed reserve and recovery.
- 1 m placement grid; 2 m standard belts. Support free placement with strict port tolerances and a visible connection preview.
- Mod inventories contain real item records. Moving visuals are pooled client representations, not networked physics objects for every item.
- The logical queue owns items. Rendered pieces must never be pickable copies.
- Backpressure propagates upstream. Output full pauses production before input is irreversibly consumed unless ingredients are already safely held as an in-progress job.
- Belts do not pull from every nearby chest. A storage dock or feeder must physically connect to a named chest.
- An open-air ejector is opt-in. Default no-target state retains the item.
- Closed loops are legal but bounded by finite buffer space. No infinite same-tick circulation.

### Splitters and mergers

Default splitter: fair round-robin among available outputs. Priority splitter: primary output first, then overflow. Filter splitter: explicit item/quality rules, with unmatched output or stop. Merger: fair rotating input preference to avoid starvation.

A manifold uses priority/overflow splitters, not a special hidden distribution radius. Its first machines fill before later machines. Show that behavior in a tutorial.

Keep filters as “ghost” selections, not consumed filter items. Match by prefab identity plus optional quality/metadata criteria. Never merge different qualities, variants or custom-data identities merely because names match.

### Storage and quotas

Each output dock can target a stock count. Count only the explicitly linked inventory group, not every chest in range. A reserve splitter can protect seeds, fuel or animal feed before allowing surplus downstream. Add high/low thresholds to avoid rapid toggling.

Mark floor collection areas explicitly. Exclude tombstones, quest containers and unauthorised player drops by default. A collection tray can collect actual station output at a known chute with ownership-safe claims. Never both intercept a station output and collect its spawned duplicate.

## 8. Renewable wood and forestry

Wood must be a complete and satisfying supply chain. Implement two distinct systems.

### 8.1 Real forestry

The player marks an owned plantation with visible corner stakes and species rules. A saw head harvests only registered mature trees or assigned logs inside that plot. Exclude decorative/protected trees, structures, unmarked wilderness and other players' plots.

Use the relevant native damage resistance/tool-tier and drop behavior. The machine cannot cut a tier of tree the settlement has not unlocked. Handle the standing tree, falling log and final drop stages without awarding every stage's full yield twice.

Replant using real seeds from a reserved buffer and the native placement/growth checks. A missing seed pauses replanting. Do not promise native tree drops are seed-positive. Actual forestry can need seed resupply.

Maintain a bounded target queue. Do not scan the whole forest each tick. A full timber buffer blocks new cutting. Once an already-cut tree falls, collect its genuine drops or leave them in the world if capacity disappears.

### 8.2 Managed coppice, default renewable wood solution

This is explicitly a new VikingFactory horticulture mechanic. It avoids modifying global tree seed rates and avoids an endless forest of physics objects.

A large managed bed contains persistent rootstock and several visible growth stages. Founding stock is consumed once; it is not an output recipe. Growth uses fixed elapsed active simulation time and appropriate native species growth duration as a lower bound. Powered heads harvest mature plots. More RPM never accelerates biological growth.

| Plot type | Founding investment | Proposed harvest | Minimum cycle | Gate |
|---|---|---|---|---|
| Common coppice | 5 verified common-tree seeds | 20 wood | Greater of 30 minutes and validated species growth time | Iron |
| Pine grove | 5 pine cones | 10 core wood + 10 wood | Greater of 40 minutes and native growth time | Iron and discovered core wood |
| Fine-wood grove | 5 valid birch seeds or acorns, species fixed | 8 fine wood + 12 wood | Greater of 50 minutes and native growth time | Iron and discovered fine wood |

Require 8 × 8 m clear cultivated/appropriate ground per bed, sky access and valid species biome. No roof-stacking plantations. Plot occupancy and spacing are authoritative. With a faster source growth mod, use explicit compatibility settings rather than silently applying two speed bonuses.

Founding stock becomes rootstock state; dismantling destroys that cultivated investment. Once mature, the bed waits without producing more batches. Buffering cannot accumulate invisible years of wood.

Resin is not automatically included in every timber batch. Add a separate tapping mode: common coppice gives 1 resin per 10 minutes while sacrificing that plot's timber production. No simultaneous full timber and resin harvest from the same plot. Switch mode only after resetting/finishing the current cycle.

No automatic ancient-bark, Yggdrasil-wood or other special-tree plantation until their species rules, progression and renewable behavior are separately reviewed. They may still use finite real forestry where supported.

## 9. Stone, mineral extraction and anti-exploit rules

### 9.1 Renewable building stone

An Iron-age **Bedrock Quarry** is the default renewable stone source: 32 DU, one stone per 10 active seconds, capacity 50, 6 × 6 m excavation footprint. Requires natural terrain, a stonecutter-era unlock, and 12 m between quarry centres. Exclude dungeons, constructions and underwater placements.

Treat “bedrock” as the mod's explicit abstraction, not a claim that vanilla terrain contains unlimited mineable stone. Animate a drill and gravel chute; do not continuously deform terrain or create terrain-save bloat.

At 6 stone/minute it produces 360/hour. It is a building-material convenience, not a route to arbitrary metals. Multiple quarries need separate ground and power.

### 9.2 Finite mining

A mining head works an explicitly surveyed real deposit, rock or eligible destructible. Mining leaves normal depletion. Respect damage type, tool tier, environmental hazards and real drop rules. Do not remove crypt gates, excavate dungeon contents remotely, mine inaccessible tiers, or award unsupported special-resource drops.

Mining strokes use a configured verified pickaxe-equivalent capability. Raw output per minute is an observed result of native damage/drops, not a promised flat ore rate. Do not implement this as “spawn ore, then lower the rock health later.”

Deposits can overlap ownership boundaries; coordinate damage/drop ownership. Record authorised target IDs. Stop when the target disappears, loses access permission, becomes unsupported or the output cannot be handled safely.

### 9.3 Late renewable older ores

The default Balanced Industry preset eventually adds **Deep Extraction**, an explicit mod-added recovery of trace deposits. It never provides the first sample of a metal and never unlocks the current biome's special material.

| Output | Required milestone | Operating location | Commissioning cost, consumed once | Rate | DU |
|---|---|---|---|---|---:|
| Copper ore | Moder defeated + copper known | Black Forest outpost | 10 copper ore | 1 per 120 seconds | 96 |
| Tin ore | Moder defeated + tin known | Black Forest coastline | 10 tin ore | 1 per 120 seconds | 96 |
| Scrap iron | Yagluth defeated + iron known + Mistlands machinery unlocked | Swamp outpost | 10 scrap iron | 1 per 180 seconds | 96 |
| Silver ore | Fader defeated + silver known | Mountains outpost | 10 silver ore | 1 per 240 seconds | 96 |

One resource selection per extractor. At least 32 m between renewable extractors, regardless of type. Use actual terrain/biome checks at the footprint, not just the player's biome. Capacity 20. No speedup from RPM and no offline catch-up in v1.

Commissioning state is tied to machine and physical site. Moving/rebuilding requires new commissioning; crafting/dismantling cannot refund consumed samples. A fully commissioned idle extractor does not spend additional sample ore. This is expensive infrastructure with a slow return, not an ore duplication recipe.

Ore remains ore. It still needs transport, coal and native smelting. Black metal is obtained through its existing supply sources; flametal and special endgame resources remain exploration/combat-dependent unless a later separately reviewed mechanic makes sense.

Do not infer “all ores” from an item name suffix. Explicitly allowlist each renewable output. Default deny unknown 1.0 or modded minerals.

### 9.4 Never add these loops

- Grind a bar to more ore than it cost to smelt.
- Convert ordinary stone into guaranteed rare metals in early progression.
- Deconstruct machines for more than construction inputs.
- Multiply output through a world resource multiplier twice.
- Reclaim consumed seeds or founding samples after earning outputs.
- Burn coal to power a machine that creates more coal from nothing.
- Reset a cooldown or resource site by rotating/reloading the piece.

Industrial charcoal is allowed to be self-fuelling because it consumes genuinely grown wood, time, land and machinery. That is a legitimate renewable production chain, not a zero-input loop.

## 10. Farming, forage, bees and sap

### Crops

Use a rectangular marked field with visible boundaries and a planter/harvester access rail or arm reach. Early machines service a small contiguous bed, proposed 4 × 4 m; Plains gantries extend to 8 × 8 m. The effect area must be visually credible and visible in placement/inspection mode.

Discover plant relationships from verified native definitions. Treat “food plant” and “seed plant” as distinct recipes where applicable. Do not hand-wave carrots, turnips or onions into self-seeding crops.

Provide three policies:

1. Replant the same approved crop, reserving the next planting cost first.
2. Seed production field: dedicate a field to the appropriate seed-crop form.
3. Food production field: receive seeds from that field through actual logistics.

Harvest only mature healthy crops through a single safe native drop path. Reserve a legal planting location and input before planting. If a player changes terrain or another plant occupies the spot, retain the seed and report blocked space.

Maintain vanilla soil, spacing, cover, biome and growth requirements. No irrigation bonus, instant fertiliser, temperature bypass or multiplied harvest in the default rules.

Reserve example: before export, retain seeds for all empty assigned tiles plus a configurable 10% safety stock. For produce-as-seed crops, reserve the actual planting cost, then export surplus. Avoid starvation when a recipe mill is competing for edible planting materials.

### Forage cultivation

Native pickables can be collected on their real respawn rules inside a marked plot. As a separate Balanced Industry feature, the Iron-age nursery can establish stationary **forage beds** for a strict allowlist.

Each bed consumes five specimens at commissioning; existing wood/stone supplies the structure. Proposed outputs are one matching item per 10 minutes, capped to no faster than the verified native respawn interval where one exists. The physical bed is 2 × 2 m and holds at most five produce. Harvest requires a powered arm. Use no more permissive biome/cover settings than the reviewed species definition.

Initial candidates: raspberries, blueberries, ordinary mushrooms and thistle after discovery; cloudberries after Plains progression. Each must be tested and explicitly enabled. The same idea does not automatically apply to royal jelly, quest plants, dungeon-only ingredients, special Ashlands plants or Deep North resources.

These are new cultivation systems. Do not present them as ordinary vanilla planting or patch every wild bush.

### Honey and sap

Hive arms collect actual honey, retaining the native hive's happiness and storage logic. Do not manufacture queen bees. Sap adapters handle actual collectors on valid roots and respect whatever regeneration/depletion rules the installed game exposes. Never assume sap and soft tissue occupy the same input/fuel roles as ore and coal; inspect that station's conversion and fuel definitions.

Add a wisp collection adapter only for a real functioning native source and its native environmental schedule. No daylight wisp generator made from an empty chest.

## 11. Animals, fishing and combat-derived supplies

### Husbandry

Automate real animal care, not a box that crafts animals out of food.

- Feed gates release one permitted food into a confined feeding trough/pad when required, preserving normal consumption.
- Native taming, breeding, pregnancy, density limits, maturation and egg conditions remain intact.
- An egg arm may transfer real eggs between an explicitly marked breeder output and hatchery. Validate fertilisation/incubation semantics before enabling it.
- Culling gates are opt-in, visually obvious, and target only tagged surplus adult tame livestock. Minimum breeder count defaults to two; player-named animals are protected. Exclude pets, immature animals, untamed creatures and every unreviewed species.
- Use legal damage/kill/drop behavior without player combat skill rewards. Do not duplicate loot with a second harvest recipe.
- Verify boars first; add chickens, wolves, lox, asksvin and any new tameable species through separate tested policies.

Meat, hides and eggs enter normal belts. Animal remains are not generic interchangeable crafting ingredients. A boar pen cannot make deer hide or serpent meat.

### Fishing

An optional fishing winch is later scope. It uses a real water placement, known rod/bait, the correct bait per species, and a real fish availability/catch pathway. If the installed game has no safe reusable capture operation, keep this feature disabled and explain the blocker; do not silently replace it with arbitrary fish spawning.

An explicitly designed virtual fishery may be proposed later, with bait consumption, coastal spacing, species unlocks and limits, but is not implied by this specification. Raw-fish processing from supplied catches is still automatable through verified recipes.

### Enemy supplies

Provide loot collectors and later physical trap/siege feed adapters where sensible. Enemies must actually spawn and die. Loot multipliers, stars and drop tables remain native. Do not add craftable enemy spawners or convert common wood into surtling cores, chains, trophies, carapace or royal jelly.

This means some chains are **processing-automated but supply-limited**. Say so clearly in the recipe browser. That is intentional balance, not an unfinished hidden promise of free every-item generation.

## 12. Cooking, baking and brewing

Support whole supply chains rather than only raw-meat racks.

1. Fields/forage/animals produce ingredients.
2. Belts reserve seeds and breeder food before kitchen export.
3. Recipe mill prepares only approved live recipes at the correct cooking/preparation station and level.
4. Racks/ovens use their own real heat, fuel, slots, timers and burn behavior.
5. Tenders remove finished food and route it to stock-controlled storage.
6. Mead base follows the live recipe into a real fermenter, then finished bottles are collected at the native completion time.

The mill is not an instant oven or fermenter. A recipe that outputs uncooked dough must output that dough, not the final bread. A fermenter conversion is not an `ObjectDB` craft recipe just because both have ingredients and output.

Cooking failure is meaningful: if power stops, a tender stops while the rack may continue cooking. Food can burn. Provide a warning and stock-controlled loading so players can engineer reliable power. A native station can still continue processing even if the external belt jams; do not freeze its timer as an accidental cheat.

Include an optional safe-loading interlock: do not insert raw food unless the tender has a confirmed output slot and enough current operating capacity. It is risk reduction, not a guarantee against later wind failure. A flywheel may be useful here.

Feasts and any special-serving objects need a separate reviewed adapter. Do not treat a multi-serving placed object as a stack of free meal items. Trader-specific ingredients stay purchased/supplied unless an actual native recipe exists. Support stock targets so the kitchen does not consume every ingredient forever.

## 13. Recipe mill and advanced assembler

### Runtime recipe catalogue

Build the catalogue from live enabled recipes and independently audited station-conversion tables. Record stable recipe identity, source mod, input alternatives, counts, required station/level, output count/quality, unlock conditions, special behavior and support status.

Do not enable every arbitrary third-party `Recipe` automatically. Default native ordinary crafting recipes can be supported through the generic engine after validation; modded recipes require an explicit compatibility profile or server opt-in. Custom side effects, mould/catalyst consumption, build-only recipes and conversion-only recipes must be classified.

### Ports and buffers

The basic mill has four physical ingredient ports, but each can accept filtered mixed inputs into a dynamic logical ingredient inventory. Support at least eight distinct ingredients in the initial UI and an extensible backend. Detect larger recipes and show a clear unsupported/capacity reason rather than silently truncating ingredients.

One output port and one recovery/service access. Advanced assembler adds ports/buffer space and more recipe classes; it does not magically double native station speed.

### Craft transaction

1. Select one recipe taught by an authorised player.
2. Validate required world/material gate and operating station.
3. Check station type, current level and relevant usability/environment conditions from the mill's marked work position.
4. Select a deterministic legal ingredient plan, including alternatives and exact metadata constraints.
5. Reserve output capacity and ingredients.
6. Move inputs into persistent work-in-progress escrow before starting.
7. Advance only while powered and all required ongoing conditions hold.
8. Finish once, using the recipe's actual output count and metadata policy.
9. Commit to output buffer. Clear escrow only after a confirmed result.

Default artificial craft time: `max(4 seconds, 4 + 2 × distinct ingredient types)` at baseline speed. Basic mill maximum speed multiplier 2; advanced assembler maximum 4, with an absolute 2-second minimum. These are mod costs because native player crafting and machine animation timings are not the desired mass-production model. Crafting a batch respects the recipe's actual output count.

Physical station presence matters continuously. Destroying an upgrade cannot let an already-started high-level job finish under an invalid station level. Pause and retain escrow; allow authorised cancellation to recovery.

### Equipment

Quality 1 crafting first. Later upgrades consume the specific existing item and its actual next-quality resources. Preserve identity, variant and relevant custom data. Validate maximum quality, station level, recipe rules and supported upgrade path. Never accept “quality 4” as a way to create free intermediate upgrades.

No default automated repair, enchantment, unique ritual or gear conversion until separately audited. Crafted creator attribution is the teaching/operator identity where valid, not a fake local player. Machines never award weapon, cooking or crafting skill repeatedly unless a separately approved integration explicitly requires it.

### Catalogue audit as a deliverable

Generate `docs/AutomationCoverage.csv` and an in-game browser. Every obtainable inventory item in the inspected runtime must have one of:

- Renewable supply + full production chain.
- Native renewable supply with physical collection.
- Finite/exploration supply + automated processing.
- Combat/husbandry supply + automated processing.
- Trader supply + automated processing.
- Manual/unique by design.
- Unsupported pending adapter, with a concrete reason.

Do not call the mod “all items automated” unless the coverage report supports that claim. Report counts and coverage percentages, excluding attacks/VFX/debug prefabs from the denominator. Include provenance and version in the export.

## 14. Required coverage matrix

This table is a design checklist. Actual recipe IDs and quantities come from the local game. Do not manufacture absent recipes to fill a row.

| Resource/output family | Acquisition design | Processing/logistics | Constraint |
|---|---|---|---|
| Wood | Real forestry, common coppice | Belts, kiln, crafting | Land and growth |
| Core wood | Pine forestry/grove | Belts, crafting | Species investment |
| Fine wood | Eligible forestry/grove | Belts, crafting | Tool/species gate |
| Ancient bark | Real supported trees or supplied stock | Crafting | No default artificial grove |
| Yggdrasil/special woods | Real supported sources | Crafting | Biome gate, explicit adapter |
| Stone | Real mining, bedrock quarry | Building-stock storage, approved crafts | Iron renewable gate |
| Flint | Actual pickups/respawn collection where supported | Crafting | No generic stone conversion |
| Coal | Wood into actual kiln | Furnace fuel manifold | Native fuel conversion |
| Resin | Real drops, coppice tapping mode | Torches/arrows/recipes | Alternative plot output |
| Copper/tin | Finite mines, late deep extraction | Native smelting, bronze mill | Moder renewable gate |
| Bronze | Verified copper + tin recipe | Nails, gear, machinery | Full input costs |
| Iron | Finite scrap supply, later extractor | Smelter, crafting | Mistlands-era renewable gate |
| Silver | Real deposits, later extractor | Smelter, crafting | Ashlands completion gate |
| Black metal | Real combat drops | Blast furnace, crafts | No common-stone source |
| Flametal | Real supported deposits/sources | Native processing | No free renewable recipe |
| Obsidian/crystal | Supported mining/combat sources | Approved crafts | No alchemy shortcut |
| Black marble/grausten | Real supported destructibles | Stock and crafts | Respect native damage/support |
| Tar | Real tar sources/enemies | Collection/storage | No wood-to-tar assumption |
| Sap | Real extractor | Refined eitr chain | Real source capacity |
| Soft tissue | Real sources | Eitr refinery | Not generated by quarry |
| Refined eitr | Actual refinery | Crafts or costly motor fuel | Hazard remains |
| Carrots/turnips/onions | Seed and food fields | Kitchen | Separate propagation loops |
| Barley/flax | Valid fields | Native mill/spinning | Valid biome and reserve |
| Jotun puffs/magecaps | Valid fields | Meals/meads | Native cultivation rules |
| New 1.0 crops | Runtime-discovered supported plants | Native recipes | Explicit reviewed mapping |
| Berries/mushrooms/thistle | Actual pickables, approved forage beds | Kitchen | Species/biome rules |
| Honey | Actual happy hives | Meals/mead | Queen bee acquisition manual |
| Eggs/poultry | Native livestock | Incubation or cooking | Preserve breeder stock |
| Boar/wolf/lox/other tame livestock products | Real approved animals | Collection, cooking, crafting | Real breeding/drops |
| Deer/hare/bird/serpent products | Actual hunting or supported capture | Cooking/crafting | Do not fake animal recipes |
| Fish/raw fish | Supplied catch, later verified winch | Native preparation | Bait/species constraints |
| Hides/leather/scales | Correct animals | Equipment/machine costs | No universal hide transmutation |
| Feathers | Actual drops or reviewed native husbandry | Arrow lines | Verify source |
| Entrails/bloodbags/bones | Actual drops | Food/mead/gear | Real enemy supply |
| Greydwarf eyes/cores/chains | Actual drops/exploration | Crafts | No magic enemy-resource mill |
| Carapace/mandibles/royal jelly | Native combat/dungeon sources | Meals/equipment | Supply-limited |
| Linen thread/barley flour | Native processors | Crafts and kitchen | Do not bypass processing |
| Nails/ammunition | Live recipes | Mills with quotas | Correct output batch |
| Cooked meat | Native cooking slots | Tender | Can burn |
| Stews/soups/salads | Live approved station recipes | Recipe mill | Station level and ingredients |
| Bread/pies | Recipe preparation + oven | Oven tender | Preserve uncooked stage |
| Mead bases/finished mead | Recipe + real fermenter | Load/unload adapter | Native fermentation |
| Feasts | Live recipe + special handling | Reviewed feast adapter | Preserve serving semantics |
| Weapons/armour/tools | Live recipe, quality 1 initially | Assembler | Actual unlocks, no repair shortcut |
| Gear upgrades | Existing item + next upgrade inputs | Later reviewed upgrade job | Identity retained |
| Trader ingredients | Purchased stock | Storage and downstream recipes | No unauthorised free supplier |
| Boss trophies/keys/unique quest items | Manual progression | Protected storage only | No renewable production |
| Deep North items/stations | Runtime catalogue and capability audit | Generic paths only when applicable | Special mould/ritual logic explicit |
| Compatible modded items | Explicit profile | Same graphs/transactions | Fail closed if semantics unknown |

## 15. Multiplayer and item conservation

Treat this as the largest engineering risk. “Only the ZNetView owner simulates” is necessary for many operations but does not solve transfers between differently owned objects, player inventory races, or process interruption.

### Authority design

Keep a pure network scheduler distinct from the authoritative owner of each vanilla object. For each mod kinetic component elect one coordinator through the verified network ownership mechanism. Use deterministic stable IDs plus an ownership epoch, not two clients independently choosing themselves. On handoff, pause until state is rehydrated and ownership confirmed.

The coordinator determines allowed production/speed, but each authoritative object adapter validates its own mutation. No arbitrary client RPC may say “add 500 iron.” Send operation intents with source, destination, permitted item selection, count, revision, transaction ID and authority epoch. Validate range/ports, permissions, item availability, power and quotas.

Do not mass-claim vanilla inventories every tick. Start with supported ownership arrangements. If cross-owner station transfers cannot be proven safe, visibly pause them pending authority coordination instead of proceeding speculatively.

### Transfer state machine

Design and test a transaction service with states such as Planned, Reserved, Escrowed, Delivered, Committed and RecoveryRequired. The precise implementation must fit Valheim's actual persistence guarantees.

- Persist a unique transfer ID and bounded deduplication records.
- Reserve source and destination against revisions.
- Detach exact item records into persistent escrow through the source authority, once.
- Destination accepts a specific transaction at most once, with an observable acknowledgement or locally verified guarded mutation.
- Finalise source escrow only when delivery is known.
- A timeout after sending is an **unknown outcome**, not proof of failure. Do not refund and resend blindly.
- On uncertainty, hold the item/job in a visible recoverable state until reconciled.
- Recheck inventory changes caused by players. A preflight capacity check is not a lock.
- Prune transaction history only after a safe checkpoint/acknowledgement policy.

If a native station's RPC has no acknowledgement/idempotency mechanism, add a minimal audited bridge at the actual accepted mutation point, or constrain the operation to one proven authority. A generic wrapper cannot simply assume the game provides transactions.

Do not claim arbitrary crash-proof exactly-once behavior across independently saved objects. Prove the supported save/restart boundary. Prefer a single authoritative durable record per transfer where feasible; otherwise document and quarantine unresolved operations rather than risk duplication.

### Metadata

Preserve prefab identity, count, quality, durability, variant, crafter identity/name, custom data and the installed game's relevant provenance/cheat fields. Determine stack compatibility using native semantics. Do not clear cheat flags. New outputs inherit provenance according to the verified native crafting/conversion policy, with conservative behavior when unknown.

Do not treat an entire `Smelter` input as a plain item name if the current version also carries metadata. Preserve current signatures through explicit adapters and startup capability checks.

### Permissions

Wards/access restrictions apply to configuration, source access, target access, harvesting and recovery. Unknown or inaccessible inventory pauses. Do not pull from player inventories, tombstones or restricted containers as if they were a workshop chest.

Require compatible mod/protocol versions across participants. This is a modded PC setup; do not promise vanilla-console connectivity merely because Valheim itself supports crossplay.

## 16. Time, sleeping and unloaded areas

No forced chunk loading and no remote factory ticking in v1. Reconstruct connections from live pieces when an area loads; persist items/jobs, not a stale graph.

Separate:

1. Mod simulation elapsed active time for generators, transfers, quarry/coppice and custom crafts.
2. Native world timestamps used by crops, fermenters or other vanilla systems.
3. Rendering time for smooth animations.

Use a fixed-step scheduler with bounded catch-up and explicit discontinuity detection. Do not feed the entire time since a factory last loaded into its production counter. Freeze custom offline progress by default. Reset the scheduler's last-active baseline after loading while retaining actual paid work progress.

Sleep semantics must be tested, not asserted. Different native components can use timestamps or special updates. Baseline rule: native objects continue according to native behavior; custom transfer/production does not replay a whole night as thousands of fictitious interactions. Tenders resume from the real post-sleep state, so food might already be burnt or a station might already be full/finished.

An optional bounded sleep simulation is future scope only after event-order tests account for generator fuel, crop growth, station conversion, burn deadlines and output congestion. `ZNet` timestamps alone do not solve those interactions. Document baseline sleep consequences in the in-game guide.

At partially loaded network boundaries, a missing continuation blocks the relevant port. Do not infer that an unloaded downstream chest is empty. Do not allow remote unloaded generators to power local machines through a cached graph.

## 17. Code organisation

Use the repository's existing conventions where possible. Suggested boundaries:

| Module | Responsibility |
|---|---|
| Core | Pure graph solver, scheduling, rate limits, item/job plans |
| Kinetics | Sources, consumers, clutches, ratios, energy buffer |
| Logistics | Typed ports, queues, splitters, storage docks |
| Transactions | Escrow, revisions, idempotency, reconciliation |
| Crafting | Runtime catalogue, teaching, station checks, jobs |
| Adapters | Individual vanilla integrations with capability probes |
| Production | Quarry, coppice, forage beds, deep extraction |
| Agriculture | Plant mapping, fields, reserve policies |
| Husbandry | Species policies, feeding and surplus protection |
| Networking | Authority epochs, RPC contracts, visual state |
| Persistence | Versioned data, migrations, recovery |
| Presentation | Models, animations, UI, audio, localisation |
| Diagnostics | Coverage export, counters, structured warnings |

Suggested interfaces are design-level names, not claims that the game has them: `IKineticNode`, `IPowerSource`, `IItemEndpoint`, `ITransactionalAdapter`, `IProductionJob`, `IWorldRuleValidator`, `IAutomationPermissionPolicy`.

Adapters: container, smelter family, cooking rack, oven, fermenter, beehive, sap collector, plant, pickable, tree/log, mineable, livestock and any special endgame station. Prefer capability-driven registration and verified prefab mappings over one giant switch or one global patch class.

Keep Harmony patches narrow. Do not replace an entire vanilla update loop when an insertion/output boundary is enough. Detect competing auto-fuel/auto-store mods and document affected combinations; never silently remove another mod's patches.

Pick one server-authoritative config solution. Use existing ServerSync if the project already relies on it; otherwise Jötunn's supported synchronisation is sufficient when it meets the requirements. Client settings only control presentation. Version and hash balance data so clients cannot locally reduce machine costs.

## 18. Persistence, assets and performance

### Saved state

Version each machine's schema. Persist inventory, work escrow, paid fuel interval, selected recipe, teaching/permissions, founding investment, growth/job progress, selected mode, transaction recovery and stored flywheel energy. Use stable prefab names and IDs. Renaming a class/model must not orphan placed machines.

Rebuild graph edges from validated ports. Never persist transient Unity instance IDs as durable identity. Migrations are idempotent; unknown newer schemas pause safely. On destruction, stop production, reconcile active transactions, then return held items once. A machine cannot destroy its recovery ledger before its held items are resolved.

Provide `vf status`, `vf network`, `vf exportcatalog`, `vf coverage`, `vf validate`, and an admin-only guided recovery diagnostic. Recovery must not be a casual “spawn all missing items” command.

### Assets and look

Original low-poly timber, iron hoops, hide belts, gears, rope, stone foundations and restrained runes. Machines use correct support, damage, weather and fire behavior appropriate to their material. No full rigid-body simulation of every gear. Shaft spin is visual; topology is logical.

Prototype with clearly labelled simple original meshes or permitted vanilla-derived prefabs. Ship only redistributable assets. Retain stable script/assembly identities expected by asset bundles. Use the Unity version matching the tested game workflow. Generate icons and localisation tokens; controller navigation matters.

### Scheduling and profiling

Initial goals, to be measured rather than promised:

- Logical scheduler 5 Hz; interpolate rendering independently.
- No whole-world scans or graph rebuilds per frame.
- Local spatial index for snapping and marked plot membership.
- Topology changes queued/debounced; recompute affected components only.
- Sleep unchanged idle machines until an inventory, power or timer event wakes them.
- Bounded operations per tick and per network, with fair scheduling.
- Compact network updates only for meaningful changes; no item transform RPC every frame.
- Limit/pool visible belt meshes separately from logical item capacity.
- Stress scene: 500 pieces, 100 active machines, two clients, saved and reloaded.
- Initial profiling target under 2 ms average mod work per simulation tick on the documented reference machine; measure p95/p99 and adjust. This is not a universal FPS guarantee.

If caps are exceeded, throttle visibly with a diagnostic. Never discard queued items to recover performance. Do not run Unity object mutations on background threads; only isolated pure calculations may run off-thread after review.

## 19. Player-facing information

Inspect any machine to see recipe/mode, required station, input shortages, output stock, progress, DU reservation and exact blocked reason.

Core states: Working, No power, Overloaded, Invalid rotation, Missing input, Output full, Wrong biome, Invalid field space, Station missing, Station level too low, Exhaust blocked, Awaiting native process, Permission denied, Waiting for ownership, Recovery needed and Unsupported adapter.

Do not expose raw class names to players. Put technical details in debug views.

Workshop journal recipes show upstream acquisition categories and downstream outputs. A dotted/manual link means the player still supplies that resource. Display estimated items/minute from actual observed or configured rates, clearly differentiating native variability from fixed machine caps.

Add copy/paste machine settings and a layout ghost preview later. A blueprint does not waive construction costs or station requirements. First release uses manual building.

## 20. Worked balance and tutorial factories

These use proposed mod rates. Any native station times in examples must be replaced by measured runtime values before presenting an exact production calculator.

### First powered kiln

Water wheel 48 DU, three bronze feeders 18 DU, four timber belt segments 4 DU: **22 DU reserved**, leaving 26 DU. Feeder roles are input, finished-output handling and a fuel/stock branch as appropriate to the actual kiln layout. Kiln runs on its native rules, independent of how much spare DU remains.

Tutorial proves that disconnecting a shaft stops item handling, reconnecting resumes, and blocked smoke stops native processing without deleting wood.

### Builder's stone supply

One quarry 32 DU + one bronze output feeder 6 DU + four belts 4 DU = **42 DU**. One water wheel can run it. Output 6 stone/minute = 360/hour. Two such lines need 84 DU, so one wheel stalls and two supply 96 DU.

Quarry buffer 50 fills in about 8 minutes 20 seconds if nothing is removed. The quarry then stops without consuming nonexistent space.

### Self-fuelling steam sawmill

One common coppice bed yields 20 wood per minimum 30 minutes, an average of 0.667 wood/minute before interruptions. A steam engine uses 2 coal/minute while running. If the verified kiln conversion is one wood to one coal, at least three ideal beds are needed just for that fuel, with no export margin. Use six or more beds for a resilient fuel reserve and useful surplus, subject to power/harvest capacity.

Route coal first to engine reserve and export only above a configurable threshold. Keep a hand crank or water-driven starter line so a fully empty fuel system can restart. Do not use an unpowered inserter as the only way to refuel its own empty engine; engine service access always allows manual fuel.

### Food kitchen

Seed field → seed reserve → food field → ingredient sorting → recipe mill at correct kitchen station → oven/rack where needed → tender → meal chest.

Prioritise replants and livestock food before meals. Use stock targets such as 30 portions per selected dish. Feed shortages show the missing upstream ingredient instead of “machine broken.”

### Bronze workshop

Measured copper and tin smelting → bronze recipe mill → bronze-nail recipe mill → stock-controlled chest. The reference catalogue has 2 copper + 1 tin → 1 bronze and 1 bronze → 20 bronze nails; verify locally rather than hard-code. Supporting ore/coal belts are independent of recipe-mill buffers.

Under the proposed late extraction rates, two copper extractors and one tin extractor supply one bronze per two minutes before downstream losses/bottlenecks: 30 bronze/hour. This requires **288 DU for extraction alone**, three properly spaced sites, and native fuel/processing. It is long-term old-tier supply, not a better first bronze rush.

### Mead hall

Hives/forage/supplied ingredients → taught live mead-base recipe → real fermenter bank → finished mead collection. Add fermenters to increase capacity; never reduce fermentation time. Stock quotas must count bottled output plus committed work in progress to avoid overproduction.

## 21. Implementation milestones with acceptance gates

Implement in this order. Each milestone must leave a usable build. Update the persistent checklist after every session.

### M0: audit and scaffold

- Compatibility report, exact reference versions, startup diagnostic, buildable inert workshop piece.
- Pure core test project and a live catalogue exporter where possible.
- Gate: builds against actual references, no guessed RPC signatures, existing features still load.

### M1: first playable power and logistics

- Crank, shaft, clutch, water wheel, one belt, one feeder, catch basket and chest dock.
- Fixed 16 RPM initially; reserve DU and stall behavior.
- Safe chest-to-buffer-to-chest transfer and item metadata persistence.
- Gate: two feeders stall crank; no loss/duplication during competing chest access and save/reload; direction visible.

### M2: native production line

- Kiln/smelter adapters, fuel/input distinction, output collection, splitter/merger.
- Gate: actual powered production with native rates/environment; full output never duplicates; authority change tested.

### M3: recipe mill

- Teaching, dynamic ingredients, station requirements, persistent escrow, quality-1 recipes and quotas.
- Demonstrate bronze/nails and at least one verified food-preparation recipe.
- Gate: station removal, recipe change, full output and cancellation preserve ingredients; five-ingredient recipe supported.

### M4: core resource generation

- Bedrock quarry, real plantation harvesting where safe, managed coppice and reserve routing.
- Gate: repeatable renewable wood/stone loop, no overlapping plots, no refund/reset duplication.

### M5: agriculture and kitchens

- Planter/harvester, seed/food fields, real hives, fermenter, cooking rack and oven.
- Gate: at least one root-crop two-field loop and one food/mead chain; stalls preserve intended burn risks.

### M6: expanded power and controls

- Steam, sail, governor, gear ratios, flywheel; filters/stock thresholds.
- Gate: energy accounting, contradictory cycles, split/merge and fuel-stall cases proven.

### M7: extraction and later resources

- Finite mining, gated old-tier extraction, sap/eitr, advanced assemblers and reviewed late-game adapters.
- Gate: progression bypass tests fail closed; sampled commissioning cannot be refunded after production.

### M8: broader supply and release hardening

- Forage cultivation, livestock, selected fishing only if proven, item upgrades, compatibility profiles and complete coverage report.
- Gate: dedicated-server soak, migration, all enabled feature acceptance tests, packaged assets and clear limitations.

Do not defer wood/stone generation until after optional UI polish or complex fishing. They are central to the user's request. Do not start moving contraptions, train systems, robot drones or conveyor portal networks in this roadmap.

## 22. Required testing

Use pure-core unit/property tests for meaningful invariants, and explicit in-game integration scenarios for Unity/native behavior. Mock tests cannot certify game RPC correctness.

### Conservation and race scenarios

- Two feeders compete for the last item.
- Player removes an item between planning and commit.
- Player fills the last destination slot mid-transfer.
- Delayed/duplicated/out-of-order messages and ownership handoff.
- Disconnect/reconnect at each transfer and craft stage.
- Save/reload during escrow, completed output and fuel interval.
- Machine destruction while holding ingredients or an upgraded item.
- Stack metadata conflicts, custom data, quality, variants and provenance.
- Unacknowledged station input enters recovery rather than refund/resend.

For pure transfers: initial world/container/escrow quantity equals final quantity. For recipes: compare a stoichiometric ledger of consumed inputs and produced outputs, not raw item counts. For finite harvesting: native world destruction and awarded drops must have one authoritative source. For renewable sources: count exactly one output per paid legal cycle.

### Graph and energy scenarios

- Supply equals demand, one DU deficit, manual disable, clutch disconnect.
- Incompatible rotation, ratio loops, speed cap, source conflict.
- Split/merge with active flywheel energy and paid generator fuel.
- Zero drive, empty fuel, source destroyed, invalid water, blocked exhaust.
- Disabled/blocked machines have the documented reserved-load behavior.

### Progression and environment

- Unknown recipe, unavailable boss gate, insufficient station level.
- Wrong crop biome/spacing/cover, missing seed, no duplicate harvest.
- Wrong ore tier, unreviewed special material, depleted deposit.
- Unhappy hive, depleted/invalid sap source, missing cooking heat.
- Live time versus sleep jump versus unload/reload.
- World resource modifier applied once through a documented policy.

### Performance and compatibility

- 500-piece test factory with real traffic, two clients and dedicated server.
- Long full-belt jam, full storages, cyclic belt network and repeated topology edits.
- Common auto-fuel/auto-store and crop mods installed individually, with explicit results rather than universal compatibility claims.
- Previous save schema migration and missing/disabled optional content.

Keep a manual QA checklist with game build, machine, observed outcome and logs. Stop broad retesting after the relevant gates pass; spend remaining effort on the next milestone.

## 23. Definition of done and what to report

For each milestone provide:

- Working code and required assets/configuration.
- What a player can now build and automate.
- How to install/run the tested build.
- Automated tests passed and actual in-game checks performed.
- Any checks that could not be performed and why.
- Current coverage, known limitations and next milestone.

For an initial public release, the minimum meaningful loop is **power → wood/stone supply → physical logistics → native processing → automatic crafting/storage**, plus working crop/kitchen basics. A collection of decorative machines is not completion.

Now inspect the repository and begin M0, then implement the first achievable playable milestone. Use the detailed design as the roadmap, not an excuse to rewrite everything in one pass. When the session ends, leave exact resumable state in `docs/ImplementationStatus.md`.

# END CURSOR BUILD PROMPT

---

## Research sources and verification boundaries

Accessed 25 September 2026. These sources support the research notes, not the invented VikingFactory rates, costs or mechanics. Local runtime inspection remains authoritative for implementation.

- **[S1] Iron Gate, Patch 1.0.15, 18 September 2026:** https://www.valheimgame.com/news/patch-1-0-15/ . Current patch and item/save-format context.
- **[S2] ItemHopper Valheim1Compat maintainer release notes:** https://old.thunderstore.io/c/valheim/p/donkeytuesday/ItemHopper_Valheim1Compat/ . Version-specific RPC and item-metadata compatibility findings; not a guarantee for other versions.
- **[S3] Jötunn generated recipe catalogue, labelled Valheim 1.0.7:** https://valheim-modding.github.io/Jotunn/data/objects/recipe-list.html . Evidence of variable ingredient counts, distinct upgrade resources and recipe batch outputs. The catalogue must not substitute for the current local game.
- **[S4] Jötunn Pieces and PieceTables:** https://valheim-modding.github.io/Jotunn/tutorials/pieces.html . Piece registration and 1.0 usage-tag changes.
- **[S5] Create project, Stress Units, Capacity and Impact:** https://github.com/Creators-of-Create/Create/wiki/Stress-Units,-Capacity-and-Impact . Conceptual inspiration; historical documentation, not a current numeric balancing template.
- **[S6] ValheimPipes maintainer package and repository:** https://thunderstore.io/c/valheim/p/coolkidsclub/ValheimPipes/ and https://github.com/Faryzal2020/ValheimPipes . Physical transport precedent; inspect version and license before reuse.
- **[S7] Automatics source repository:** https://github.com/eideehi/valheim-automatics . Existing automation precedent, not a mandatory dependency.
- **[S8] Factorio developers, Friday Facts #364:** https://factorio.com/blog/post/fff-364 . Connected-transport optimisation and update complexity.
- **[S9] Jötunn Persistent & Synced Configurations:** https://valheim-modding.github.io/Jotunn/tutorials/config.html . Existing server-controlled configuration support.
- **[S10] Jötunn recipe integration documentation:** https://valheim-modding.github.io/Jotunn/tutorials/recipes.html . Recipe registration and station relationship overview.
- **[S11] Jötunn generated piece catalogue, labelled Valheim 1.0.12:** https://valheim-modding.github.io/Jotunn/data/pieces/piece-list.html . Secondary implementation checklist; exported live definitions should replace static assumptions.
- **[S12] Satisfactory official game site:** https://www.satisfactorygame.com/ . Factory-building and conveyor inspiration; VikingFactory's manifold rules above are its own explicit design.

No local VikingFactory repository or game binaries were supplied for this research document. The proposed implementation has not been compiled, playtested or benchmarked. The prompt requires Cursor to validate those details against your actual project before enabling them.
