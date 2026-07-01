# GitHub Issue Workflow

Active open work is tracked in GitHub Issues for the private repository:

`https://github.com/MeowKJ/procedural-rts-godot/issues`

## Agent Rules

1. Pick an open issue before starting a bounded slice.
2. Comment on that issue with the planned scope, files likely touched, and gates
   expected for the slice.
3. Implement the slice in the repository.
4. Comment with verification evidence: build, ReviewGate mode, replay/QA tool,
   PerfSmoke, or Godot headless result as appropriate.
5. Close the issue only when the corresponding TODO block is complete, review
   evidence exists where required, and relevant gates pass.

## TODO.md Role

`TODO.md` remains the planning source/snapshot during the transition. New
execution status should move through GitHub issue comments so remote agents can
report progress without relying on local desktop notifications.

## Comment Format

Use short comments with this shape:

```text
Slice:
- What changed

Files:
- path/to/file.cs

Verification:
- command: pass/fail

Next:
- Remaining follow-up, if any
```
