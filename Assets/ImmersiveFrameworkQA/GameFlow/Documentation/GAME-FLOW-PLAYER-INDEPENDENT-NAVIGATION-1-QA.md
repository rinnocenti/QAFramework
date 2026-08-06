# Game Flow Player-independent Navigation QA

Manual Unity regression matrix for the player-independent navigation cut. No FIRSTGAME asset is changed.

| Case | Expected authority | Expected readiness / diagnostic |
| --- | --- | --- |
| Route without Activity to Route without Startup Activity | Target Route | No Player handoff required |
| Route without Activity to GameplayReady Startup Activity without Player | Target Route and Startup Activity | `NotReady`, warning, no handoff |
| Active Route without Activity to GameplayReady Activity without Player | Target Activity | `NotReady`, warning, no handoff |
| `NotReady` Activity to GameplayReady Activity | Target Activity | `NotReady` unless its lifecycle satisfies readiness |
| GameplayReady to GameplayReady Activity | Target Activity | P3K.7G transactional handoff preserved |
| GameplayReady Route to GameplayReady Startup Activity | Target Route | P3K.7H transactional handoff preserved |
| Invalid Activity ID | Origin retained | `FailedInvalidConfig`, no public exception |
| Activity request before Game Flow runtime | Origin retained | runtime-not-ready result; Loading `NotExecutedRequestRejected` |
| Failed-before-commit handoff | Origin retained | blocking diagnostic |
| Post-commit finalization issue | Destination retained | explicit finalization issue |

For every committed case, compare `FrameworkRuntimeHost` state with Route Lifecycle `CurrentRoute`, `CurrentActivity`, and Activity readiness. Verify Player-dependent gameplay stays gated while readiness is `NotReady`.
