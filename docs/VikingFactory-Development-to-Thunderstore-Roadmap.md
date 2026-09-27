# VikingFactory: from project to Thunderstore release

Internal workflow and research notes from 25 September 2026. The public roadmap is `ROADMAP.md`. What is built is `docs/ImplementationStatus.md`.

This file is a proposal for how a future package could be made. It is not a release checklist that has been completed. A headless load registered the current pieces. No world was entered. Do not publish from this document.

## 1. Direct answers

### How do I create the assets?

Start with simple working machines. Use runtime kitbashing of existing game parts or original primitive models for prototypes, then replace their appearance with original Blender models. Assemble those models into Unity prefabs and export AssetBundles. Cursor writes the C# that loads the prefabs, gives them behavior and registers them with Valheim. Jötunn documents both kitbashing and asset loading. [R1, R2]

Your finished mod has two main deliverables: compiled behavior in `VikingFactory.dll`, and visual/prefab assets in bundles. Textures, icons, audio and localisation are supporting assets. An AI-generated concept picture is useful reference material but is not a finished game model with pivots, UVs, collision, ports and logic.

For VikingFactory, start with a reusable timber-and-iron parts kit. Shafts, brackets, gears, belt supports and machine frames can share shapes and materials. The first water wheel is a better learning project than a complicated animated assembler.

### How does BepInEx fit in?

**BepInEx is the loader that runs your mod inside Valheim.** Your project compiles C# into a DLL, which the loader finds under its plugins directory. It also provides logging, configuration and dependency helpers. [R3]

**Jötunn is a helper library your plugin uses**, especially for registering custom pieces, recipes and assets. **Harmony is a method-patching tool**, used sparingly where an existing game operation needs integration. Cursor is the editor/assistant producing your code, not the loader.

Use the Valheim-specific BepInEx pack and a tested Jötunn version. Do not automatically choose a generic BepInEx download or a different major version because its version number is higher. The pack is configured for Valheim. [R4]

### How will multiplayer work?

VikingFactory's proposed support policy is **install it on the server and every client**, with compatible dependencies and matching mod versions during early development. New machines and messages require everyone to understand the same content. Jötunn supports checking these requirements when players connect. [R5]

The server sets gameplay configuration, while the implementation must coordinate authoritative world objects and synchronize the results. A wheel's animation can run locally on every screen; the inventory change it causes must happen once. “BepInEx is installed” does not automatically solve networking.

Factories follow the master design's active-area rule. An online dedicated server does not imply unattended factories across the entire map continue producing. Remote simulation/chunk loading is explicitly outside the first release.

### Does Cursor need game files?

**Local access to your installed game's managed assemblies is the normal practical route. You do not need to upload the whole game into the chat.** Give Cursor the installation path, your modded development-profile path and the project. Jötunn's development workflow supports configurable local references and generated publicized references. [R6]

The compiler uses assemblies to resolve game types. Cursor may need selected locally inspected method signatures or decompiled methods to understand actual behavior. A DLL is not plain-text source code just because you attach it to a prompt. Build tools, a local assembly inspector and runtime diagnostics do the useful work.

Keep original/publicized game assemblies out of the public repository and release ZIP. Thunderstore explicitly prohibits distributing game files without permission. [R7]

## 2. Recommended development setup

Use one primary development computer at first. Windows or Linux can work, but pick the OS you are actually using and make the build scripts match it. Do not mix instructions for native Linux Valheim, Windows Valheim under Proton and Windows native paths.

