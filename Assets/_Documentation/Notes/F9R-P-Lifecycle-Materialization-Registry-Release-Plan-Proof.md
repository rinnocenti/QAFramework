# F9R-P — Lifecycle Materialization Registry Release Plan Proof

Status: Closed / PASS
Type: RuntimeContent / LifecycleMaterialization hardening proof
Scope: `Packages/com.immersive.framework`

## Goal

Prove that the lifecycle-owned materialization registry can build a passive release plan from registered materialization evidence.

The release plan answers:

```text
Which registered materialized entries currently need release for this owner or scope?
```

This cut does not execute release.

## Implemented

- Added `LifecycleMaterializationReleasePlan`.
- Added `LifecycleMaterializationReleasePlanStatus`.
- Added `LifecycleMaterializationReleasePlanTargetKind`.
- Added owner-targeted release planning to `LifecycleMaterializationRegistry`.
- Added scope-targeted release planning to `LifecycleMaterializationRegistry`.
- Added QA smoke button:

```text
Run Lifecycle Registry Release Plan Smoke
```

## Release candidate rule

The plan includes entries whose lifecycle materialization state is:

- `Active`
- `ReleaseFailed`

The plan skips entries whose state is:

- `ReleaseRequested`
- `Released`

This matches the existing registry transition contract: `Active` and `ReleaseFailed` can be requested for release, while `ReleaseRequested` is already pending and `Released` is complete.

## Expected smoke evidence

Expected QA log:

```text
QA Lifecycle Registry Release Plan Smoke step completed.
step='lifecycle-materialization-registry-release-plan'
passed='True'
ownerPlan='SucceededPlanned'
ownerRequests='2'
ownerTotalEntries='4'
ownerActiveCandidates='1'
ownerReleaseFailedCandidates='1'
ownerSkippedReleaseRequested='1'
ownerSkippedReleased='1'
repeatedPlanStable='True'
scopePlan='SucceededPlanned'
scopeRequests='3'
scopeTotalEntries='5'
scopeActiveCandidates='2'
scopeReleaseFailedCandidates='1'
scopeSkippedReleaseRequested='1'
scopeSkippedReleased='1'
emptyPlan='SucceededEmpty'
emptyRequests='0'
releasePlanQueryOnly='True'
releaseExecution='False'
physicalRelease='False'
logicalRuntimeContentRelease='False'
contentAnchorBindingCleanup='False'
automaticLifecycleWiring='False'
routeActivityAutoMaterialization='False'
routeActivityAutoRelease='False'
```


## Smoke closure evidence

Validated by user-provided QA smoke. The smoke completed with:

```text
passed='True'
ownerPlan='SucceededPlanned'
ownerRequests='2'
ownerTotalEntries='4'
ownerActiveCandidates='1'
ownerReleaseFailedCandidates='1'
ownerSkippedReleaseRequested='1'
ownerSkippedReleased='1'
repeatedPlanStable='True'
scopePlan='SucceededPlanned'
scopeRequests='3'
scopeTotalEntries='5'
scopeActiveCandidates='2'
scopeReleaseFailedCandidates='1'
scopeSkippedReleaseRequested='1'
scopeSkippedReleased='1'
emptyPlan='SucceededEmpty'
emptyRequests='0'
releasePlanQueryOnly='True'
releaseExecution='False'
physicalRelease='False'
logicalRuntimeContentRelease='False'
contentAnchorBindingCleanup='False'
automaticLifecycleWiring='False'
routeActivityAutoMaterialization='False'
routeActivityAutoRelease='False'
```

Closure result:

```text
IF-FW-F9R-P — CLOSED / PASS
```

## Explicit non-goals

F9R-P does not implement:

- physical release;
- logical RuntimeContent release;
- ContentAnchor binding cleanup;
- lifecycle release execution;
- Route/Activity lifecycle integration;
- Route/Activity auto-release;
- Route/Activity auto-materialization;
- Pause consumer;
- Camera consumer;
- Audio consumer;
- Save/progression consumer;
- Actor materialization;
- Pooling/runtime-spawned integration;
- PlayerJoin;
- F34/gameplay.

## Boundary

The release plan is query-only. It creates `RuntimeReleaseRequest` values that a later explicit executor can consume, but this cut does not consume them.
