#!/usr/bin/env bash
# Starts Valheim through BepInEx with a save directory that is not the normal worlds folder.
# A second directory is created for a later second client. This script does not publish.
set -euo pipefail
game="${VALHEIM_INSTALL:-$HOME/.local/share/Steam/steamapps/common/Valheim}"
profile="${1:-primary}"
case "$profile" in
  primary) save="$HOME/.local/share/VikingFactory/valheim-dev" ;;
  client2) save="$HOME/.local/share/VikingFactory/valheim-dev-client2" ;;
  *) echo "Use: scripts/launch-dev.sh [primary|client2]" >&2; exit 2 ;;
esac
mkdir -p "$save" "$HOME/.local/share/VikingFactory/valheim-dev" "$HOME/.local/share/VikingFactory/valheim-dev-client2"
if [[ ! -x "$game/start_game_bepinex.sh" ]]; then
  echo "start_game_bepinex.sh was not found under $game" >&2
  exit 2
fi
cd "$game"
exec ./start_game_bepinex.sh -savedir "$save" "$@"
