# FXX-CLOSEOUT - CONS-B Common Participant Executor + Synthetic Smoke

Status: Closed / docs-only summary
Cut: CONS-B
Date: 2026-06-30

## 1. Summary

CONS-B added the internal generic Participant execution mechanics in `Runtime/Common/Participants` and a synthetic smoke runner in `Runtime/Diagnostics`.

No real domain was migrated. The cut only prepared the common executor shape for later participant-domain pilot migrations.

## 2. Files created

### Common participant primitives

- `Packages/com.immersive.framework/Runtime/Common/Participants/ParticipantExecutionEntry.cs`
- `Packages/com.immersive.framework/Runtime/Common/Participants/ParticipantExecutionIssue.cs`
- `Packages/com.immersive.framework/Runtime/Common/Participants/ParticipantExecutionIssueSeverity.cs`
- `Packages/com.immersive.framework/Runtime/Common/Participants/ParticipantExecutionResult.cs`
- `Packages/com.immersive.framework/Runtime/Common/Participants/ParticipantExecutor.cs`

### Synthetic smoke

- `Packages/com.immersive.framework/Runtime/Diagnostics/ParticipantExecutorSyntheticSmokeRunner.cs`

## 3. Executor shape

The executor is internal and generic.

It accepts:

- ordered participant entries
- a participant invoke delegate
- a result validity delegate
- a result success delegate
- a result blocking delegate
- an issue-count delegate
- an exception-to-issue delegate
- an invalid-result-to-issue delegate

The executor:

- validates entry shape
- rejects null entry lists
- executes participants in order
- captures exceptions
- classifies blocking vs optional failures
- detects invalid returned results through the validation delegate
- aggregates participant execution summary and issues

## 4. Smoke coverage

The synthetic smoke covers:

- success with 2 participants
- non-blocking failure in an optional participant
- blocking failure in a required participant
- exception thrown by a required participant
- exception thrown by an optional participant
- invalid result returned by a participant
- empty participant list
- null participant rejected at entry construction
- null entry list rejected by the executor
- invalid entry rejected by the executor

## 5. Mechanical contract confirmed

The new types are:

- internal
- mechanical only
- additive
- free of domain semantics
- non-public

Confirmed exclusions:

- No CycleReset domain was migrated.
- No ObjectReset domain was migrated.
- No Snapshot, ActivityContentExecution or LocalContribution migration was introduced.
- No public API was added or changed.
- No public enum was replaced.
- No public result/status shell was replaced.
- No asmdef changed.

## 6. Smokes pending

No Unity compile, import or smoke was run in this cut.

Pending validation should cover:

- `ParticipantExecutorSyntheticSmokeRunner`
- `CycleResetQaSmokeRunner` for later CONS-C work
- `ObjectResetQaSmokeRunner` for later CONS-D work

If the synthetic smoke is later wired into an existing QA surface, that wiring should stay additive and must not alter existing smoke output.

## 7. Risks remaining

- The executor is intentionally generic and should not absorb domain semantics in later cuts.
- `ParticipantExecutionIssueSeverity` is internal only and must not become a public cross-domain severity model.
- The synthetic smoke is present, but Unity validation is still pending.
- The participant model will need proof through real domain pilots in CONS-C and CONS-D before widening scope.

## 8. Next cuts suggested

1. `CONS-C` - CycleReset Pilot Migration.
2. `CONS-D` - ObjectReset Pilot Migration.

## 9. Manual validation needed

- Confirm the new `Common/Participants` types remain internal only.
- Confirm the synthetic smoke returns success on the covered synthetic cases.
- Confirm the executor rejects invalid entry lists and invalid entries as intended.
- Confirm no existing smoke output changed.
- Confirm no CycleReset/ObjectReset code changed behavior in this cut.

## 10. Files altered in this cut

- `Packages/com.immersive.framework/Runtime/Common/Participants/ParticipantExecutionEntry.cs`
- `Packages/com.immersive.framework/Runtime/Common/Participants/ParticipantExecutionIssue.cs`
- `Packages/com.immersive.framework/Runtime/Common/Participants/ParticipantExecutionIssueSeverity.cs`
- `Packages/com.immersive.framework/Runtime/Common/Participants/ParticipantExecutionResult.cs`
- `Packages/com.immersive.framework/Runtime/Common/Participants/ParticipantExecutor.cs`
- `Packages/com.immersive.framework/Runtime/Diagnostics/ParticipantExecutorSyntheticSmokeRunner.cs`
- `Assets/_Documentation/Closeouts/FXX-CLOSEOUT-CONS-B-Common-Participant-Executor.md`
