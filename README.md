# Procedural RTS Godot

Desktop RTS prototype built with Godot 4.7 Mono and C#.

## Codex / Agent Entry

Codex agents should start with `AGENTS.md`. Use GitHub Issues/Project for active
work; this repository intentionally has no local TODO backlog.

## Run

Open this folder in Godot 4.7 Mono:

```powershell
& "$env:LOCALAPPDATA\Microsoft\WinGet\Packages\GodotEngine.GodotEngine.Mono_Microsoft.Winget.Source_8wekyb3d8bbwe\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64.exe" --path .
```

Build C# from the command line:

```powershell
dotnet build .\ProceduralRts.csproj
```

Verify with Godot headless:

```powershell
& "$env:LOCALAPPDATA\Microsoft\WinGet\Packages\GodotEngine.GodotEngine.Mono_Microsoft.Winget.Source_8wekyb3d8bbwe\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe" --headless --path . --scene res://scenes/Battle.tscn --quit-after 3
```

## Headless Linux Development

The C# simulation and validation tools are cross-platform. On a headless Linux
worker, install .NET 8 and Godot 4.7 Mono, then point `GODOT_BIN` at the Godot
executable if it is not already on `PATH`.

```sh
export GODOT_BIN=/opt/godot/Godot_v4.7-stable_mono_linux.x86_64
dotnet build ProceduralRts.csproj --no-restore
sh tools/verify-all.sh
```

`tools/VerifyAll` also searches `godot`, `godot4`, `godot-mono`,
`godot4-mono`, `Godot_v4.7-stable_mono_linux.x86_64`, and the Windows Godot
Mono executable names on `PATH`.

## Export

The project includes a Windows export preset named `Windows Desktop`.

```powershell
& "$env:LOCALAPPDATA\Microsoft\WinGet\Packages\GodotEngine.GodotEngine.Mono_Microsoft.Winget.Source_8wekyb3d8bbwe\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe" --headless --path . --export-release "Windows Desktop" .\builds\windows\ProceduralRTS.exe
```

Godot export templates must be installed locally before release export can produce the `.exe`.
The strict release command fails when templates are missing, clears its whole output
directory, packages the complete export, and verifies a clean extracted runtime:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\PackageWindowsRelease.ps1
```

`assets/release/release-identity.json` is the canonical source for the menu
version, Windows version mapping, release tag, BuildInfo, and authored sample
identity. Do not edit version values in isolation. The canonical package name is
`ProceduralRTS-<tag>-<target>.zip` (currently
`ProceduralRTS-v0.2.0-rc.1-windows-x86_64.zip`).

Run the same strict package path with its disposable smoke output:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\ExportSmoke.ps1
```

After merged-main verification and physical Windows acceptance evidence, use
`tools/PublishWindowsPrerelease.ps1` with `-Publish`; it refuses to publish
without the exact release commit, checked assets, and supplied acceptance evidence.
The evidence is a `procedural-rts.windows-acceptance` schema-v2 JSON record
bound to the generated package SHA-256, authored sample id/hash, exact commit,
UTC acceptance time, redacted Windows host metadata, and explicit successful
interactive checks for extract, desktop launch, preview entry, authored-map
load, unit selection, move command, completed building, produced unit, resource
gathering, combat engagement, victory outcome, pause/resume, match restart,
return to normal skirmish, main-menu return, and clean application exit. Validate
it before publishing with:

`host.machineClass` must be `physical Windows PC`, `host.os` must identify a
Windows desktop release, and `host.architecture` must be `x86_64`. The
`interactive` object must set every one of these keys to `true`:
`packageExtracted`, `desktopLaunch`, `authoredMapPreviewEntry`,
`authoredMapLoaded`, `unitSelection`, `moveCommand`, `buildingConstructed`,
`unitProduced`, `resourceGathering`, `combatEngagement`, `victoryOutcome`,
`pauseAndResume`, `matchRestart`, `normalSkirmishReturn`, `mainMenuReturn`, and
`cleanApplicationExit`. The required `attestation` object declares
`human-physical-windows-interactive`, a redacted human operator, and a
permalink to a #570 issue comment containing the recorded screenshots, video,
or logs. The offline validator checks the package binding and attestation
structure. During `-Publish`, GitHub must also resolve that exact comment and
its body must record the exact release commit and package SHA-256. It also
requires that commit to be reachable from `main` and to have its latest
exact-SHA `VerifyAll` push run on `main` succeed; branch CI or a manually
dispatched VerifyAll cannot substitute for that gate. The remote tag is peeled
and checked against that exact SHA, then `--verify-tag` prevents automatic tag
creation. The maintainer still inspects the linked human evidence before
releasing.

```powershell
dotnet run --project .\tools\WindowsAcceptanceEvidence\WindowsAcceptanceEvidence.csproj -- `
  --evidence .\windows-acceptance.json `
  --identity .\assets\release\release-identity.json `
  --release-root .\builds\release `
  --commit <verified-merged-main-sha>
```

## Current Prototype

- Programmatic 2D grid
- Procedural vector-like unit drawing
- Player and enemy units
- Camera movement and zoom
- Drag selection
- Right-click move commands
- HUD shell
