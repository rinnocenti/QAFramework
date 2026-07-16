# P3 Canonical Pre-FIRSTGAME QA

Status: Active
Last updated: 2026-07-16

## Purpose

This is the operational Player QA entry point for the canonical local
join/ActorProfile materialization lane before FIRSTGAME integration.

It validates the current P3 path from Player Slot authoring through real local
join, selected Actor materialization, GameplayReady admission and Activity
lifecycle handoff without restoring removed Player contracts.

The alternative Pre-Authored Player Composer model has its own separate
Editor-only smoke and is not part of this canonical result.

## Canonical run

Use:

```text
Immersive Framework/QA/Player/P3 Run Canonical Pre-FIRSTGAME Smoke
```

No setup or repair menu is required. The command repairs the canonical fixture,
preserves the previous saved Scene setup in `SessionState`, enters Play Mode,
runs the integrated lane, exits Play Mode and restores the previous Scenes.

## Canonical Editor phase

- Player Slot profile authoring;
- provisioning surface validity, duplicate rejection and manager divergence;
- Activity participation projection;
- ordered Session Slot initialization, joining and capacity;
- join contract authoring;
- synthetic provisioning bridge, explicit technical-host parent, callback,
  rollback, CLR-null/fake-null and single-flight behavior;
- canonical P3G4/P3H4/P3J5/P3J6 fixture repair.

## Canonical Play Mode phase

- real local Player join and technical-host admission;
- public default Actor selection;
- Actor materialization and preparation;
- Route/Activity restart and owner identity replacement;
- gameplay occupancy, input binding, camera eligibility and admission;
- Activity lifecycle handoff and cleanup.

## Alternative Pre-Authored Player coverage

Run separately:

```text
Immersive Framework/QA/Player Alternatives/P3B Run Pre-Authored Player Composer Smoke
```

This Editor-only smoke validates the optional model where one concrete Player
already exists with its own `PlayerInput` and
`PreAuthoredPlayerComposer`. It proves Apply/Rebuild, generated declaration,
Gate wiring, camera anchors, explicit failures and idempotency.

Its result must not be counted as evidence for the canonical join lane.

## Removed contracts

The canonical suite must not depend on `PlayerSlotDeclaration`,
`PlayerSlotOccupancy`, `PlayerEntry`, `PlayerViews`, `PlayerControls`,
`PlayerTopology`, F49/F51/F52 PlayerBinding, or
`SessionPlayerInputManagerDeclaration`.

## Canonical result

Success emits one aggregate record:

```text
[P3_CANONICAL_PREFIRSTGAME_SMOKE] status='Passed' phases='2' cases='<total>' completed='<case ids>'
```

Failure emits the run id, phase, current case, exception, message, completed
cases, Scene and Play Mode state. A stale interrupted run is rejected and
cleaned before a new run proceeds.

## Manual validation

1. Import and compile Framework and QAFramework.
2. Confirm the canonical and alternative menu commands are separate.
3. Run the canonical command from Edit Mode with all open Scenes saved.
4. Confirm automatic Play Mode entry/exit and previous Scene restoration.
5. Confirm the single canonical aggregate log.
6. Run the alternative Pre-Authored smoke independently.
7. Do not declare PASS for either lane without its own evidence.