| Tool | Job | When needed |
|---|---|---|
| Cursor | Edit C#, review changes, run local scripts, diagnose build errors | Immediately |
| Git and a source repository | History, releases, working branches and recovery | Immediately |
| Installed Valheim | Actual game assemblies and in-game testing | Immediately |
| Valheim mod manager or isolated manual profile | Separate development dependencies from normal play | Immediately |
| Valheim BepInEx pack | Load the plugin and dependencies | Immediately |
| Jötunn | Game-content registration and common integration helpers | Immediately |
| Compatible .NET SDK/build tools | Compile the existing project's target framework | Immediately |
| Local assembly inspector, e.g. ILSpy | Inspect relevant game definitions | When an adapter is written |
| Blender | Original models, UVs and exported meshes | Once prototype behavior works |
| Unity Hub + verified editor version | Assemble prefabs and build bundles | Before custom asset export |
| Image editor | Texture atlas, machine icons, package icon | Art pass |
| Dedicated test server | Network and persistence verification | As soon as item transfer exists |

The current Jötunn asset guide names Unity 6000.0.61. Treat that as a research snapshot: check your installed game's log/editor compatibility before creating the asset project, and pin the verified version. Do not let Unity Hub silently upgrade it. [R8]

The installed SDK version and the mod's target framework are different things. Cursor should inspect the existing `.csproj` and a current working template. A newer SDK is not a reason to retarget the plugin to a runtime the game cannot load.

### Three environments

| Environment | Purpose | Rule |
|---|---|---|
| Development profile + disposable world | Fast rebuilds, debug tools, destructive experiments | Only your isolated plugin folder is auto-deployed |
| Clean QA profile + test-world copies | Reproduce how a new user installs it | Install the release ZIP, not a loose developer DLL |
| Ordinary play profile/world | Your existing game | Never the initial target of automated deployment |

A separate mod profile does not necessarily isolate character and world saves. Use a deliberately named test world and test character; where supported, configure a separate save directory. Confirm which saves are being used before destructive tests.

Jötunn warns that loading modded worlds without required mods can lose modded content. Back up the actual world and character state as well as the profile. [R9]

## 3. What to give Cursor on day one

Provide these facts in a local project setup note:

| Input | Example/instruction |
|---|---|
| Project location | Your current VikingFactory repository |
| Game root | Locate through Steam's installed-file browser; use the exact path |
| Game build | Record from game/log, plus relevant assembly hashes |
| Development BepInEx directory | Use the profile actually launched, which may not be under the Steam game directory |
| Development plugins directory | Explicit deploy destination for VikingFactory only |
| Current mod dependencies | Tested BepInEx pack and Jötunn package versions |
| Target OS/runtime | Windows native, Linux native, or explicitly tested Proton setup |
| Test world and server | Clearly separate from normal gameplay |
| Design files | Both the master prompt and this roadmap |

Typical Windows/Linux game installations contain a `valheim_Data/Managed` directory. Discover its exact case and layout locally; do not hard-code a Windows path into every project file. Relevant references usually include `assembly_valheim.dll`, the Unity modules actually used, and dependency assemblies. Resolve secondary dependencies from the installed version rather than copying a static list from an old tutorial.

Use a private local configuration file, such as the template's `Environment.props`, to reference game/dependency/deploy paths. Preserve the project's existing reference system if it already works; avoid adding duplicate auto-reference tasks. [R6]

Publicizing creates a development reference with otherwise inaccessible members exposed. It is not an instruction to overwrite the original game assembly. Keep generated files in a disposable ignored directory, refresh them when game binaries change, and validate runtime access behavior.

For a cloud-only Cursor agent, your desktop installation will not automatically be accessible. Give that agent the public project and interfaces/tests it can build without game binaries; run integration builds locally or on a properly provisioned private runner. Never solve this by committing the entire Managed directory to a public repo.

### Repository layout to ask Cursor to maintain

| Relative location | Contents |
|---|---|
| `src/` | Plugin code and pure simulation core |
| `tests/` | Core invariants and integration harness support |
| `unity/Assets/VikingFactory/` | Your own prefabs, materials, editor tooling |
| `art-source/` | Original Blender files, texture sources, source audio |
| `docs/` | Design, setup, compatibility, coverage, testing and current status |
| `scripts/` | Build, local deploy, asset export and package validation |
| `packaging/` | README, manifest template, icon, licence notices |
| `artifacts/` | Generated DLLs, bundles, ZIPs; ignored or release-managed |
| Private local reference directory | Game/publicized references; ignored, never packaged |

