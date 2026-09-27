#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
dotnet="${DOTNET:-${HOME}/.dotnet/dotnet}"
cd "$root"
"$dotnet" test VikingFactory.sln --configuration Release
