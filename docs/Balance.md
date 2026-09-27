# Balance

These numbers are the master prompt's proposed defaults. They are not measured Valheim rates. Pieces have been placed in a world. None of these rates was timed there. `BalanceDefaults` in the core holds every one of them, and the core tests lock the rules built on them.

## Progress

The cross-off list is `docs/ImplementationStatus.md`. The public summary is `ROADMAP.md`.

- [x] Every source, consumer, and production rate below is in `BalanceDefaults` and used by the `0.3.0` plugin
- [x] Core tests cover the stall rule, ratios, governor, flywheel, fuel, belt rates, mill cycle, quarry, coppice, forage, and extraction rules
- [ ] Any rate observed in a world
- [ ] Playtested tuning

## Sources

| Source | DU | RPM | Rule |
|---|---:|---:|---|
| Hand crank | 8 | 16 | Only while held, 3 stamina a second |
| Water wheel | 48 | 16 | Paddles 0.4 m into water at least 0.5 m deep, 6 m apart, clear wheel |
| Steam engine | 160 | 32 | 1 coal per 30 running s, water intake within 4 m, clear chimney |
| Sail wheel | 0 or 32–256 | 32 | 256 × wind × exposure, nothing below 10 % |
| Eitr motor | 320 | 64 | 1 refined eitr per 120 running s |
| Reinforced steam engine | 320 | 64 | 1 coal per 15 running s |
| Flywheel | up to 80 out | — | Stores 2400 DU·s, charges up to 80 DU at 80 % |

Sources at different speeds on one line need a governor (8, 16, 32, or 64 RPM). A governed source slower than the setting idles instead of stalling the line.

## Consumers

| Consumer | DU | Rate |
|---|---:|---|
| Gravity trough | 0 | 15 a minute, downhill only |
| Timber belt, corner | 1 | 30 a minute |
| Iron belt | 1 | 60 a minute |
| Bronze feeder | 6 | 30 a minute |
| Iron feeder | 8 | 60 a minute |
| Splitter, merger | 0 | Need a turning line |
| Recipe mill | 16 | `max(4, 4 + 2 × ingredient types)` s, up to 2× faster, never under 2 s |
| Advanced assembler | 32 | Same cycle, up to 4× faster |
| Planter | 8 | One planting every 4 s |
| Harvester | 12 | One pick every 4 s |
| Farm gantry | 20 | Both, 8 × 8 m |
| Timber saw | 24 | One stroke every 3 s, 60 chop at tier 2 |
| Bedrock quarry | 32 | 1 stone per 10 s, holds 50 |
| Mining head | 48 | One stroke every 4 s, 60 pickaxe at tier 2 |
| Deep extractor | 96 | Copper or tin 120 s, scrap iron 180 s, silver 240 s, holds 20 |
| Cooking tender | 8 | One action every 2 s |
| Feed gate | 6 | Checks every 4 s |
| Culling gate | 16 | Checks every 10 s |

Feeder and belt speed scales with useful RPM between a quarter and full rate. More RPM than 16 does not raise a feeder past its maximum. Production machines ignore RPM except the mill and assembler.

## Supply

| Supply | Founding | Output | Minimum cycle |
|---|---|---|---|
| Common coppice | 5 beech seeds | 20 wood | 30 min or native growth |
| Pine grove | 5 pine cones | 10 core wood, 10 wood | 40 min or native growth |
| Fine-wood grove | 5 birch seeds or acorns | 8 fine wood, 12 wood | 50 min or native growth |
| Resin tapping | Common coppice | 1 resin | 10 min, holds 5 |
| Forage bed | 5 specimens | 1 of the specimen | 10 min or native respawn, holds 5 |
| Deep extractor | 10 of the ore | 1 ore | As above |

## Worked lines

| Line | Reserved | Supply | Result |
|---|---:|---:|---|
| One crank, one feeder | 6 | 8 | Runs (tested) |
| One crank, two feeders | 12 | 8 | Stalls (tested) |
| Water wheel, 3 feeders, 4 belts | 22 | 48 | Runs (tested with a fake placement probe) |
| Quarry, feeder, 4 belts | 42 | 48 | Runs on one wheel |
| Two quarry lines | 84 | 48 | Stalls on one wheel, runs on two |
