# In-world QA checklist

The milestone gates that core tests cannot close. Run them in the isolated world from `scripts/launch-dev.sh`, in order. Each later step builds on the earlier ones. For each step, record the game build, plugin version, what you saw, and any `BepInEx/LogOutput.log` lines from VikingFactory, then tick the matching box in `docs/ImplementationStatus.md`.

Before starting: press F5, run `devcommands`, then `debugmode` (it resets every launch). Pieces are on the hammer's Crafting tag. `vf status` and `vf network` print the live lines.

## 1. M0 and M1: first line

1. Remove the floating `0.2.0` wheel. Place a new water wheel on dry ground. Its feet should touch the ground without Shift. Hover it: "Paddles are not in water."
2. Check the hammer buttons show the rendered pictures.
3. Place a chest, a bronze feeder with its back at the chest, a basket at its front, a shaft from a hand crank to the feeder's kinetic port. Put 20 wood in the chest.
4. Hold interact on the crank. Expected: stamina drains, the shaft and feeder arm move, one wood every 2 s into the basket. Hover shows "8 DU supply / 6 DU reserved".
5. Add a second feeder on the same shaft. Expected: "Overloaded", both stop, nothing moves.
6. Add a reversing cog in the line. Expected: the segment after it spins the other way, hover says counter-clockwise.
7. Save, quit to menu, reload. Expected: basket contents and chest count add up to 20.
8. Destroy the basket with the hammer. Expected: its items drop once on the ground.

## 2. M1: belts

1. Chest → feeder → timber belt → belt → basket, powered by a water wheel standing in a real stream at least 0.5 m deep.
2. Expected: items travel, the deck scrolls only while the line turns, the wheel reports "In water".
3. Block the basket (fill it). Expected: belts fill to four items each and stop. Nothing drops.
4. Save and reload with items mid-belt. Expected: the same items are still on the belts.

## 3. M2: kiln and smelter

1. Wood belt → feeder into a charcoal kiln. Feeder with its back at the kiln's chute → basket.
2. Expected: coal appears at the native rate. The kiln smokes. Blocking the chimney stops the kiln without deleting wood.
3. Ore and coal lines through a priority splitter into two smelters. Expected: the first fills first, overflow goes to the second. Bars collected once each.
4. With two clients near the line, let the first leave. Expected: the line pauses briefly, then continues on the second client with no duplicated bars.

## 4. M3: recipe mill

1. Forge in range. Use bronze on a mill: expected "No recipe makes that" or "Taught" only for a recipe the character knows. Use bronze nails to teach nails.
2. Bronze by belt into an input port. Expected: 20 nails per bronze, every 6 s at 16 RPM.
3. Remove the forge mid-craft. Expected: "Station missing … Ingredients held." Rebuild it: the job finishes once.
4. Shift+interact. Expected: held bronze leaves through the output.
5. Teach a cauldron recipe with five ingredients near a cauldron and repeat.

## 5. M4: wood and stone

1. Quarry on natural ground by a stonecutter. Expected: one stone per 10 s while powered, stops at 50. A second quarry 8 m away reports "Another quarry is within 12 m".
2. Coppice bed in the open Meadows. Use 5 beech seeds. Expected: growth percentage rises only while the area is loaded. A timber saw within 6 m takes 20 wood when mature.
3. Two stakes around three planted beech trees, a saw beside them, beech seeds in the saw's input. Expected: the saw fells them with native drops and replants the stumps.
4. Dismantle and rebuild the bed. Expected: it starts unplanted, with no refund of the seeds.

## 6. M5: fields and kitchens

1. Cultivate a 4 × 4 m plot. Planter with carrot seeds. Expected: seeds planted with native spacing. Harvester on a second plot of carrots.
2. Carrot seed field → seed belt → food field planter.
3. Cooking tender between a chest of raw meat and a cooking rack over fire. Expected: it loads only while the chest has room for cooked meat and unloads when done. Stop the power: meat burns.
4. Mill a mead base at a cauldron, feed a fermenter, wait two in-game days, tap by feeder.

## 7. M6: power

1. Steam engine with a water intake in a stream and coal. Expected: 160 DU at 32 RPM, one coal per 30 running seconds, "Exhaust blocked" when the chimney is roofed.
2. Water wheel and steam engine on one line without a governor. Expected: rotation conflict. Add a governor at 16: both contribute.
3. Sail wheel in the open and under trees. Expected: capacity follows wind and openness.
4. Flywheel on a sail line. Expected: stored energy rises, then carries the line through a short calm.

## 8. M7 and M8

1. Mining head against a copper deposit. Expected: native drops, deposit depletes.
2. Deep extractor before Moder: "Locked". After Moder in the Black Forest with 10 copper ore: one ore per 120 s, capacity 20.
3. Forage bed with 5 raspberries, harvested by a feeder.
4. Feed gate beside tame boars with carrots. Culling gate enabled with six tame adult boars and a reserve of four: expected one unnamed, lowest-level adult taken, named boars never.
5. `vf coverage` in the world and compare with `docs/AutomationCoverage.csv`.
6. Dedicated server: install the Valheim Dedicated Server app, set `VF_DEV_SERVER_PASSWORD`, run `scripts/launch-dev-server.sh`, connect two clients, repeat steps 1, 3, and 4, and leave a line running for an hour.