Commit Unity `.meta` files for your own assets, plus project settings and package manifests. Ignore Unity caches such as `Library`/`Temp`, build intermediates, local path configuration, saves, credentials and copied game references. Avoid a blanket `*.dll` rule as your only protection: audit the package contents explicitly.

## 4. Asset production workflow

### Stage A: functional prototypes

Ask Cursor to make one consistent prototype kit with readable physical ports and dimensions. Use primitive geometry or runtime kitbashes; the latter reuse game parts by reference at runtime instead of shipping extracted original meshes. Jötunn has kitbash and mock-reference mechanisms for this. [R1, R10]

First prototype pieces: crank, shaft, wheel, feeder, belt, basket. Color input/output/service points differently in developer mode. Confirm snapping, collision and orientation before detailed art.

### Stage B: original modular art kit

Create these reusable components in Blender:

- Timber beams and cross-braces.
- Iron straps, bolt heads and axle bearings.
- Wooden cog variants and a wheel hub.
- Conveyor supports, rollers and hide/wood belt surfaces.
- Small stone foundation variations.
- Chutes, baskets and service panels.

Proposed art targets, not engine limits: small connectors around 100–500 triangles, ordinary machines around 1,000–4,000, large hero pieces around 3,000–8,000. Start with one shared 512- or 1024-pixel material atlas per visual family and very few material slots. Measure in a dense factory before raising budgets. Draw calls and shader complexity matter as well as polygon count.

Scale matters more than detail. Use the existing 2 m belt module and 1 m snapping subdivision. Check a finished model next to the player and vanilla furniture. Do not let a wheel's decorative axle silently change the logical connection location.

### Stage C: one complete water-wheel asset

1. Define footprint, axle height, wheel radius, interaction point and shaft connection in a small asset brief.
2. Model the fixed frame and rotating wheel as separate objects.
3. Put the wheel pivot at the axle centre; put the complete machine's origin at the agreed placement anchor.
4. Make UVs and simple readable materials; apply/check transforms and normals.
5. Export the mesh, for example as FBX, and inspect its scale/orientation in Unity. Blender documents its FBX export options. [R11]
6. Create a prefab with stable children for frame, rotor, collision, interaction and ports.
7. Configure or add the required native/mod components using the verified integration route. Build previews, support, destruction and networking are not supplied by the mesh alone.
8. Export a bundle and have the plugin load/register the piece.
9. In game, build it, connect a shaft, load it, stall it, destroy it and reload the world.
10. Only then reuse the pattern across the art catalogue.

The exact Unity component list is determined by the piece adapter. Avoid guessing that every clone already contains a valid network identity or stripping components needed for save persistence.

### Animation, sound and icons

For the first machines, rotate rigid child objects from replicated state. An elaborate character rig is unnecessary for a shaft. Feeders may later use authored clips or simple procedural motion, with authoritative item ownership separate from the animation.

Make collisions simple. The appearance of a turning cog does not require simulating every tooth. Whether blades cause gameplay damage is a separate deliberate feature.

Use original or properly licensed short audio loops, volume falloff and an overall factory-audio limit. A hundred machines should not create a hundred equally loud loops. Produce clear machine icons from a consistent view, and test the package icon at its actual small display size.

### AssetBundles and platforms

Bundle data and executable code are different deliverables. Unity documents platform-specific bundles and excludes script assets from the bundle's executable-code role. Build for each supported target, select the correct bundle at runtime, and keep required script types available in the DLL. [R12]

Begin with the actual development client platform and a Linux dedicated-server target if that is your hosting goal. Do not advertise Mac or Steam Deck support without appropriate tests. A headless server still needs the registered network prefab/component data; it need not render the art. Establish a tested server asset-loading route early.

Avoid renaming public prefab IDs or serialized script identities once testers have built machines. A later art replacement should preserve saved instances.

