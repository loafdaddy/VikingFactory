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
| Pieces in code | 39 in `0.3.0`, listed in `src/VikingFactory.Plugin/Machines/MachineCatalog.cs`. The marker is a block. The other 38 load polished or expansion GLBs. |
| Core tests | 66 passed on 27 September 2026. They do not load the game. |
| `0.3.0` load | Headless, 27 September 2026: `Registered 39 of 39 workshop pieces`, 38 models attached, every material slot resolved. Repeated from the packaged ZIP with the same result. No world entered. |
| Earlier load | Headless, 27 September 2026, via `start_game_bepinex.sh -batchmode -nographics` and the isolated save directory from `scripts/launch-dev.sh`. Pieces registered `Valid=True`. No world was entered on that run. A later windowed `0.2.0` client entered a world and placed one water wheel. That wheel floated. |

## What the plugin references

Taken from `Jotunn.xml` shipped in 2.30.2, not from a guessed signature:

- `Jotunn.Main.ModGuid` is `com.jotunn.jotunn`
- `CustomPiece(string name, bool addZNetView, PieceConfig)` creates an empty primitive and adds `ZNetView` when the flag is true
- `PieceManager.AddPiece`, `PrefabManager.OnVanillaPrefabsAvailable`, `PrefabManager.GetPrefab`
- `LocalizationManager.AddLocalization(CustomLocalization)`
- `PieceConfig.AddRequirement(string item, int amount, bool recover)`

The pieces use the prefab ids `Wood`, `LeatherScraps`, `Bronze`, `BronzeNails`, `RoundLog`, and `DeerHide`. `RoundLog` is the core-wood item. The headless load found all of them. Registration of every piece is skipped if `GetPrefab("Wood")` returns null. A missing later material skips only that piece.

## Game methods the adapters call

Read from the local `assembly_valheim.dll` with ILSpy on 27 September 2026, not guessed.

| Station | Insert | Output | Checked on the ZDO |
|---|---|---|---|
| `Smelter` family | `RPC_AddOre(string prefab, bool cheated)`, `RPC_AddFuel()` | Drops at `m_outputPoint`; `RPC_EmptyProcessed()` for stacked output | `s_queued`, `s_fuel`, `s_spawnAmount` |
| `CookingStation` | `RPC_AddItem(string prefab, bool cheated)`, `RPC_AddFuel()` | `RPC_RemoveDoneItem(Vector3, int)` spawns the item | `slot{i}`, `slotstatus{i}`, `s_fuel` |
| `Fermenter` | `RPC_AddItem(int prefabHash, bool cheated)` | `RPC_Tap()`, drops at `m_outputPoint` after 1.5 s | `s_content`, `s_startTime` |
| `Beehive`, `SapCollector` | — | `RPC_Extract()` | `s_level` |
| `Pickable` | — | `RPC_Pick(int bonus)`; reads `Player.m_localPlayer` | — |
| `ItemDrop` | — | `CanPickup(false)`, `RequestOwn()`, `RemoveOne()` | Owner only |
| `TreeBase`, `TreeLog`, `MineRock`, `MineRock5`, `Character` | `Damage(HitData)` with chop, pickaxe, or plain damage and `m_toolTier` | Native drops | — |
| `CraftingStation` | `HaveBuildStationInRange`, `GetLevel()`, `m_craftRequireRoof`, private `m_haveFire` | — | — |
| `Player` | `IsRecipeKnown(sharedName)`, `IsMaterialKnown`, `UseStamina`, `HaveStamina` | — | — |

`ZRoutedRpc` runs an RPC at once when the target is this peer. That is why inserts are only attempted while this peer owns the station.

Boss keys come from each boss prefab's `Character.m_defeatSetGlobalKey`: `defeated_eikthyr`, `defeated_gdking`, `defeated_bonemass`, `defeated_dragon`, `defeated_goblinking`, `defeated_queen`, `defeated_fader`.

## Not verified

Every adapter above in a world, wind, smoke, sleep, and save/load of any piece. No Harmony patch is used. No second client or dedicated server.

## Launch

Steam will not load BepInEx until the game's launch options are:

```
./start_game_bepinex.sh %command%
```

`start_game_bepinex.sh` in the game root is already executable. A headless launch already wrote `VikingFactory 0.1.0 loaded` and `Registered vf_water_wheel. Valid=True`. Starting the game from the Steam window still needs the launch option above. The catalogue from that launch is `BepInEx/config/VikingFactory/catalogue.json`.
