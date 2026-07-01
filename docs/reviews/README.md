# AI Review Records

Store one review record per implemented TODO slice in this folder. The record is
the durable handoff evidence between Owner AI, Reviewer AI, and Integrator AI.

This folder is an evidence archive, not the main navigation surface. For current
status, start from root `TODO.md`, the latest ReviewGate run, and only then open
the relevant recent review record.

File name:

```
YYYY-MM-DD-milestone-short-slice.md
```

Template:

```
# Review Record - <slice name>

Step:
Milestone:
Owner AI:
Reviewer AI:
Integrator AI:

Scope:
- Files/folders:
- Non-goals:

Implementation summary:
- 

Automated gates:
- Command:
  Result:
  Evidence:

Manual/visual gates:
- Check:
  Result:
  Evidence:

Reviewer result:
- Status: pass / pass-with-warnings / fail
- Required fixes:
- Residual risks:

TODO update:
- Items marked done:
- Items left open:
- Reason:
```

Rules:
- Every implemented TODO slice needs a review record before the TODO is marked
  done or progress is claimed as verified.
- Use `dotnet run --project tools/ReviewGate/ReviewGate.csproj review
  --require-record=<slice-name>` when a slice needs proof that a specific durable
  review record exists.
- A review record cannot mark a broad TODO done with only a narrow check.
- If `ReviewGate` reports warnings, the record must either fix them or list why
  they remain open TODO work.
- If a slice touches simulation authority, include deterministic replay evidence.
- If a slice touches presentation, include screenshot/manual visual evidence when
  visual correctness matters.
- Keep old records stable. Do not rewrite historical records except for factual
  evidence backfill or broken-link repair.
- New broad architecture rules belong in `docs/`, not in individual review
  records.
- If this folder becomes hard to scan, add an index by milestone or date instead
  of moving old records without a migration note.