### What Cursor can reasonably handle

| Work | Cursor's role | Your role |
|---|---|---|
| C# behavior and build scripts | Write, refactor, compile and test | Review behavior and playtest |
| Repetitive model geometry | Generate Blender scripts if Blender is available | Check shape, scale and appearance |
| Unity prefab/bundle automation | Write editor scripts | Run/inspect Unity and fix references |
| Visual style | Turn your direction into asset briefs | Choose the look and approve assets |
| Network integrity | Implement/test core invariants and diagnostics | Run real multiplayer scenarios |
| Balance | Calculate rates and identify loops | Play survival progression and tune |

Cursor may automate parts of Blender or Unity through scripts, but do not assume it can inspect and operate their graphical interfaces in your setup. Ask it to state what it actually executed.

## 5. BepInEx development loop

Your first successful build should only load, log its version and register one inert piece. Establish that before the first machine network.

1. Install tested dependencies into the isolated development profile.
2. Launch that profile once and check that the loader and Jötunn appear in its log.
3. Configure plugin identity, version and dependencies in the project.
4. Build with the selected toolchain. For an SDK-style project this may use `dotnet build`; legacy project setups may need their existing MSBuild route.
5. Copy only VikingFactory's output to the configured plugin directory, preferably with a repeatable deploy script.
6. Restart the game and test in the disposable world.
7. Read the active profile's `BepInEx/LogOutput.log` and relevant game/server logs. Find the first actionable error, not just the final cascade.
8. Fix, rebuild, retest and commit the working slice.

BepInEx loads DLL plugins and provides logger/configuration facilities; you generally do not need to write your own loader. [R3]

Ask Cursor for editor tasks named **Build**, **Test Core**, **Deploy Dev**, **Build Assets**, **Package**, and **Validate Package**. These are requested project automation, not claims that those commands already exist. A deploy task must reject empty paths, refuse the source game Managed directory and never recursively clear the entire plugins directory.

Do not assume hot reload is safe. For ordinary development, exit the test game before replacing a loaded DLL and relaunch. Keep the last known-good package so a bad build can be isolated quickly.

## 6. Multiplayer engineering and server setup

### Recommended support contract

| Scenario | Initial policy |
|---|---|
| Solo modded PC game | Supported after local tests |
| Friend-hosted world | Supported after two-client ownership tests |
| Modded dedicated server | Supported after separate server tests |
| Only the server has VikingFactory | Reject; clients need custom content |
| Different VikingFactory builds | Reject during alpha; loosen only with a tested protocol policy |
| Vanilla console client | Not supported by this BepInEx mod design |
| No players near factory | No continuous custom factory simulation in initial release |

These are VikingFactory design choices. Jötunn provides compatibility checks, but you must configure them and implement correct synchronization yourself. [R5]

### Separate authoritative work from presentation

- One accepted action removes an item from a source and delivers it once.
- Other clients display belt travel, sounds and rotor movement from state.
- Server-controlled configuration determines costs, capacity and progression.
- Inventory and station adapters respect the actual owner of their game objects.
- A new owner resumes persistent work rather than starting a second copy of it.

Do not simplify this to “the server runs absolutely everything” unless the actual implementation establishes that authority for those objects. Valheim integrations must cope with object ownership and handoff. The master prompt's transaction and recovery design remains the engineering target, not a promise of crash-proof behavior before testing.

### Smallest useful multiplayer test

Run one dedicated test server and connect two genuine clients. A second computer/friend can provide the second client. Test:

1. Both see the same machines and inventories.
2. Both attempt to take the final item while a feeder is running.
3. The first player leaves the area; the second stays.
4. The first player disconnects while a transfer is in progress.
5. The server restarts with items buffered and a job partly complete.
6. Both players return after the area has unloaded.
7. The machine is dismantled while holding inputs.
8. A client with an incompatible build is rejected clearly.

### Hosted servers

