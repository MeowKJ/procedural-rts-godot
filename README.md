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
identity. Do not edit version values in isolation.

Run the same strict package path with its disposable smoke output:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\ExportSmoke.ps1
```

After merged-main verification and physical Windows acceptance evidence, use
`tools/PublishWindowsPrerelease.ps1` with `-Publish`; it refuses to publish
without the exact release commit, checked assets, and supplied acceptance evidence.

## Current Prototype

- Programmatic 2D grid
- Procedural vector-like unit drawing
- Player and enemy units
- Camera movement and zoom
- Drag selection
- Right-click move commands
- HUD shell
