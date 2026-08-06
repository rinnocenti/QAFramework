# QA-READY-PROGRESS-02A-FIX2 — Causal Evidence Snapshot

Date: 2026-08-04  
Repository: `rinnocenti/QAFramework`  
Type: QA technical correction

## Objective

Correct the remaining Q2A smoke failure without weakening the negative-path contract.

The FIX1 proved that a determinate Loading update below 100% was emitted before the Required participant failed. However, the post-terminal assertion reread `PresentationEvidence`, whose retained collection no longer contained that update. The regression then reported:

```text
Direct terminal path did not retain a determinate value below 100%.
```

FIX2 makes the already-established causal event stream the evidence authority for the operation.

## Baseline

Requires:

- `QA-READY-PROGRESS-01`;
- `QA-READY-PROGRESS-01-FIX1`;
- `QA-READY-PROGRESS-01-FIX2`;
- `QA-READY-PROGRESS-02A`;
- `QA-READY-PROGRESS-02A-FIX1`.

Package baseline:

- `com.immersive.framework` commit `99893aa804a9f40cb057449d2b4900a00a2fc3ed` or a compatible later commit containing P1–P3.

## Files to replace

Copy the complete files from this ZIP over the existing project files:

```text
Assets/ImmersiveFrameworkQA/GameFlow/InternalEditor/QaParticipantAwareReadinessLoadingTerminalRegression.cs
Assets/ImmersiveFrameworkQA/GameFlow/InternalEditor/QaParticipantAwareReadinessLoadingTerminalRegression.cs.meta
```

The `.meta` preserves the existing GUID.

## Files to create

```text
Assets/ImmersiveFrameworkQA/Documentation/QA-READY-PROGRESS-02A-FIX2-2026-08-04.md
Assets/ImmersiveFrameworkQA/Documentation/QA-READY-PROGRESS-02A-FIX2-2026-08-04.md.meta
```

## Files removed

None.

## Technical correction

`QaTerminalDeterminateProgressProbe` now captures, during the operation:

- number of determinate Loading `Update` requests;
- last determinate progress value;
- whether any 100% update was observed;
- whether a Loading `Hide` was observed.

The terminal assertions consume this immutable causal snapshot instead of rereading the adapter's retained evidence collection after the request terminates.

The regression still requires:

- a valid `FadeWithLoading` plan;
- real Activity scene side-effects;
- a Loading surface requirement;
- at least one determinate update below 100% before Required failure;
- no 100% update after failure;
- no Loading Hide before explicit recovery;
- Loading and Transition retained;
- recovery gate retained;
- committed destination authority retained;
- full cleanup and restoration.

No timeout, `Task.Delay`, frame polling, log parsing, reflection or global object lookup was added.

## Diagnostic note

The framework may continue logging:

```text
loadingPresentation='SkippedByActivityPolicy'
```

for this intentional terminal failure. The operation does not execute Loading Hide, so request-level diagnostics do not contain a complete Show/Hide pair. This QA cut does not use that summary text as proof and does not change package behavior.

## Unity smoke — exact order

### 1. Prepare the session

Exit Play Mode and run:

```text
Immersive Framework
  > QA
    > Setup
      > Activity Entry Readiness
        > Prepare Direct Readiness Policies Regression
```

### 2. Start a fresh Play Mode

Enter Play Mode and wait for the QA Hub boot to complete.

### 3. Execute Q2A

Run:

```text
Immersive Framework
  > QA
    > Regressions
      > Game Flow
        > Run Participant-Aware Readiness Loading Terminal Regression
```

### 4. Expected evidence

The framework intentionally logs `FailedCommittedTargetNotReady` because one Required participant is failed by the regression. That package error log is expected negative-path evidence.

The final QA result must be:

```text
[QA_READY_PROGRESS_02A] status='Passed' cases='34'
```

### 5. Postconditions

Confirm after PASS:

- Loading hidden;
- Transition hidden;
- no recovery gate remaining;
- `QA Hub Route` authoritative;
- `QA Hub Activity` authoritative.

Exit Play Mode after collecting the result.

## Acceptance criteria

- project compiles with zero C# errors;
- Q2A completes all 34 cases;
- causal probe observes a determinate Loading update below 100%;
- causal probe observes no 100% update after Required failure;
- causal probe observes no Hide before explicit cleanup;
- committed destination and recovery gate remain during failure;
- cleanup restores presentation, gate and initial authority;
- no execution failure is replaced by cleanup case-order errors.

## Out of scope

- Q2B Route/Game Application startup parity;
- package runtime changes;
- package diagnostics correction;
- FIRSTGAME integration;
- changes to shared QA foundation files.

## Commit message

```text
test(qa): retain q2a causal loading evidence
```