For your chosen provider, check custom plugin upload access, dependency installation, logs, backup/restore and startup control. Do not assume its one-click mod menu accepts unpublished ZIPs. If it cannot host development builds, use a separate local dedicated server until you have a published package.

Deploy in this order: stop server, back up, update server plugin/dependencies, distribute matching client build/profile, start server, inspect logs, run a brief join/save test. Avoid automatic live updates while people play.

A mod profile can help friends get matching dependencies, but private locally imported mods may need separate ZIP distribution. Thunderstore documents that local mods do not automatically gain dependency installation or normal profile-sharing behavior. [R13]

## 7. Development roadmap

Release numbers below are proposed labels, not promised compatibility or dates. The full master design is a substantial project; use gates rather than committing to a calendar before inspecting the repository. Advanced fishing, endgame-specific behavior and broad mod compatibility can each add significant work.

### Phase 0 — Baseline and reproducible setup

**Work:** audit existing code, pin references/dependencies, isolate test saves, make build/deploy scripts, choose stable plugin identity, record compatibility.

**Output:** a clean checkout can be set up using documented local paths; the plugin logs its version in game.

**Gate:** no game binaries in Git/package; no guessed framework or APIs; no automatic deployment to normal play.

**Cursor brief:** inspect and repair the environment before adding gameplay. Do not replace working project scaffolding unnecessarily.

### Phase 1 — First asset and saved piece

**Work:** simple original/kitbashed machine, icon, placement, collision, support, destruction and saved identity. Export one original asset bundle as a pipeline proof.

**Output:** one machine can be built, seen by another client and restored after save/reload.

**Gate:** no missing scripts/materials; second placement does not reuse an invalid identity; headless registration works.

### Phase 2 — Power and item integrity

**Work:** crank, shaft, wheel, clutch, fixed-speed DU model, feeder, basket, short belt and chest dock. Implement persistent buffering and transaction diagnostics.

**Output:** a powered chest-to-chest transfer line. One crank runs one feeder but stalls under the designed larger load.

**Gate:** two-client races, handoff, full output and save/reload preserve items and metadata. This gate precedes dozens of new machines.

### Phase 3 — Native processing and recipe mill

**Work:** kiln/smelter adapters, splitter/merger, fuel ports, output collection, recipe teaching, station checks and craft escrow.

**Output:** ore/fuel into processing, bars into the mill, nails into storage. One verified food-preparation recipe works too.

**Gate:** smoke/fuel/time rules remain native; output count is correct; missing station or changed level pauses safely; recipes with five ingredients work.

### Phase 4 — Renewable construction supplies

**Work:** quarry, managed coppice, real forestry where safe, founding-stock persistence, plot validation and reserve routing.

**Output:** visible power → renewable wood/stone → kiln/crafting → storage loop.

**Gate:** no repeated commissioning refunds, cooldown resets or overlap exploits. Survival-mode construction costs are achievable and useful.

### Phase 5 — Private alpha, proposed 0.1.0

**Work:** package the tested core, install it into a clean profile, recruit a small group of friends/testers, collect reproducible bug reports.

**Output:** a private ZIP plus precise installation instructions and feature list. This is an alpha package, not a completed overhaul.

**Gate:** testers can install without your development machine, identify blocked states and recover their world backup. No known reproducible item duplication or destructive save bug.

### Phase 6 — Fields and food

**Work:** crop/seed relationships, planter, harvester, replant reserves, hives, cooking, oven and fermenter adapters.

**Output:** at least one complete seed-to-meal chain and one real fermenter chain.

**Gate:** seed reserve prevents extinction, native growth/biomes apply, food can burn under documented conditions, sleep/unload behavior is understood.

### Phase 7 — Public alpha preparation, proposed 0.5.0

**Work:** focused art pass on existing machines, diagnostics, tutorial factory, bug triage, clean install tests and release documentation.

**Output:** a coherent limited release covering power, transport, wood, stone, basic crafting and kitchen/farm loops. More advanced features remain explicitly planned.

