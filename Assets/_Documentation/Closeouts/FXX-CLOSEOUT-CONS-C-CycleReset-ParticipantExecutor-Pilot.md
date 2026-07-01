# FXX-CLOSEOUT - CONS-C CycleReset ParticipantExecutor Pilot

Status: Closed / docs-only summary  
Cut: CONS-C  
Date: 2026-06-30

## 1. Summary

CONS-C migrated the internal execution path of `CycleResetRuntime` to the common `ParticipantExecutor` mechanics created in CONS-B.

The public CycleReset surface was preserved:

- `ICycleResetParticipant` unchanged
- `CycleResetParticipantEntry` unchanged
- `CycleResetParticipantResult` unchanged
- `CycleResetResult` unchanged
- `CycleResetIssue` unchanged
- `CycleResetParticipantRequiredness` unchanged
- `CycleResetParticipantResultStatus` unchanged

No ObjectReset, Snapshot, ActivityContentExecution or LocalContribution migration was introduced.

## 2. Files altered in this cut

- `Packages/com.immersive.framework/Runtime/CycleReset/CycleResetRuntime.cs`
- `Assets/_Documentation/Closeouts/FXX-CLOSEOUT-CONS-C-CycleReset-ParticipantExecutor-Pilot.md`

## 3. What was migrated

`CycleResetRuntime.ExecutePlan(...)` now uses the internal common participant executor for the executable participant path.

The CycleReset module keeps the domain semantics locally and feeds them into Common through delegates:

- participant invocation
- result validation
- result success/failure classification
- blocking classification
- issue count selection
- invalid-result conversion
- exception conversion

## 4. What stayed public and unchanged

The following public contracts remain intact:

- participant contract
- participant entry
- participant result shell
- aggregate result shell
- issue shell
- requiredness enum
- participant result status enum

No public enum was replaced and no public API signature was changed.

## 5. CycleReset -> Common Participant mapping

The cut uses an explicit internal mapping:

- `CycleResetParticipantRequiredness.Required` -> `ParticipantRequiredness.Required`
- `CycleResetParticipantRequiredness.Optional` -> `ParticipantRequiredness.Optional`

`Unknown` or invalid requiredness remains rejected as before.

## 6. Rules preserved

The following behavior was preserved:

- invalid plan continues to be rejected
- rejected plan continues to be rejected
- skipped plan or empty plan continues to return success with no participants
- non-planned plan continues to be rejected
- invalid entry continues to generate a blocking issue
- unsupported scope continues to generate a blocking issue
- participant exception continues to produce a failure result plus a blocking issue
- invalid participant result continues to produce a failure result plus an invalid-result issue
- required failure continues to block reset
- optional failure continues to remain non-blocking
- source/reason/message normalization remains equivalent

## 7. Known differences

No intended diagnostic text changes are known at this stage.

Unity compile/import validation is still pending, so any incidental compiler-driven adjustment must be documented here if it appears later.

## 8. Smokes affected

Manual validation should cover:

- `CycleResetQaSmokeRunner`
- `RouteCycleResetTrigger` smoke
- `ActivityCycleResetTrigger` smoke
- `ParticipantExecutorSyntheticSmokeRunner` as a Common sanity check
- Standard Smoke only if the shared route lifecycle path shows collateral impact

## 9. Validation still required

- Unity compile/import
- `CycleResetQaSmokeRunner`
- `RouteCycleResetTrigger` smoke
- `ActivityCycleResetTrigger` smoke
- `ParticipantExecutorSyntheticSmokeRunner`

## 10. Risks remaining

- The common executor is intentionally generic and must not absorb CycleReset semantics.
- Later participant-domain pilots must keep validation and result mapping inside their owning module.
- The current cut is code-only and still needs Unity validation.

## 11. Next cut suggested

`CONS-D` - ObjectReset Pilot Migration.
