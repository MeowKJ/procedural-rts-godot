$ErrorActionPreference = "Stop"

$project = Resolve-Path (Join-Path $PSScriptRoot "..")
$godot = Join-Path $env:LOCALAPPDATA "Microsoft\WinGet\Packages\GodotEngine.GodotEngine.Mono_Microsoft.Winget.Source_8wekyb3d8bbwe\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe"

if (-not (Test-Path $godot)) {
    throw "Godot console executable was not found: $godot"
}

& $godot --path $project --scene "res://scenes/VisualQaCapture.tscn"
if ($LASTEXITCODE -ne 0) {
    throw "Visual QA capture failed with exit code $LASTEXITCODE"
}

Get-ChildItem -Path (Join-Path $project "artifacts\visual-qa") -Filter "*.png" |
    Sort-Object Name |
    Select-Object Name, Length, LastWriteTime
