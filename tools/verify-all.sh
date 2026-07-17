#!/usr/bin/env sh
set -eu

cd "$(dirname "$0")/.."
# Suite registration, including typed Map Authoring and plugin lifecycle QA, lives in VerifyAll/Program.cs.
dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore "$@"
