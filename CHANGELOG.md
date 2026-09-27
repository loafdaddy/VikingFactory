# Versions

Patch notes for each VikingFactory update. This is not a player release. A version number means the source on that branch, not a Thunderstore package.

`Version.props` is the number. The plugin constant and `packaging/manifest.json` match it. `main` keeps the last snapshot that was pushed. The next update gets the next number and its own branch.

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
