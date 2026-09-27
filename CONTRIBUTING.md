# Contributing

VikingFactory is early development. Code, art, testing, balance notes, documentation, and bug reports are all useful. You need the right to submit what you send. Do not open a pull request with extracted Valheim assets, someone else’s models, credentials, or a patch you cannot license.

There is no contributor license agreement and no separate moderation contact. Use [Issues](https://github.com/loafdaddy/VikingFactory/issues) and [pull requests](https://github.com/loafdaddy/VikingFactory/pulls).

## Set up

Follow [docs/Development.md](docs/Development.md). Core tests do not need the game. The plugin build does.

```bash
./scripts/test.sh
```

A change to workshop behaviour should also say how you would check it in a world, even if you could not run that check.

## Choosing work

Read [ROADMAP.md](ROADMAP.md) and [docs/ImplementationStatus.md](docs/ImplementationStatus.md) before starting. The most useful work now is playing [docs/QA-Checklist.md](docs/QA-Checklist.md) and reporting what a world shows.

Open an issue before a large change. A new machine needs a progression tier, a power cost, and a reason it does not skip a vanilla station. Models without that are welcome as art, and they should not be registered as hammer pieces in the same pull request unless the behaviour exists.

## Bug reports

Use the bug template. Include:

- Plugin version and whether the report is from a world or from the core tests
- Valheim, BepInEx, and Jötunn versions if the game was running
- What you placed, what you expected, and what happened
- Whether items were lost, duplicated, or left behind
- A short log excerpt, not your whole save or your `Environment.props`

## Feature proposals

Use the feature template. Say who the machine is for, which biome or station it should sit behind, and which vanilla rule it must leave alone. Balance numbers in the master prompt are proposals. Do not present an untested rate as a measured Valheim value.

## Pull requests

Keep a request to one change. Do not mix a new machine, a balance retune, and a docs rewrite unless they are the same fix.

- Match the style of the file you edit. Core code is nullable. The plugin project currently is not.
- Run `./scripts/test.sh` when you touch `VikingFactory.Core` or the tests.
- Run `./scripts/build.sh` when you touch the plugin and you have a local game install.
- Update [docs/ImplementationStatus.md](docs/ImplementationStatus.md) when behaviour changes. Do not check a box for a test you did not run.
- Do not add game DLLs, publicized assemblies, or absolute install paths.
- Do not claim multiplayer, save/load, or in-game visuals unless that session actually did those things.

Review is a read of the diff and the status note. There is no release branch yet.

## Art

Original models are welcome. Preferred source is glTF, right-handed, Y up, in metres, with the material names `Oak`, `OakLight`, `Iron`, `Bronze`, `Hide`, and `Stone`. Put static meshes under `Static`. Put moving parts on their own empties. Do not embed Valheim textures, shaders, or meshes.

The game clones vanilla materials at runtime from those names. A texture baked into a GLB will not be shipped. Read [docs/Development.md](docs/Development.md) before adding a generator script. Keep hand edits separate from a script that overwrites its output.

`packaging/icon.png` must stay 256×256 if you replace it. The README lockup lives in [`branding/`](branding/README.md).
