# IF-FW-F25R - Activity Scene Operation Architecture Reset

## Status
Accepted / Documentation reset

## Context

F25C through F25D4 added Activity scene execution, release and local guards after the F25A/F25B contract baseline.

Those cuts exposed a structural issue:

```text
ActivityVisualTransitionMode.Seamless
+
Activity scene load/release side-effect
=
LoadingSurface appears without fade
```

This is invalid because LoadingSurface can only appear inside a valid visual envelope when Activity scene side-effects are visible to the player.

## Decision

F25C-D4 are now treated as experimental/partial execution evidence, not as the final Activity scene operation architecture.

The canonical reset is recorded in:

```text
Packages/com.immersive.framework/Documentation~/ADRs/F25R-ADR-ACTIVITY-001-Activity-Scene-Operation-Architecture-Reset.md
```

The next implementation work must introduce `ActivityOperationPlan` as the owner of:

- Activity visual policy;
- Activity scene composition;
- Activity scene release;
- LoadingSurface requirement;
- TransitionSurface visual envelope requirement;
- Activity scene side-effect validity;
- Route startup Activity operation;
- Route exit cleanup operation;
- Activity scene ledger ownership.

## Preserved Rules

- `AlreadyLoaded` is diagnostics only, not a side-effect.
- Route change force-releases Activity-owned content regardless of Activity release policy.
- Activity release policy applies only to Activity change/clear.
- Tracked Activity scene evidence must be verified against actual Unity scene load state.

## Replaced Direction

Future cuts must replace host-side loading probes and scattered execution decisions with one Activity operation plan/executor path.

No runtime code was changed in this reset.
