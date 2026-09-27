<p align="center">
  <img src="branding/vikingfactory-logo.png" alt="VikingFactory logo: timber and iron gears behind the words Viking Factory" width="640">
</p>

<h1 align="center">VikingFactory</h1>

<p align="center">
  Mechanical workshops for Valheim, built like a Norse settlement invented them.
  <br>
  Timber frames, bronze and iron fittings, stone foundations, and machines that follow the game's own progression.
</p>

<p align="center">
  <img src="https://img.shields.io/badge/status-early%20development-b8860b" alt="Project status: early development">
  <br>
  <a href="ROADMAP.md">Roadmap</a>
  ·
  <a href="CHANGELOG.md">Versions</a>
  ·
  <a href="docs/Development.md">Development setup</a>
  ·
  <a href="docs/ImplementationStatus.md">What is actually built</a>
  ·
  <a href="CONTRIBUTING.md">Contributing</a>
  ·
  <a href="https://github.com/loafdaddy/VikingFactory/issues">Issues</a>
</p>

**Early development.** The public repository is [loafdaddy/VikingFactory](https://github.com/loafdaddy/VikingFactory). It is here so people can follow the work, share ideas, and help build it. It is not a release for players. There is no Thunderstore package. What changed in each number is [CHANGELOG.md](CHANGELOG.md).

VikingFactory is an automation mod for Valheim. The long-term aim is machinery for power, transport, processing, farming, forestry, mining, and cooking, where automation fits the world. Those are design goals. A model or a class name is not a working machine.

## Why this exists

I’m a big fan of Create for Minecraft, Satisfactory, Dyson Sphere Program, Factorio, and Anno, and of Valheim. VikingFactory started because I wanted that same enjoyment of building machines and production chains inside Valheim, without leaving the game’s materials, biomes, or progression behind. Development stays public for anyone who wants to follow along, share an idea, or contribute.

Valheim, and the games named above, are other people’s work. This project is not affiliated with them.

## What the machines are meant to be

Power and items stay separate. A shaft carries rotation. A belt is for goods. If a line asks for more drive than its wheels and cranks can give, it stalls, and you can see that it stalled.

Machines use the materials you already have: wood and hide first, then bronze and iron, then the later metals and stations the game already gates. They should speed up work you can already do. They should not invent new ores, skip a boss, or change how long a smelter or a crop takes.

The full target is written up as a design proposal in [`docs/VikingFactory-Master-Prompt.md`](docs/VikingFactory-Master-Prompt.md). [`DESIGN.md`](DESIGN.md) is an earlier outline. Where those two disagree, the master prompt is the target, and [`docs/ImplementationStatus.md`](docs/ImplementationStatus.md) is what exists today.

## What works today

Update `0.3.0` contains code for every planned milestone. It has been compiled, covered by 66 core tests, and loaded headless. A windowed session then placed fourteen kinds of piece in the isolated world `VIKINGFACTORY` and saved. That shows they can be built. It does not show that they run. The in-world test script is [docs/QA-Checklist.md](docs/QA-Checklist.md).

- The plugin loads under BepInEx and Jötunn and registers 39 hammer pieces on the Crafting tag, each with a model and a rendered picture.
- **Power:** hand crank, water wheel, steam engines, sail wheel, and eitr motor. Shafts, rope, cogs with ratios, reversing cogs, clutches, a governor, and a flywheel. A line stalls when load exceeds drive, or when its ratios or sources disagree. Every segment shows which way it turns.
- **Items:** belts and troughs carry real item queues. Feeders, splitters, mergers, baskets, and docked chests move items without deleting or duplicating them, as far as the tests can show.
- **Native stations:** feeders load and unload kilns, smelters, blast furnaces, windmills, spinning wheels, eitr refineries, fermenters, hives, sap collectors, cooking racks, and ovens through the game's own actions. Timers stay native.
- **Crafting:** a recipe mill runs a recipe you know, next to the real station, from belts. An advanced assembler can also upgrade an item one level.
- **Supply:** bedrock quarry, managed coppice, a timber saw for staked plantations, planters and harvesters, forage beds, a mining head, late deep extraction of older ores, and livestock feed and culling gates.
- A coverage report classifies all 969 items in this game by how they can be supplied and processed.

The hammer only lists pieces the character has discovered. `debugmode` shows every piece and resets every launch.

## Build and test

There is no player install. If you want to compile the current source:

```bash
cp Environment.props.example Environment.props
# Set VALHEIM_INSTALL in Environment.props to your local game. Do not commit that file.
./scripts/test.sh
./scripts/build.sh
```

`scripts/test.sh` runs the core tests. `scripts/build.sh` also compiles the plugin, which needs a local Valheim install, BepInEx, and Jötunn. Paths and versions are in [docs/Development.md](docs/Development.md).

## Known limits

- No in-game screenshots, save/load of a running line, second client, or dedicated server. The world did save on quit after the pieces were placed. Nobody reloaded to see that they were still there.
- Fourteen kinds of piece have been placed. Rates, pivots, colliders, and snap points are still unverified. Shafts and ropes in that session shared one height above sloping ground. Several other pieces, including the water wheel, logged no terrain hit under the pivot.
- The water wheel is meant to stand on the ground and give drive only with its paddles in real water. One was placed. Its feet, the Shift key, and the hover text were not recorded.
- Multiplayer is not implemented as a tested feature. The intention is that the piece owner simulates, and that a factory stops when nobody is nearby. That has not been tried with two clients.
- Unity `6000.0.75f1` matches the current game player. No AssetBundle has been built.
- There is no license file yet. Do not treat the source as free to relicense or ship until one is added.

## Follow along

- Progress and priorities: [ROADMAP.md](ROADMAP.md)
- Engineering checklist: [docs/ImplementationStatus.md](docs/ImplementationStatus.md)
- Bugs and ideas: [GitHub Issues](https://github.com/loafdaddy/VikingFactory/issues), using the templates
- How to help: [CONTRIBUTING.md](CONTRIBUTING.md)

## Docs

| Doc | What it is |
|---|---|
| [CHANGELOG.md](CHANGELOG.md) | Patch notes for each version |
| [ROADMAP.md](ROADMAP.md) | Current focus, next, later, and what “ready” would mean |
| [docs/Development.md](docs/Development.md) | Prerequisites, build, assets, multiplayer notes |
| [docs/ImplementationStatus.md](docs/ImplementationStatus.md) | Checked boxes for work that exists |
| [docs/QA-Checklist.md](docs/QA-Checklist.md) | The in-world steps that close each milestone gate |
| [docs/AutomationCoverage.csv](docs/AutomationCoverage.csv) | Every item in the game and how it can be supplied |
| [docs/Compatibility.md](docs/Compatibility.md) | Game, BepInEx, and Jötunn versions this tree was built against |
| [docs/Balance.md](docs/Balance.md) | Every proposed rate and power number |
| [CONTRIBUTING.md](CONTRIBUTING.md) | Bugs, proposals, and pull requests |

---

VikingFactory is developed with AI assistance, but most of the code is written by me.
