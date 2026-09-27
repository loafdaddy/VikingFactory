#!/usr/bin/env bash
# Validates a staged Thunderstore directory. Does not upload.
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
stage="${1:-$root/packaging}"
fail=0

require() {
  if [[ ! -f "$stage/$1" ]]; then
    echo "Missing $1" >&2
    fail=1
  fi
}

require manifest.json
require README.md
require icon.png

if [[ -d "$stage" ]]; then
  while IFS= read -r -d '' file; do
    base="$(basename "$file")"
    case "$base" in
      assembly_valheim.dll|UnityEngine*.dll|Unity*.dll|Environment.props)
        echo "Prohibited file: $file" >&2
        fail=1
        ;;
    esac
    if [[ "$file" == *"/publicized/"* || "$file" == *"/Managed/"* ]]; then
      echo "Prohibited path: $file" >&2
      fail=1
    fi
  done < <(find "$stage" -type f -print0)
fi

if [[ -f "$stage/icon.png" ]]; then
  if command -v identify >/dev/null 2>&1; then
    size="$(identify -format '%wx%h' "$stage/icon.png")"
    if [[ "$size" != "256x256" ]]; then
      echo "icon.png is $size, Thunderstore requires 256x256" >&2
      fail=1
    fi
  else
    echo "ImageMagick is not installed, so icon dimensions were not checked." >&2
  fi
fi

if [[ "$fail" -ne 0 ]]; then
  echo "Package is not ready to upload." >&2
  exit 1
fi

echo "Package staging checks passed for $stage"
