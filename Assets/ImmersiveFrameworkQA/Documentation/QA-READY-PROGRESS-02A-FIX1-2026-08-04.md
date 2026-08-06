# QA-READY-PROGRESS-02A-FIX1

## Purpose

Correct the participant-aware readiness terminal regression so that the Required failure is triggered only after the Loading surface has recorded real determinate progress below 100%.

## Why the correction is required

The original regression waited for participants to enter `Preparing` and immediately failed one Required participant. Participant preparation could become observable before the asynchronous Loading update had been applied by the surface adapter. The terminal failure then stopped the operation-scoped progress envelope, leaving no determinate update for the regression to inspect.

Cleanup still executed correctly, but strict `QaCaseRegistry.Complete` calls attempted to register cleanup cases while earlier execution cases were missing. That secondary case-order exception hid the primary failure.

## Corrected causal sequence

```text
Target operation preview
  -> FadeWithLoading confirmed
  -> real scene side-effect confirmed
  -> Loading requirement confirmed
  -> Loading evidence probe attached
  -> Activity request starts
  -> all participants Preparing
  -> first determinate Loading update below 100% recorded
  -> one Required participant Failed
  -> typed committed-not-ready terminal result
  -> no 100%
  -> no Hide
  -> Loading and Transition retained
  -> recovery gate retained
  -> explicit cleanup
  -> initial authority restored
```

## Failure reporting

Cleanup cases use `TryCompleteIfNext`. Cleanup continues to restore the QA session, but it cannot replace an earlier execution failure with a misleading case-order exception.

The regression emits a dedicated `stage='DirectRequiredFailure'` error containing separate fields for execution, unwind, participant cleanup, fixture cleanup, presentation cleanup, gate cleanup and authority cleanup.

## Execution

Prepare in Edit Mode:

```text
Immersive Framework/QA/Setup/Activity Entry Readiness/Prepare Direct Readiness Policies Regression
```

Enter a fresh Play Mode and run:

```text
Immersive Framework/QA/Regressions/Game Flow/Run Participant-Aware Readiness Loading Terminal Regression
```

Expected final result:

```text
[QA_READY_PROGRESS_02A] status='Passed' cases='34'
```

The framework error for `FailedCommittedTargetNotReady` is expected because this is a negative terminal regression.
