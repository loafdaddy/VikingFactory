#!/usr/bin/env bash
# This machine's Valheim install is the game client. The dedicated-server app is a separate Steam install
# and valheim_server.x86_64 is not here. The script is ready for that binary when it is installed.
# It does not use the stock server script, which would write a world into the normal save folder.
set -euo pipefail
game="${VALHEIM_INSTALL:-$HOME/.local/share/Steam/steamapps/common/Valheim}"
save="$HOME/.local/share/VikingFactory/valheim-dev"
mkdir -p "$save" "$HOME/.local/share/VikingFactory/valheim-dev-client2"
if [[ ! -x "$game/valheim_server.x86_64" ]]; then
  echo "valheim_server.x86_64 was not found under $game" >&2
  exit 2
fi
if [[ -z "${VF_DEV_SERVER_PASSWORD:-}" ]]; then
  echo "Set VF_DEV_SERVER_PASSWORD. It is not stored in the repository." >&2
  exit 2
fi
cd "$game"
export DOORSTOP_ENABLED=1
export DOORSTOP_TARGET_ASSEMBLY="$game/BepInEx/core/BepInEx.Preloader.dll"
export LD_LIBRARY_PATH="$game/doorstop_libs:$game/linux64${LD_LIBRARY_PATH:+:$LD_LIBRARY_PATH}"
export LD_PRELOAD="libdoorstop_x64.so${LD_PRELOAD:+:$LD_PRELOAD}"
export SteamAppId=892970
exec ./valheim_server.x86_64 -savedir "$save" -name "Workshop" -port 2458 -world "VikingFactoryDev" -password "$VF_DEV_SERVER_PASSWORD"
