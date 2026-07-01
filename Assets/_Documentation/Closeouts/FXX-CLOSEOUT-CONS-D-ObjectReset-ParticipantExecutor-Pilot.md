# FXX-CLOSEOUT - CONS-D ObjectReset ParticipantExecutor Pilot

Status: Closed / docs-only summary  
Cut: CONS-D  
Date: 2026-06-30

## 1. Summary

CONS-D migrated the internal participant execution path of `ObjectResetRuntime` to the common `ParticipantExecutor` created in CONS-B.

The public ObjectReset surface was preserved:

- `IObjectResetParticipant` unchanged
- `ObjectResetParticipantEntry` unchanged
- `ObjectResetParticipantResult` unchanged
- `ObjectResetResult` unchanged
- `ObjectResetIssue` unchanged
- `ObjectResetParticipantRequiredness` unchanged
- `ObjectResetParticipantResultStatus` unchanged

No Snapshot, ActivityContentExecution or LocalContribution migration was introduced.

## 2. Files altered in this cut

- `Packages/com.immersive.framework/Runtime/ObjectReset/ObjectResetRuntime.cs`
- `Packages/com.immersive.framework/Runtime/ObjectReset/ObjectResetQaSmokeRunner.cs`
- `Assets/_Documentation/Closeouts/FXX-CLOSEOUT-CONS-D-ObjectReset-ParticipantExecutor-Pilot.md`

## 3. What was migrated

`ObjectResetRuntime.Execute(...)` now uses the internal common participant executor for the executable participant path.

ObjectReset keeps the domain rules locally and feeds them into Common through delegates:

- participant invocation
- result validation
- result success/failure classification
- blocking classification
- issue count selection
- exception conversion
- invalid-result conversion

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

## 5. ObjectReset -> Common Participant mapping

The cut uses an explicit internal mapping:

- `ObjectResetParticipantRequiredness.Required` -> `ParticipantRequiredness.Required`
- `ObjectResetParticipantRequiredness.Optional` -> `ParticipantRequiredness.Optional`

`Unknown` or invalid requiredness remains rejected as before.

## 6. Rules preserved

The following behavior was preserved:

- invalid plan continues to be rejected
- skipped plan or empty plan continues to return success with no participants
- invalid entry continues to generate an equivalent issue
- unsupported scope continues to generate an equivalent issue
- participant exception continues to produce a failure result plus an equivalent issue
- required failure continues to block reset
- optional failure continues to remain non-blocking
- source/reason/message normalization remains equivalent

## 7. Explicit bug fix

ObjectReset did not have the same explicit returned-result validation shape as the CycleReset pilot.

This cut adds explicit validation for returned participant results:

- defined status, not `Unknown`
- request identity
- resolved target identity
- participant id
- participant target
- requiredness
- `IsValid`

If a participant returns an invalid result, the runtime now converts it into a failure result and an issue instead of letting the shell aggregate silently continue.

This is an accepted bug fix, not a pure refactor.

## 8. Known differences

No existing public diagnostics were intentionally renamed.

The invalid-result path is now visible through the runtime executor smoke and may surface a failure where the previous implementation would have accepted or misclassified the result.

## 9. Smokes affected

Manual validation should cover:

- `ObjectResetQaSmokeRunner` runtime executor smoke
- `ObjectResetQaSmokeRunner` runtime host integration smoke
- `ObjectResetQaSmokeRunner` trigger smoke
- `ObjectResetQaSmokeRunner` bridge smoke
- `ObjectResetQaSmokeRunner` foundation closure smoke
- `ObjectResetQaSmokeRunner` Unity participant source smoke
- `ObjectResetQaSmokeRunner` transform participant smoke
- `ObjectResetQaSmokeRunner` required guardrails smoke
- `ObjectResetQaSmokeRunner` Unity adapters closure smoke
- `ObjectResetQaSmokeRunner` GameObject active closure smoke
- `ParticipantExecutorSyntheticSmokeRunner`
- `CycleResetQaSmokeRunner` regression check so the first pilot is still intact

## 10. Validation still required

- Unity compile/import
- `ObjectResetQaSmokeRunner`
- `ParticipantExecutorSyntheticSmokeRunner`
- `CycleResetQaSmokeRunner`

## 11. Risks remaining

- The common executor is intentionally generic and must not absorb ObjectReset semantics.
- Returned-result validation must stay inside the owning module if later pilots need further adjustments.
- The current cut is code-only and still needs Unity validation.

## 12. Next cut suggested

`CONS-F` - Participant Consolidation Closeout / Decision.
