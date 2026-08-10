# C9R — Camera Override Authority QA

Status: **Canonical positive Camera authority regression**  
Last updated: **2026-08-10**

## Contract

The current product precedence convention is:

```text
Local Player  50
Activity     100
Route        200
Session      300
```

Higher precedence wins. Request timing is not priority policy.

Route and Activity lifecycle entry make their override available. The QA
explicitly requests/releases overrides, verifies restoration and lets canonical
owner exit prove cleanup.

## Composition

Persistent QA content owns exactly one:

```text
CameraOutputSessionBinding
SessionCameraOverrideBinding
Unity Camera
CinemachineBrain
```

The arbitration scene does not create another physical Camera output. Player,
Activity and Route consumers receive the persistent output through
`CameraOutputInjectionRuntime`.

## Setup

Install/repair the existing C9R composition:

```text
Immersive Framework > QA > Setup > Camera >
Install Camera Override Authority QA
```

Successful setup emits:

```text
[_CAMERA_OVERRIDE_AUTHORITY_SETUP] status='Succeeded'
```

## Canonical 11 cases

C9R keeps one fixed positive-contract count:

```text
01 player-default
02 activity-request
03 route-request
04 session-request
05 session-release-restores-route
06 route-release-restores-activity
07 activity-release-restores-player
08 duplicate-request
09 duplicate-release
10 activity-lifecycle-cleanup
11 route-lifecycle-cleanup
```

Final success evidence:

```text
[CAMERA_RUNTIME_HOST_INTEGRATION_REGRESSION]
status='Passed'
phase='canonical-override-fixture'
cases='11'
```

## ADR-004B / ADR-004C evidence reuse

C9R remains the positive lifecycle owner. It also records focused evidence for
004B/004C while keeping the canonical count at 11.

Current additional probes include:

- Activity abnormal disable;
- non-winning Activity disable under Route winner;
- Session disable;
- Activity destruction;
- Route abnormal disable / re-enable;
- cleanup idempotence;
- no silent re-publication.

These probes do not become additional C9R cases. They are consumed by the ADR
certification runners in the **same Play Mode session**.

Expected sequence:

```text
Setup Camera
  -> Play Mode
  -> Camera Override Authority / C9R
  -> C9R 11/11
  -> ADR-004C 10/10
  -> ADR-004B 18/18
```

## Current certified result

```text
C9R      PASS 11/11
ADR004C  PASS 10/10
ADR004B  PASS 18/18
```

The former 004B abnormal Route-owner probe now proves `orphan='False'` after the
004C package hardening.

## QA teardown note

A post-certification teardown diagnostic in the synthetic Local Player binding
was classified as QA cleanup hygiene: the local publisher could attempt a
redundant release after its request was already absent from the output context.
The v10 QA-only patch reconciles that state before releasing again.

This teardown hygiene is outside the canonical 11-case result and does not
replace or weaken C9R evidence.
