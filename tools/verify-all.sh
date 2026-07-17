#!/usr/bin/env sh
set -eu

cd "$(dirname "$0")/.."
# Suite registration, including PlayableMapHandoffQa, lives in VerifyAll/Program.cs.
dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore "$@"
