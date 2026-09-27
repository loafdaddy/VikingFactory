#!/usr/bin/env bash
# Copies only a built VikingFactory plugin into an explicit plugins directory.
# Refuses empty paths, the game Managed folder, and any recursive delete.
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
dest="${VF_DEPLOY_PLUGINS:-}"

if [[ -z "$dest" && -f "$root/Environment.props" ]]; then
  dest="$(sed -n 's:.*<VF_DEPLOY_PLUGINS>\(.*\)</VF_DEPLOY_PLUGINS>.*:\1:p' "$root/Environment.props" | head -n 1 | tr -d '[:space:]')"
fi

if [[ -z "$dest" ]]; then
  echo "VF_DEPLOY_PLUGINS is empty. Set it to the isolated profile's BepInEx/plugins/VikingFactory directory." >&2
  exit 2
fi

case "$dest" in
  *valheim_Data/Managed*|*Valheim_Data/Managed*|*valheim_Data/Managed/*|*Valheim_Data/Managed/*)
    echo "Refusing to deploy into the game Managed directory: $dest" >&2
    exit 2
    ;;
esac

if [[ "$(basename "$dest")" == "Managed" ]]; then
  echo "Refusing to deploy into a Managed directory: $dest" >&2
  exit 2
fi

dll="$root/src/VikingFactory.Plugin/bin/Release/VikingFactory.dll"
if [[ ! -f "$dll" ]]; then
  echo "No plugin DLL has been built. Deploy skipped. Core tests do not require this copy." >&2
  exit 2
fi

mkdir -p "$dest"
cp "$dll" "$dest/VikingFactory.dll"
core="$root/src/VikingFactory.Plugin/bin/Release/VikingFactory.Core.dll"
if [[ ! -f "$core" ]]; then
  echo "VikingFactory.Core.dll is missing beside the plugin. Deploy stopped." >&2
  exit 2
fi
cp "$core" "$dest/VikingFactory.Core.dll"
mkdir -p "$dest/Assets"
pack="$root/VikingFactory-Assets"
copy_model() {
  local id="$1"
  local polished="$pack/polished/$id/$id.glb"
  local proto="$pack/models/$id/$id.glb"
  if [[ -f "$polished" ]]; then
    cp "$polished" "$dest/Assets/$id.glb"
    echo "Copied polished $id.glb"
  elif [[ -f "$proto" ]]; then
    cp "$proto" "$dest/Assets/$id.glb"
    echo "Polished $id.glb is missing. Copied the prototype."
  else
    echo "No GLB for $id" >&2
    exit 2
  fi
}
copy_model vf_shaft_2m
copy_model vf_water_wheel
copy_model vf_hand_crank
copy_model vf_clutch
copy_model vf_conveyor_2m
copy_model vf_feeder
copy_model vf_catch_basket
mkdir -p "$dest/Assets/icons"
icon_dir="$pack/polished/icons"
if [[ ! -d "$icon_dir" ]]; then
  echo "Piece icons are missing under polished/icons." >&2
  exit 2
fi
cp "$icon_dir"/*.png "$dest/Assets/icons/"
echo "Copied the plugin DLLs, workshop GLBs, and piece icons to $dest"
