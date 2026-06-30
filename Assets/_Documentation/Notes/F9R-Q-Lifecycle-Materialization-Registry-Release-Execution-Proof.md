# F9R-Q — Lifecycle Materialization Registry Release Execution Proof

Status: Closed / PASS
Type: RuntimeContent / LifecycleMaterialization hardening proof
Scope: `Packages/com.immersive.framework`

## Goal

Prove that a lifecycle-owned release plan can be executed explicitly through a caller-provided runtime release executor.

The execution path is explicit:

```text
LifecycleMaterializationRegistry.CreateReleasePlan
  -> LifecycleMaterializationRegistry.ExecuteReleasePlan
  -> delegated RuntimeReleaseRequest executor
  -> lifecycle registry entry state update
```

This cut does not connect release execution to Route/Activity exit.

## Implemented

- Added `LifecycleMaterializationReleaseExecutionStatus`.
- Added `LifecycleMaterializationReleaseExecutionResult`.
- Added `LifecycleMaterializationRegistry.ExecuteReleasePlan(...)`.
- Added QA smoke button:

```text
Run Lifecycle Registry Release Execution Smoke
```

## Execution contract

`ExecuteReleasePlan` receives:

- a `LifecycleMaterializationReleasePlan`;
- a caller-provided `Func<RuntimeReleaseRequest, RuntimeReleaseResult>` executor;
- source/reason diagnostics.

The registry does not execute physical release directly. It delegates each `RuntimeReleaseRequest` and mirrors the result into lifecycle registry state:

| Delegated result | Lifecycle registry state |
|---|---|
| runtime release success | `Released` |
| runtime release failure | `ReleaseFailed` |
| missing lifecycle entry | partial failure result |

## Smoke evidence

QA smoke completed with PASS:

```text
QA Lifecycle Registry Release Execution Smoke step completed.
step='lifecycle-materialization-registry-release-execution'
passed='True'
plan='SucceededPlanned'
planRequests='2'
execution='SucceededReleasedAll'
executedRequests='2'
releaseRequested='2'
released='2'
releaseFailed='0'
missingEntries='0'
releaseResults='2'
registryResults='4'
repeatedPlan='SucceededEmpty'
repeatedExecution='SucceededNoRequests'
repeatedRequests='0'
entries='2'
active='0'
registryReleaseRequested='0'
registryReleased='2'
registryReleaseFailed='0'
runtimeHandles='0'
firstHandleReleased='True'
secondHandleReleased='True'
explicitSubmit='True'
releaseExecution='True'
physicalRelease='False'
logicalRuntimeContentRelease='True'
contentAnchorBindingCleanup='False'
automaticLifecycleWiring='False'
routeActivityAutoMaterialization='False'
routeActivityAutoRelease='False'
addressables='False'
pooling='False'
actorSpawn='False'
playerJoin='False'
gameplayConsumer='False'
cameraConsumer='False'
audioConsumer='False'
saveConsumer='False'
```

## Closeout

F9R-Q is closed / PASS. It proves explicit lifecycle registry release execution and keeps Route/Activity auto-release blocked.

## Explicit non-goals

F9R-Q does not implement:

- physical release from lifecycle registry;
- ContentAnchor binding cleanup;
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

This cut proves explicit lifecycle release execution. It does not authorize automatic release on Route/Activity exit.
