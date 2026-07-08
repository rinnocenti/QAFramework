# F50C — Player Binding Authoring Issue Cleanup QA

## Objective

Validate that `PlayerBindingAuthoringValidationReport` now separates root-cause issues from derived topology/readiness/diagnostic evidence.

## Smoke

1. Apply package delta.
2. Apply QA delta.
3. Run:

```text
Immersive Framework QA > Hub > Create or Refresh Hub and Player QA Scenes
```

4. Open the QA Hub and run:

```text
Player Binding Authoring Issue Cleanup QA
```

Expected log:

```text
[F50C_PLAYER_BINDING_AUTHORING_ISSUE_CLEANUP_QA] status='Succeeded'
```

## Covered cases

```text
valid report has no root or derived issue buckets
missing PlayerSlotDeclaration is a single root cause with derived issues suppressed in default diagnostics
missing PlayerSlotOccupancy is a single root cause with derived issues preserved for detail mode
topology-only failure remains visible as root cause
default summary suppresses derived noise
detailed summary preserves derived issues
passive boundary remains false for view/control/camera/input/movement/spawn
```
