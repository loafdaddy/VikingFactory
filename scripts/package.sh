#!/usr/bin/env bash
# Stages a Thunderstore-shaped ZIP under artifacts/ from a Release build. Validates it. Does not upload.
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
version="$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$root/Version.props" | head -n 1)"
bin="$root/src/VikingFactory.Plugin/bin/Release"
for dll in VikingFactory.dll VikingFactory.Core.dll; do
  if [[ ! -f "$bin/$dll" ]]; then
    echo "$dll is not built. Run scripts/build.sh first." >&2
    exit 2
  fi
done

stage="$root/artifacts/package/VikingFactory-$version"
rm -rf "$stage"
mkdir -p "$stage/plugins/VikingFactory/Assets/icons"
cp "$root/packaging/manifest.json" "$root/packaging/icon.png" "$stage/"
cp "$root/packaging/PLAYER-README.md" "$stage/README.md"
cp "$root/CHANGELOG.md" "$stage/CHANGELOG.md"
cp "$bin/VikingFactory.dll" "$bin/VikingFactory.Core.dll" "$stage/plugins/VikingFactory/"

pack="$root/VikingFactory-Assets"
models="$(grep -o '"vf_[a-z0-9_]*\.glb"' "$root/src/VikingFactory.Plugin/Machines/MachineCatalog.cs" | tr -d '"' | sort -u)"
for file in $models; do
  id="${file%.glb}"
  for candidate in "$pack/polished/$id/$file" "$pack/expansion/models/$id/$file"; do
    if [[ -f "$candidate" ]]; then
      cp "$candidate" "$stage/plugins/VikingFactory/Assets/$file"
      break
    fi
  done
  [[ -f "$stage/plugins/VikingFactory/Assets/$file" ]] || { echo "Missing model $file" >&2; exit 2; }
done
cp "$pack/polished/icons/"*.png "$stage/plugins/VikingFactory/Assets/icons/"

if ! grep -q "\"version_number\": \"$version\"" "$stage/manifest.json"; then
  echo "manifest.json does not say $version" >&2
  exit 2
fi

"$root/scripts/validate-package.sh" "$stage"
zipfile="$root/artifacts/VikingFactory-$version.zip"
rm -f "$zipfile"
(cd "$stage" && zip -qr "$zipfile" .)
echo "Staged $zipfile ($(du -h "$zipfile" | cut -f1)). Not uploaded."
