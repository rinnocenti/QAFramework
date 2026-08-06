# QA-READY-PROGRESS-02B — Canonical Startup Loading Parity

Date: 2026-08-04
Type: technical QA regression
Package prerequisite: `c423d4c6c9b46bac5f5eaf106be5050f46120d52`
Applied correction: `QA-READY-PROGRESS-02B-FIX2`

## Objective

Prove participant-aware `WaitCovered` Loading progress through the official host
for both startup paths:

- Route Startup Activity;
- Game Application Startup Activity.

## Runtime authority

The regression uses host-retained typed diagnostics as the primary authority:

```text
LastRouteActivityEntryLoadingDiagnostics
LastStartupActivityEntryLoadingDiagnostics
```

The Loading and Transition adapters provide secondary presentation and ordering
evidence only.

## Participant model

```text
Required: 4
Optional: 1
Optional outcome: FailedNonBlocking
```

The expected terminal path is:

```text
technical progress below 100%
→ Required 0/4
→ Required 1/4
→ Required 2/4
→ Required 3/4
→ Required 4/4 = 100%
→ Loading Hide
→ Transition reveal
→ gate release
```

## Operation-scoped presentation grammar

Startup evidence is selected by exact request `Source`. Within that operation:

```text
Show: RequestReceived → VisibleApplied → ResultRecorded
Update*: RequestReceived → VisibleApplied → ResultRecorded
Hide: RequestReceived → HiddenApplied → ResultRecorded
```

The presentation `Detail` must be non-empty and stable. It is presentation
metadata and is not used as the request reason.

The first Show request records adapter state before application, so its
`ActualVisible` value is diagnostic rather than a fixed grammar requirement.
Applied and result entries still require the correct post-application state.

## Route Startup smoke

1. Exit Play Mode.
2. Run:

```text
Immersive Framework/QA/Setup/Activity Entry Readiness/
Prepare Route Startup Progress Parity
```

3. Enter a fresh Play Mode.
4. Run:

```text
Immersive Framework/QA/Regressions/Game Flow/
Run Participant-Aware Startup Loading Parity Regression
```

Expected:

```text
[QA_READY_PROGRESS_02B_ROUTE] status='Passed' cases='25'
```

## Game Application Startup smoke

Run in a separate session.

1. Exit Play Mode.
2. Run:

```text
Immersive Framework/QA/Setup/Activity Entry Readiness/
Prepare Game Application Startup Progress Parity
```

3. Enter a fresh Play Mode.
4. Run the same regression menu.

Expected:

```text
[QA_READY_PROGRESS_02B_GAME_APPLICATION] status='Passed' cases='20'
```

## Manual restore

```text
Immersive Framework/QA/Setup/Activity Entry Readiness/
Restore Startup Progress Parity
```

## Acceptance

- zero C# compilation errors;
- Route Startup passes 25 cases;
- Game Application Startup passes 20 cases;
- typed diagnostics are participant-aware and terminal;
- progress is finite, monotonic and reaches 100% once;
- 100% precedes Hide;
- Route Hide precedes Transition reveal;
- surfaces finish hidden;
- transition gate is released;
- canonical Route authority is restored;
- fixture scene is released.
