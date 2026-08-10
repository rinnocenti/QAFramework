# ADR-004B Camera Negative Integrity QA

## Purpose

Certify the negative and transactional integrity boundaries required by
`IF-ADR-004B — Camera Negative Integrity Certification` without introducing a
parallel Camera runtime or changing `com.immersive.framework`.

The implementation reuses the canonical C9R Camera Override Authority fixture
for real Activity/Route lifecycle evidence and the existing Camera authoring
regressions for persistent-output validation.

## Revisions used to author this cut

```text
QAFramework:              f4ce36335878113e4b64e79d337c0645f6499707
com.immersive.framework:  bbaf05dbc7442290de8916fe312acd77a11f2b58
```

The final certification log prints both revisions. If either repository is
updated, re-audit the smoke against the new source before treating the embedded
revision evidence as current.

## Setup

Use the existing Camera setup. No ADR-004B-specific setup, scene, fixture,
context, service or output is added.

```text
Immersive Framework > QA > Setup > Camera > Install Camera Override Authority QA
```

## Execution

ADR-004B deliberately uses the real C9R lifecycle for cases 14-16.

1. Open the QA Hub and enter Play Mode.
2. Run the existing `Camera Override Authority` Hub entry.
3. Wait until C9R returns to the Hub and prints its canonical PASS.
4. Stay in the same Play Mode session.
5. Run:

```text
Immersive Framework > QA > Regressions > Camera >
Run ADR-004B Negative Integrity Certification
```

The final runner executes cases 1-13 directly, consumes the C9R evidence for
cases 14-16, and executes the canonical authoring regressions for cases 17-18.

If the final runner is invoked before C9R has completed in the same Play Mode
session, cases 14-16 are reported as `Blocked`; they are never assumed.

## Matrix ownership

```text
01-13  QaCameraAdr004BNegativeIntegrityRegression
14     canonical C9R Activity lifecycle cleanup evidence
15     canonical C9R Route lifecycle cleanup + persistent Session survivor preservation
16     focused C9R abnormal Route-owner disable probe
17     QaPersistentCameraPresentationCompositionRegression / two-outputs
18     QaCameraOutputSessionBindingAuthoringRegression / invalid references
```

Cases 11-13 use deterministic fixture-controlled rig invalidation. They do not
use timing, reflection or a new production fault-injection service:

- admission rollback: a higher-precedence request has a composer with no
  materialized Cinemachine Camera;
- release rollback: an invalid lower-precedence request is admitted while a
  valid higher-precedence request remains winner;
- rollback failure: the valid winner's physical rig is then removed before the
  release, making both replacement apply and rollback apply fail deterministically.


## Case 15 evidence boundary

Route lifecycle cleanup owns the canonical Route request only. The C9R proof
requires that the Route request is absent after Route exit and that the
synthetic Session survivor remains admitted. It deliberately does **not**
require that survivor to be the resulting winner.

The persistent `SessionCameraOverrideBinding` may be re-published by the
transition boundary and legitimately win by its higher precedence. Treating
`winner == synthetic survivor` as a Route-cleanup invariant is therefore a QA
false negative, not a framework contract.

## Case 16 decision gate

The owner-loss probe disables the active canonical `RouteCameraOverrideBinding`
while its request is admitted, captures whether the request remains in the
output context, then performs QA-owned cleanup so C9R can continue.

If the request remains admitted:

```text
[QA_CAMERA_ADR004B] case='16-abnormal-owner-loss' status='Failed' ... orphan='True'
```

The final verdict must be:

```text
ADR-004B NOT CERTIFIED — OWNER LOSS ORPHAN REPRODUCED; OPEN IF-ADR-004C
```

Do not change the package inside this cut. The failing evidence is the gate for
a separate narrow `IF-ADR-004C — Camera Owner Lifetime Integrity` correction.

If no orphan is reproduced, case 16 passes and 004C is not opened.

## Final verdict

Certification requires all 18 cases:

```text
[QA_CAMERA_ADR004B]
status='Passed'
cases='18/18'
failed='0'
blocked='0'
verdict='ADR-004B CAMERA NEGATIVE INTEGRITY CERTIFIED'
```

A failure or missing delegated prerequisite prevents the certified verdict.

## C9R result versus ADR-004B result

The owner-loss probe is observational and performs its own cleanup. It does not
change the 11-case positive C9R contract. Therefore C9R may still finish with
its canonical PASS while the ADR-004B probe has emitted `status='Failed'` for
case 16. That is intentional: C9R proves the supported lifecycle path; 004B
probes an abnormal owner-loss boundary and owns the certification verdict.

## Observed certification handoff — 2026-08-10

Manual C9R/ADR-004B execution reproduced abnormal Route-owner loss:

```text
case='16-abnormal-owner-loss'
operation='DisableRouteOwner'
admittedBefore='2'
admittedAfter='2'
orphan='True'
```

Cases 01-14 and 17-18 proved their intended boundaries. The initial case 15
failure was a QA evidence error caused by requiring the synthetic Session
survivor to be the winner after Route exit; that assertion has been narrowed
to owner-scoped cleanup and survivor preservation.

ADR-004B therefore remains **NOT CERTIFIED** because case 16 reproduced a
package owner-lifetime defect. The next cut is `IF-ADR-004C — Camera Owner
Lifetime Integrity`.
