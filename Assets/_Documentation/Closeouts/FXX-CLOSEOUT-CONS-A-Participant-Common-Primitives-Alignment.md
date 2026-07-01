# FXX-CLOSEOUT - CONS-A Participant Common Primitives Alignment

Status: Closed / docs-only summary
Cut: CONS-A
Date: 2026-06-30

## 1. Summary

CONS-A prepared the minimal internal primitives needed for the participant consolidation track without migrating any real domain yet.

The cut stayed inside `Runtime/Common` and added only internal participant primitives that can support `CONS-B`.

## 2. Files created

### Common primitives

- `Packages/com.immersive.framework/Runtime/Common/Participants/ParticipantRequiredness.cs`
- `Packages/com.immersive.framework/Runtime/Common/Participants/ParticipantValidation.cs`

## 3. What is internal

The following are internal only:

- `ParticipantRequiredness`
- `ParticipantValidation`

`ParticipantValidation` reuses `FrameworkEnumValidation` for fail-fast enum checks and does not introduce new public validation surface.

## 4. Public contracts preserved

- No public enum was replaced.
- No public API was added or changed.
- No result/status shell was replaced.
- No serialized field changed.
- No asmdef changed.

## 5. CycleReset / ObjectReset not migrated

This cut did not migrate:

- `CycleReset`
- `ObjectReset`
- `Snapshot`
- `ActivityContentExecution`
- `LocalContribution`

No participant domain was rewritten to consume the new Common primitives yet.

## 6. Dependency for CONS-B

CONS-B is the next step and can use these internal primitives as the starting point for the participant executor shape.

This cut intentionally stops before:

- `ParticipantExecutor`
- participant descriptor wrappers
- participant entry wrappers
- participant issue shells
- any domain migration

## 7. Smokes pending

No Unity smoke was run in this cut.

Pending validation should cover the future participant consolidation work, at minimum:

- `CommonParticipantExecutorSmokeRunner`
- `CycleResetQaSmokeRunner`
- `ObjectResetQaSmokeRunner`

Additional domain smokes should only be added when CONS-B actually introduces them.

## 8. Risks remaining

- `ParticipantRequiredness` is now available, but the executor shape is not yet proven.
- `ParticipantValidation` is intentionally minimal and may need expansion only after a second concrete participant call site exists.
- The participant consolidation track can drift into over-generic abstractions if CONS-B starts too early without concrete pilot evidence.

## 9. Manual validation needed

- Confirm the new files stay internal and do not expose a public participant abstraction.
- Confirm no CycleReset or ObjectReset source changed behavior in this cut.
- Confirm the next participant cut is still blocked on a concrete CONS-B plan.

## 10. Files altered in this cut

- `Packages/com.immersive.framework/Runtime/Common/Participants/ParticipantRequiredness.cs`
- `Packages/com.immersive.framework/Runtime/Common/Participants/ParticipantValidation.cs`
- `Assets/_Documentation/Closeouts/FXX-CLOSEOUT-CONS-A-Participant-Common-Primitives-Alignment.md`
