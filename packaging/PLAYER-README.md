# VikingFactory

Early development test build. Expect bugs, and back up your world first.

Mechanical workshops for Valheim: hand cranks, water wheels, steam and sail power, shafts, cogs, belts, feeders, recipe mills, quarries, coppice, fields, and livestock gates. Machines use vanilla materials and follow the game's progression. Native smelting, cooking, fermenting, and growth times are never changed.

## Install

Install on the server and on every client, with the same version:

- BepInExPack Valheim 5.4.2351
- Jötunn 2.30.2
- This package, into `BepInEx/plugins/VikingFactory/`

## Start

The pieces are on the hammer's Crafting tab. Place a hand crank, a shaft, and a bronze feeder between two chests. Hold interact on the crank and the feeder moves one item every two seconds. Add a second feeder and the crank stalls: load has passed drive.

Hover any machine to see its drive, load, speed, direction, and why it has stopped. Console: `vf status`, `vf network`, `vf validate`, `vf coverage`.

## Limits

Factories run only where a player is nearby. Nothing ticks in unloaded areas and sleep is not replayed as extra work. This build has not been tested on a dedicated server or with two clients.

Source, issues, and the full status list: https://github.com/loafdaddy/VikingFactory