**Gate:** repeatable tests on the claimed platforms, dedicated server checked, package validated, feature claims match reality. Publish to Thunderstore only after this gate, if you choose to release an alpha.

This is the earliest recommended public release, not an instruction to advertise the entire master design as available.

### Phase 8 — Expanded industry, proposed 0.6.x–0.8.x

**Work:** steam/sail power, governors, ratios, flywheels, improved routing, finite mining, later old-tier extraction, sap/eitr integration and advanced assemblers.

**Output:** progressive factory tiers with late-game incentives.

**Gate:** energy/fuel conservation, progression gates, recovery, migration and a meaningful survival playtest. No unbounded RPM exploit.

### Phase 9 — Broader coverage and art completion

**Work:** reviewed forage beds, livestock rules, equipment upgrades, any proven fishing implementation, current endgame adapters and compatibility profiles.

**Output:** generated automation coverage report for the supported game build, with every excluded or manual-supply family explained.

**Gate:** unknown recipes fail safely; unique resources retain their intended sources; feature status is honest. A blocked fishing adapter must be disabled and documented rather than replaced with unplanned free spawning.

### Phase 10 — Beta and feature freeze, proposed 0.9.x

**Work:** no new broad systems; fix stability, performance, UX, controller interactions, localisation, assets, packaging and upgrade migrations.

**Output:** release candidate ZIP with a tested compatibility matrix and known limitations.

**Gate:** normal new-user installation and update from the previous public version both pass. Network races, destruction, load boundaries and large jammed factories are exercised.

Run at least several scheduled multiplayer playtests and a longer dedicated-server session with a factory under load. Choose a soak length such as 8–24 hours as a project QA target, not proof of universal stability. Record results and unresolved bugs.

### Phase 11 — Stable 1.0 release

**Work:** select the already-tested release candidate, finalise version and documentation, package and publish. Do not rebuild with untested code after sign-off.

**Output:** source tag, changelog, checksummed ZIP, Thunderstore package, support link and installation/upgrade guide.

**Gate:** no known reproducible loss/duplication or destructive migration defects; no experimental features enabled by accident; all advertised features passed their tests.

If scope is reduced for 1.0, state the reduction explicitly. “Stable” describes the supported feature set, not automatic completion of every wish-list feature.

### Phase 12 — Maintenance

**Work:** triage reports, test new Valheim/dependency releases, update adapters, preserve old saves, patch critical issues and evolve the design separately.

**Output:** predictable patch releases and a public supported-version table.

**Gate:** each patch is installed and tested as a package, not merely compiled. Keep expansion work on a separate branch so urgent compatibility fixes do not ship unfinished machinery.

## 8. The repeatable release pipeline

Ask Cursor to implement these steps as explicit project scripts/tasks:

1. Verify exact dependency/game reference versions and a clean release source revision.
2. Run pure-core tests and fail on conservation or graph regressions.
3. Build the plugin in Release configuration.
4. Build or select previously verified bundles for supported targets; record their hashes and source revision.
5. Stage only approved distributable files in a new package directory.
6. Generate matching plugin/package versions from one source.
7. Validate metadata, icon, file paths and prohibited contents.
8. ZIP the staged contents without an extra enclosing directory.
9. Install that ZIP into a clean QA profile and test.
10. Record the exact tested ZIP checksum and source tag.
11. Upload that ZIP after release sign-off.

Do not let a normal build command upload publicly. Make publishing a separate explicit operation. Keep any publishing token outside the repository.

For CI, pure-core tests can run without a local game install. Full integration builds need legitimately available references, and Unity asset builds need a properly configured licensed editor environment. Start with reproducible local packaging or a private provisioned runner; do not publish reference DLLs as CI artifacts.

## 9. Thunderstore package

Thunderstore requires a ZIP with `manifest.json`, `README.md` and an exactly 256 × 256 PNG named `icon.png` at its root. The manifest includes name, three-part version, website URL, description and dependency strings. Use actual tested dependency identifiers, not placeholder versions. [R14]

