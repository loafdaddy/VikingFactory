# Compatibility

Updated 27 September 2026 after Valheim was installed. Observations were checked on this machine. Assumptions were not.

## Progress

The cross-off list is `docs/ImplementationStatus.md`. A headless load registered the pieces. A later windowed session entered an isolated world and placed one water wheel. Setup steps are `docs/Development.md`. Patch notes are `CHANGELOG.md`.

## Observations

| Check | Result |
|---|---|
| Game root | Local Steam install. The path is `VALHEIM_INSTALL` in gitignored `Environment.props`. |
| Steam app | 892970, build id 25527674 |
| `app.info` | Company `IronGate`, product `Valheim`. No version number in that file. |
| Unity | `6000.0.75f1` (`26349cd2a5c8`), read from `UnityPlayer.so` |
| `assembly_valheim.dll` | SHA-256 `68f3baa9454653e5d3cdde49ad8932d36c13716caaa15b352aa3fb530ad63d9c` |
| `UnityEngine.CoreModule.dll` | SHA-256 `4cb2a683351f6644de26086d01e7b5879fcf228ee0dbd0d7cc0536570bd15681` |
| BepInEx pack | denikson-BepInExPack_Valheim 5.4.2351, extracted into the game root |
| Jötunn | ValheimModding-Jotunn 2.30.2, in `BepInEx/plugins/Jotunn`. Its manifest asks for BepInEx pack 5.4.2333. 5.4.2351 is the newer pack that was current on Thunderstore. |
| Plugin target | `net472`, compiled against those local DLLs, plus `assembly_utils` for `Vector2i`. `VikingFactory.dll` and `VikingFactory.Core.dll` are in `BepInEx/plugins/VikingFactory`. |
| Pieces in code | `vf_marker`, `vf_crank`, `vf_shaft`, `vf_clutch`, `vf_water_wheel`, `vf_belt`, `vf_feeder`, `vf_basket`. The marker is still a block. The other seven load polished GLBs. |
| Core tests | 13 passed on 27 September 2026. They do not load the game. |
| In-game load | Headless, 27 September 2026, via `start_game_bepinex.sh -batchmode -nographics` and the isolated save directory from `scripts/launch-dev.sh`. Pieces registered `Valid=True`. No world was entered on that run. A later windowed `0.2.0` client entered a world and placed one water wheel. That wheel floated. |

## What the plugin references

Taken from `Jotunn.xml` shipped in 2.30.2, not from a guessed signature:

- `Jotunn.Main.ModGuid` is `com.jotunn.jotunn`
- `CustomPiece(string name, bool addZNetView, PieceConfig)` creates an empty primitive and adds `ZNetView` when the flag is true
- `PieceManager.AddPiece`, `PrefabManager.OnVanillaPrefabsAvailable`, `PrefabManager.GetPrefab`
- `LocalizationManager.AddLocalization(CustomLocalization)`
- `PieceConfig.AddRequirement(string item, int amount, bool recover)`

The pieces use the prefab ids `Wood`, `LeatherScraps`, `Bronze`, `BronzeNails`, `RoundLog`, and `DeerHide`. `RoundLog` is the core-wood item. The headless load found all of them. Registration of every piece is skipped if `GetPrefab("Wood")` returns null. A missing later material skips only that piece.

## Not verified

Station RPCs, wind, smoke, sleep, and save/load of any piece. No Harmony patch calls into the game. Basket and clutch state are written to ZDO fields in code, and that write has not been observed in a world.

## Launch

Steam will not load BepInEx until the game's launch options are:

```
./start_game_bepinex.sh %command%
```

`start_game_bepinex.sh` in the game root is already executable. A headless launch already wrote `VikingFactory 0.1.0 loaded` and `Registered vf_water_wheel. Valid=True`. Starting the game from the Steam window still needs the launch option above. The catalogue from that launch is `BepInEx/config/VikingFactory/catalogue.json`.
