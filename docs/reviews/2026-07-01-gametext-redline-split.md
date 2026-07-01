# Review Record - GameText red-line split

Step: Split localization dictionaries out of GameText
Milestone: Single responsibility - god-class breakup
Owner AI: Codex
Reviewer AI: Build / ReviewGate filesize
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/GameText.cs`, `scripts/core/GameText.English.cs`, `scripts/core/GameText.ChineseSimplified.cs`, `tools/ReviewGate/FileSizeGate.cs`, `TODO.md`.
- Non-goals: rewriting translations, changing language selection behavior, or changing UI copy.

Implementation summary:
- Converted `GameText` to a static partial class.
- Moved the English dictionary into `GameText.English.cs`.
- Moved the Simplified Chinese dictionary into `GameText.ChineseSimplified.cs`.
- Kept `CurrentLanguage`, `T`, `Format`, `HasTranslation`, `Keys`, and `TableFor` in the small `GameText.cs` API shell.
- Removed `scripts/core/GameText.cs` from the known red-line file-size debt whitelist.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: local run completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- filesize`
  Result: pass
  Evidence: red-line debt warning dropped from 11 to 10 files over 600 lines after the split.

Reviewer result:
- Status: pass
- Required fixes: none.
- Residual risks: existing translation text quality is unchanged; this slice only fixes file responsibility and size.

TODO update:
- Items marked done: `GameText red-line split`.
- Items left open: other red-line files.
- Reason: localization dictionaries are now separated by language and the public lookup API is small.
