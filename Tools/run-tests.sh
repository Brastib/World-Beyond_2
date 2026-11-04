#!/usr/bin/env bash
# Unity-freie Tests (Simulation, Persistenz, Content). Benötigt .NET 8 SDK.
set -euo pipefail
cd "$(dirname "$0")/../DotNet"
dotnet test WorldBeyond.Tests --logger "console;verbosity=normal" "$@"
