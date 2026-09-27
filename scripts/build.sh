#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
dotnet="${DOTNET:-${HOME}/.dotnet/dotnet}"
cd "$root"
"$dotnet" build VikingFactory.sln --configuration Release
