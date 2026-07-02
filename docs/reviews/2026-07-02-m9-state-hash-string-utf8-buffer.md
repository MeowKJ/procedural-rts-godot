# Review Record - M9 state hash string UTF8 buffer

Step: Avoid deterministic string hash byte-array allocation
Milestone: M9 Elegance, Decoupling, Performance
Owner AI: Codex
Reviewer AI: ReviewGate regression / SimReplay
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/entities/EntityStateHash.cs`, `tools/ReviewGateDomains/RegressionReviewGate.cs`, `TODO.md`.
- Non-goals: changing FNV constants, hash field coverage, nullable string behavior, component ordering, gameplay behavior, or the remaining `OrderBy` allocations inside `EntityStateHash`.

Implementation summary:
- Replaced `Encoding.UTF8.GetBytes(value)` in `EntityStateHash.Add(string)` with a 4-byte stack buffer and span-based UTF-8 encoding.
- Preserved valid surrogate-pair grouping so non-ASCII strings hash the same byte sequence as the previous whole-string UTF-8 encoding path.
- Added `ReviewGate regression` evidence requiring `stackalloc byte[4]` and forbidding the byte-array `Encoding.UTF8.GetBytes(value)` path.
- Updated TODO evidence under the M9 allocation paydown parent.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: sequential rerun passed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: deterministic replay suite passed; existing hashes stayed stable, including `production-loop` `427000301860631748`, `combat` `3773882959108536546`, and `outcome` `18128435059327466148`.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- regression --max-warnings=0`
  Result: pass
  Evidence: ReviewGate regression completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- filesize --max-warnings=0`
  Result: pass
  Evidence: sequential rerun completed with 0 errors and 0 warnings; `RegressionReviewGate.cs` remains at 200 lines.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m9-state-hash-string-utf8-buffer`
  Result: pass
  Evidence: required review record gate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- all --max-warnings=0`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings.
- Command: `GODOT_BIN=$(command -v godot-dotnet) DOTNET_ROLL_FORWARD=Major sh tools/verify-all.sh`
  Result: pass
  Evidence: full grouped verification completed 23/23.

Manual/visual gates:
- Check: GUI visual QA
  Result: not run
  Evidence: this was a deterministic validation allocation slice; no rendering behavior changed.

Reviewer result:
- Status: pass.
- Required fixes: none.
- Residual risks: `EntityStateHash` still has small `OrderBy` allocation paths for mounts, production queues, ability cooldowns, and command queues; those remain future #10 child slices.

TODO update:
- Items marked done: none.
- Items left open: `Per-tick allocation paydown`.
- Reason: #125 closes only the string UTF-8 allocation in deterministic hashing; #10 remains a broader allocation paydown tracker.
