# VikingFactory package template

This directory is the source for the test ZIP, not an upload. `scripts/package.sh` stages it with the built DLLs, models, and hammer pictures under `artifacts/`, runs `scripts/validate-package.sh`, and zips it.

- `manifest.json` names BepInExPack Valheim 5.4.2351 and Jötunn 2.30.2.
- `PLAYER-README.md` becomes the package's `README.md`.
- `icon.png` is a flat 256×256 mark. The repository lockup is `branding/vikingfactory-logo.png`.

Install path inside the package: `plugins/VikingFactory/VikingFactory.dll`, `VikingFactory.Core.dll`, and `Assets/`.

The `0.3.0` ZIP loaded 39 of 39 pieces headless. It has not been tested in a world, with two clients, or on a dedicated server. There is no license file. Do not publish.