Recommended staging layout:

| ZIP path | Content |
|---|---|
| `manifest.json` | Generated package metadata |
| `README.md` | Current features, installation and limitations |
| `icon.png` | 256 × 256 package icon |
| `CHANGELOG.md` | Changes for this version |
| `LICENSE` | Your chosen code licence |
| `BepInEx/plugins/VikingFactory/VikingFactory.dll` | Compiled plugin |
| `BepInEx/plugins/VikingFactory/Assets/...` | Bundles/data used by the tested loader |
| `BepInEx/plugins/VikingFactory/THIRD_PARTY_NOTICES.md` | Required asset/library notices, if applicable |

The manager treats recognized BepInEx directories specially, so use its supported paths and verify the resulting installation. A ZIP that looks tidy in an archiver may still install assets incorrectly if folder routing is wrong. [R15]

Do not bundle BepInEx or Jötunn inside VikingFactory when they are installed as dependencies. Also exclude game/Unity runtime assemblies, publicized references, source-game assets, test worlds, local configs, Unity caches, credentials and compiler garbage. Your own original assets and any correctly licensed redistributable dependencies are distinct from copied game files.

### Manifest template, not ready to upload

Replace bracketed values before validation. The dependency placeholders deliberately are not valid package identifiers.

```json
{
  "name": "VikingFactory",
  "version_number": "0.5.0",
  "website_url": "[your actual project URL]",
  "description": "Mechanical power and physical automation for Valheim. Build production lines with shafts, belts, farms and workshops.",
  "dependencies": [
    "[tested Valheim BepInEx dependency string]",
    "[tested Jotunn dependency string]"
  ]
}
```

### README contents

Describe the actual build: supported game/mod versions, features currently present, server-and-client requirement, setup, a first-factory tutorial, progression, configuration, loaded-area behavior, known conflicts, backup/upgrade/uninstall procedure and reporting link.

Separate planned features from available ones. Show at least one real in-game screenshot and a short demonstration of the core loop when ready. Source-code transparency and test results help contributors understand what they are joining.

### Publishing sequence

Create/select the correct Thunderstore team and Valheim community, confirm the final package name, choose applicable categories, upload the tested ZIP and inspect the resulting page. Then install through the manager as a normal user to verify the published package.

With Cursor-generated code or AI-generated assets, check the community's **AI Generated** category. Thunderstore's current global rules call for that label when available, prohibit untested/nonfunctional AI-generated mods, and require accurate descriptions. This generally is not enforced merely for a generated README or icon. [R7]

This roadmap does not perform or authorise an actual upload; it prepares the path to one.

## 10. Upgrade, rollback and uninstall

Thunderstore versions are updated by publishing a higher version; an existing version cannot simply be edited in place. Do not expect uploading a lower number to replace the latest release. [R16]

Before a server update, record the exact plugin/dependency set and back up all world-state files used by the game plus relevant configuration. Clients should retain appropriate character backups too. Restore version-consistent state when rolling back a migration; replacing just the DLL may not reverse a changed save schema.

Provide a safe removal path: stop machines, reconcile work/escrow, empty buffers, dismantle custom pieces using the still-installed mod, save and back up, then remove it. If data is shared among machines, include a tested administrative check for remaining content. Simply uninstalling the DLL must not be advertised as harmless to an existing factory world.

For a critical broken release, stop recommending it, publish a tested higher-version correction and provide recovery guidance. Keep package identity and saved prefab IDs stable.

## 11. Practical release checklist

