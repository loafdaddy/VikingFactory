# VikingFactory package template

This directory is not a Thunderstore upload and not a player readme. The project readme is the repository `README.md`.

`scripts/validate-package.sh` checks that this folder has `manifest.json`, this file, and a 256×256 `icon.png`, and that it does not contain game DLLs. The manifest website is the GitHub repository. Dependencies are still empty.

`icon.png` is a flat mark. The lockup used on the repository front page is `branding/vikingfactory-logo.png`.

Planned install path, once a tested build exists:

`BepInEx/plugins/VikingFactory/VikingFactory.dll`

`BepInEx/plugins/VikingFactory/VikingFactory.Core.dll`

Server and every client will need the same build. Remote factories are not meant to keep running when nobody is nearby. That rule has not been tested.
