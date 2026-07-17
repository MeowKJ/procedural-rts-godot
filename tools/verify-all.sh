#!/usr/bin/env sh
set -eu

cd "$(dirname "$0")/.."
# Suite registration, including MapSpec artifact and Godot API bake QA, lives in VerifyAll/Program.cs.
dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore "$@"
