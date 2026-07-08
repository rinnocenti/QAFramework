# F49K Player Binding Readiness QA

## Objective

Validate that the framework can aggregate PlayerTopology, PlayerViewTopology and PlayerControlTopology into a passive binding readiness summary.

## Expected smoke

`[F49K_PLAYER_BINDING_READINESS_QA] status='Succeeded'`

## Scope

- Valid full readiness.
- Missing topology evidence.
- View topology issue blocking view readiness.
- Control topology issue blocking control readiness.
- Topology reference mismatch.
- Empty participants are non-blocking but not ready.
- PlayerTopology issue propagation.

## Out of scope

- Real camera activation.
- Real input activation.
- Movement.
- Runtime binding lifecycle.
