# F44-ADR-Lifecycle-Operation-Evidence-Kernel

Status: Implemented locally / pending Unity validation
Last updated: 2026-07-01
Supersedes: none
Superseded by: none

## Context

`LIFECYCLE-KERNEL-REMAINING` remains partial. Route and Activity already emit many diagnostic fields, but operation evidence is spread across lifecycle runtimes, route/activity results, scene composition, content release, runtime scope, readiness, loading and transition projections.

F39 requires preserving original domain evidence. F43 hardened Loading adapter evidence in real request logs. F44 starts the remaining lifecycle kernel with the safest bounded piece: lifecycle-local operation evidence projection.

## Decision

Create a lifecycle-local operation evidence kernel under:

`Packages/com.immersive.framework/Runtime/Common/LifecycleOperations/`

The kernel records caller-owned evidence only. Route and Activity keep their own request/result/status types. The kernel does not decide domain success or failure.

Created types:

- `FrameworkLifecycleOperationKind`
- `FrameworkLifecycleOperationStage`
- `FrameworkLifecycleOperationStageEvidence`
- `FrameworkLifecycleOperationEvidence`
- `FrameworkLifecycleOperationEvidenceBuilder`
- `FrameworkLifecycleOperationDiagnostics`

`FrameworkRuntimeHost` projects this evidence into existing Route Request and Activity Request logs through additive `lifecycleOperation*` fields.

## Kernel Ownership

The kernel may own:

- normalized operation kind/source/reason text
- lifecycle-local operation kind
- lifecycle-local stage names
- stage evidence list
- stage status text received from Route/Activity/domain results
- issue and blocking issue counts received from domain results
- side-effect flags received from domain results
- progress/ledger counts received from domain results
- aggregate counters derived mechanically
- diagnostic string projection

The kernel must not own:

- route selection
- activity selection
- scene load/release
- content enter/exit
- RuntimeContent materialization/release
- ContentAnchor binding/placement
- loading/transition/pause application
- readiness decision
- lifecycle ordering
- GameFlow request envelope

## Current Shape Map

| Path | File/type | Operation | Observable stages | Original result/status | Issue counts | Side effects | Current diagnostics | Safe integration |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Route request log | `ApplicationLifecycle/FrameworkRuntimeHost.BuildRouteRequestFields` | Route request completion | Transition before/after, Loading before/after, route exit, route release, activity scene route release, route content exit/enter, route scene composition, route scope enter/exit, startup Activity projection | `FrameworkRouteRequestResult.Kind`, `RouteLifecycleStartResult`, `RouteSceneCompositionResult.Status`, `ContentReleaseResult.Status`, `ActivityFlowStartResult` | Existing blocking/issue counts from route scene composition, route release, activity scene release, route content callbacks and nested Activity projection | Existing loaded/released/applied flags and counts | Existing `Route Request completed` log fields | Integrate additively in log field projection only. |
| Activity request log | `ApplicationLifecycle/FrameworkRuntimeHost.BuildActivityRequestFields` | Activity request/clear completion | Transition before/after, Loading before/after, activity scene release/composition, activity scope enter/exit, activity content execution, content enter/exit, readiness, scene ledger | `FrameworkActivityRequestResult.Kind`, `ActivityFlowStartResult`, `ActivitySceneCompositionResult.Status`, `ActivitySceneReleaseResult.Status`, `ActivityReadinessState.Status` | Existing blocking/issue counts from activity scene composition/release, content execution, content lifecycle, readiness and ledger projection | Existing loaded/released/applied flags and counts | Existing `Activity Request completed` log fields | Integrate additively in log field projection only. |
| Route lifecycle runtime | `RouteLifecycle/RouteLifecycleRuntime` and result types | Route lifecycle execution | Route exit, scene composition, release, content enter/exit, scope | Domain-local result types | Domain-local | Domain-local | Already consumed by host logs | Do not rewrite in F44. |
| Activity flow runtime | `ActivityFlow/ActivityFlowRuntime` and result types | Activity start/clear | Activity scene composition/release, activity content execution, readiness, scope | Domain-local result types | Domain-local | Domain-local | Already consumed by host logs | Do not rewrite in F44. |
| Scene lifecycle | `SceneLifecycle/SceneLifecycleRuntime` | Scene load/unload | Primary/additive load, unload | `SceneLifecycleLoadResult`, `SceneLifecycleUnloadResult` | Not a lifecycle-kernel aggregate owner | Unity scene load/unload | Consumed through Route/Activity scene composition/release | Do not move scene execution into Common. |
| RuntimeContent/ContentAnchor | `RuntimeContent/**`, `ContentAnchor/**` | Logical scope/content/anchor evidence | scope roots, binding cleanup, discovery | Domain-local result types | Domain-local | Domain-local | Consumed by Route/Activity logs | Preserve semantics; no F44 behavior change. |

## Runtime Projection

Route Request logs now include:

- `lifecycleOperationKind`
- `lifecycleOperationStages`
- `lifecycleOperationBlockingIssues`
- `lifecycleOperationIssues`
- `lifecycleOperationSideEffects`
- `lifecycleOperationFailedStages`
- `lifecycleOperationSkippedStages`
- `lifecycleOperationStageNames`
- `lifecycleOperationStageStatuses`
- `lifecycleOperationDiagnostics`

Activity Request logs expose the same fields.

The projection uses only evidence already available in `FrameworkRuntimeHost` at log time. Missing per-stage detail remains missing; F44 does not infer scene/content/readiness behavior.

## Consequences

This cut starts `LIFECYCLE-KERNEL-REMAINING`, but does not close it.

Closed in F44:

- lifecycle operation evidence kernel
- stage/step ledger projection
- Route/Activity diagnostic projection

Still pending:

- full lifecycle orchestration kernel
- content dispatch kernel
- readiness kernel
- GameFlow envelope

## Rejected Scope

- No `FrameworkResult`
- No universal `Result<T>`
- No universal enum
- No `FrameworkLifecycleStatus`
- No GameFlow envelope
- No movement of Route/Activity orchestration to Common
- No scene load/release movement to Common
- No content dispatch movement to Common
- No service locator/singleton/reflection
- No lifecycle behavior/order change
- No smoke button or QA Canvas asset change

## Validation

Static validation is required in F44. Unity validation remains pending because runtime C# changed and Unity must refresh generated project files.

Manual validation after import/compile:

1. Unity import/compile.
2. Standard Smoke.
3. Activity Baseline Smoke.
4. Route Scene Composition Smoke.
5. Route Release Smoke.
6. Composite Lifecycle Release Smoke.
7. Inspect real Route Request completed logs for `lifecycleOperation*`.
8. Inspect real Activity Request completed logs for `lifecycleOperation*`.

Available related QA Canvas buttons inspected in F44:

- `Run Standard Smoke`
- `Run Activity Baseline Smoke`
- `Run Route Scene Composition Smoke`
- `Run Route Release Smoke`
- `Run Composite Lifecycle Release Smoke`
- `Run Scope Tail Operation Synthetic Smoke`

No direct lifecycle operation evidence smoke button exists; validate through Standard/Route/Activity logs until a future diagnostics cut wires one intentionally.

## Next Gate

If F44 validates, next gate should be `LIFECYCLE-KERNEL-2`, not `GAMEFLOW`.