- [ ] Current source revision recorded. Game build and dependency versions are in `docs/Compatibility.md`.
- [x] Build succeeds from documented local setup.
- [x] Automated core tests pass.
- [ ] No known reproducible loss/duplication issue. No world test has been run, so this stays open.
- [ ] Save/load, owner handoff and machine destruction tested.
- [ ] Dedicated-server and two-client tests completed.
- [ ] Native processing, growth and progression constraints checked.
- [ ] Large jammed factory profiled; measurements recorded.
- [ ] All supported-platform bundles and headless registration tested.
- [ ] New installation and previous-version upgrade tested.
- [ ] Recovery/rollback/uninstall instructions reviewed.
- [ ] No private/game-reference files in ZIP or public repo.
- [ ] Code/assets have recorded origins and required notices.
- [ ] Plugin and manifest version match.
- [ ] Icon, README, manifest and ZIP paths validate.
- [ ] README only claims tested features/platforms.
- [ ] Appropriate Thunderstore categories selected.
- [ ] Published ZIP is the one that passed QA.
- [ ] Post-publication clean install confirmed.

## 12. Your next Cursor instruction

Copy this into Cursor with both design documents available:

> Read the VikingFactory master prompt and development-to-Thunderstore roadmap, then audit this existing repository. Preserve working code. First make the project reproducibly build and load in an isolated Valheim development profile. Discover the installed game, target framework and dependency versions; ask me only for paths you cannot resolve. Keep game/publicized assemblies and personal paths outside Git and the release package. Add build, test, safe deploy and package-validation tasks. Start with one saved buildable prototype, then a crank/shaft/water-wheel system and one metadata-preserving chest-to-chest feeder. Bring in a second client and dedicated server before expanding the machine catalogue. Use simple assets until the core works, but prove one custom Unity asset-bundle pipeline early. Do not claim anything has passed an in-game test unless it was actually run. Maintain docs/ImplementationStatus.md with completed checks, blockers and the exact next task. Do not publish publicly as part of a normal build.

Your first personal task is to locate the actual game directory and create an isolated development profile/test world. Your first joint success with Cursor is a tiny plugin that loads correctly. The first major gameplay success is a reliable powered transfer, not a finished model catalogue.

## Sources

Checked 25 September 2026. Version-dependent guidance should be rechecked when setting up or releasing. Project-specific workflows, art budgets, gates and version labels above are recommendations.

- [R1] Jötunn kitbashing: https://valheim-modding.github.io/Jotunn/tutorials/kitbash.html
- [R2] Jötunn asset loading: https://valheim-modding.github.io/Jotunn/tutorials/asset-loading.html
- [R3] BepInEx plugin overview: https://docs.bepinex.dev/articles/dev_guide/plugin_tutorial/index.html
- [R4] Valheim-specific BepInEx package: https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/
- [R5] Jötunn network compatibility: https://valheim-modding.github.io/Jotunn/tutorials/networkcompatibility.html
- [R6] Jötunn development setup and local references: https://valheim-modding.github.io/Jotunn/guides/guide.html
- [R7] Thunderstore global rules: https://wiki.thunderstore.io/moderation/global-rules
- [R8] Jötunn Unity asset guide: https://valheim-modding.github.io/Jotunn/tutorials/asset-creation.html
- [R9] Jötunn persistence/removal warning: https://valheim-modding.github.io/Jotunn/tutorials/overview.html
- [R10] Jötunn mock references: https://valheim-modding.github.io/Jotunn/tutorials/asset-mocking.html
- [R11] Blender FBX export manual: https://docs.blender.org/manual/en/5.0/addons/import_export/scene_fbx.html
- [R12] Unity AssetBundle limitations/platforms: https://docs.unity3d.com/6000.0/Documentation/Manual/AssetBundles-Preparing.html
- [R13] Thunderstore local package testing: https://wiki.thunderstore.io/mods/mod-not-visible
- [R14] Thunderstore package requirements: https://wiki.thunderstore.io/mods/creating-a-package
- [R15] Thunderstore BepInEx folder routing: https://wiki.thunderstore.io/mods/packaging-your-mods
- [R16] Thunderstore package updates: https://wiki.thunderstore.io/mods/updating-a-package
- Reference template: https://github.com/Valheim-Modding/JotunnModStub
