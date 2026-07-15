# P3 Canonical Pre-FIRSTGAME QA

Status: Active
Last updated: 2026-07-15

## Purpose

This is the only operational Player QA entry point before FIRSTGAME migration.
It validates the current P3 lane from Player authoring through Activity lifecycle
handoff without restoring removed Player contracts.

## Run

Use exactly one Unity menu command:

```text
Immersive Framework/QA/Player/P3 Run Canonical Pre-FIRSTGAME Smoke
```

No setup or repair menu is required. The command repairs the canonical fixture,
preserves the previous saved Scene setup in `SessionState`, enters Play Mode,
runs the integrated lane, exits Play Mode and restores the previous Scenes.

## Editor phase

- PlayerComposer idempotency and canonical materialization allowlist;
- Player Slot profile authoring;
- provisioning surface validity, duplicate rejection and manager divergence;
- Activity participation projection;
- ordered Session Slot initialization, joining and capacity;
- join contract authoring;
- synthetic provisioning bridge, explicit technical-host parent, callback,
  rollback, CLR-null/fake-null and single-flight behavior;
- canonical P3G4/P3H4/P3J5/P3J6 fixture repair.

## Play Mode phase

- real local Player join and technical-host admission;
- public default Actor selection;
- Actor materialization and preparation;
- Route/Activity restart and owner identity replacement;
- gameplay occupancy, input binding, camera eligibility and admission;
- Activity lifecycle handoff and cleanup.

## Removed contracts

The suite must not depend on `PlayerSlotDeclaration`, `PlayerSlotOccupancy`,
`PlayerEntry`, `PlayerViews`, `PlayerControls`, `PlayerTopology`, F49/F51/F52
PlayerBinding, or `SessionPlayerInputManagerDeclaration`.

## Result

Success emits one aggregate record:

```text
[P3_CANONICAL_PREFIRSTGAME_SMOKE] status='Passed' phases='2' cases='<total>' completed='<case ids>'
```

Failure emits the run id, phase, current case, exception, message, completed cases,
Scene and Play Mode state. A stale interrupted run is rejected and cleaned before
a new run proceeds.

## Manual validation

1. Import and compile Framework and QAFramework.
2. Confirm the Player QA menu contains only the canonical command.
3. Run the command from Edit Mode with all open Scenes saved.
4. Confirm automatic Play Mode entry/exit and previous Scene restoration.
5. Confirm the single aggregate log. Do not declare PASS without this evidence.
