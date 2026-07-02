# F45-ADR-Lifecycle-Content-Readiness-Evidence-Projection

Status: Implemented locally / pending Unity validation
Last updated: 2026-07-02
Supersedes: none
Superseded by: none

## Context

F44 created the lifecycle-local operation evidence kernel and projected `lifecycleOperation*` fields into real Route and Activity request logs. Content and readiness evidence was still spread across Route content callback fields, Activity content lifecycle fields, Activity content participant execution fields, Activity readiness fields and ContentAnchor cleanup/binding diagnostics.

The next safe lifecycle kernel step is to make that evidence easier to read operationally without moving dispatch, changing readiness policy or creating a GameFlow envelope.

## Decision

Content/readiness evidence projection is lifecycle-local.

`RuntimeContent`, `ContentAnchor`, `RouteLifecycle` and `ActivityFlow` continue to own real execution and domain result/status semantics. F45 only projects values that already exist at request log time.

`lifecycleOperation*` remains the common operational ledger. F45 adds dedicated `lifecycleContent*` and `lifecycleReadiness*` fields to Route and Activity request logs so content/readiness can be read without reconstructing many older fields manually.

Domain status remains the primary source of truth. The new projection does not authorize:

- a content dispatch kernel
- a readiness kernel
- a GameFlow request envelope
- a universal enum
- a universal `Result<T>`
- `FrameworkResult`

## Evidence Map

| Evidence | Current source | Current log fields | F45 projection |
| --- | --- | --- | --- |
| Route content enter | `RouteContentLifecycleDispatchResult` from `RouteLifecycleStartResult.RouteContentEnterResult` | `routeContentEnterReceivers` plus route content callback diagnostics | `lifecycleContentEnter`, `lifecycleContentEnterRequests`, `lifecycleContentBlockingIssues`, `lifecycleContentDiagnostics` in Route request logs |
| Route content exit | `RouteContentLifecycleDispatchResult` from `RouteLifecycleStartResult.RouteContentExitResult` | `routeContentExitReceivers` plus route content callback diagnostics | `lifecycleContentExit`, `lifecycleContentExitRequests`, `lifecycleContentBlockingIssues`, `lifecycleContentDiagnostics` in Route request logs |
| Activity content lifecycle | `ActivityContentLifecycleResult` from `ActivityContentApplyResult.LifecycleResult` | `activityContentLifecycle`, `activityContentEnterFailed`, `activityContentExitFailed` | Preserved in old fields and enriched inside `lifecycleOperationStageStatuses` ContentEnter/ContentExit stages |
| Activity participant execution | `ActivityContentExecutionLifecycleResult` from `ActivityFlowStartResult.ActivityContentExecutionResult` | `activityContentExecution*`, `activityContentParticipant*` | `lifecycleContentStatus`, `lifecycleContentEnter`, `lifecycleContentExit`, `lifecycleContentParticipants`, `lifecycleContentParticipantSource`, `lifecycleContentBlocksReadiness`, `lifecycleContentDiagnostics` in Activity request logs |
| Activity readiness | `ActivityReadinessState` from `ActivityFlowStartResult.ActivityReadinessState` | `activityReadiness`, `activityReadinessReason`, `activityReadinessIssues` | `lifecycleReadiness`, `lifecycleReadinessReason`, `lifecycleReadinessIssues`, `lifecycleReadinessBlockedByContent` in Route and Activity request logs |
| Content handles | `RouteContentSet.Count` and `ActivityContentApplyResult.ActivityContentCount` | `routeContentHandles`, `activityContentHandles` | `lifecycleContentHandles` |
| ContentAnchor cleanup/binding | `ContentAnchorBindingLifecycleResult` and discovery results | `routeContentAnchorBindingCleanup*`, `activityContentAnchorBindingCleanup*`, `contentAnchor*`, `activityContentAnchor*` | Preserved as domain fields; not folded into content dispatch or readiness policy |

## Runtime Projection

F45 adds internal non-MonoBehaviour projection types under:

`Packages/com.immersive.framework/Runtime/Common/LifecycleOperations/`

- `FrameworkLifecycleContentEvidence`
- `FrameworkLifecycleContentEvidenceProjection`
- `FrameworkLifecycleReadinessEvidence`
- `FrameworkLifecycleReadinessEvidenceProjection`

These types receive textual statuses, counters and booleans prepared by the lifecycle log owner. They do not reference RouteLifecycle, ActivityFlow, RuntimeContent, ContentAnchor or UnityEngine.

Route request logs project Route content evidence into `lifecycleContent*` and startup Activity readiness evidence into `lifecycleReadiness*`.

Activity request logs project Activity content participant/lifecycle evidence into `lifecycleContent*` and Activity readiness into `lifecycleReadiness*`.

`lifecycleOperationStageStatuses` is also made more explicit for ContentEnter, ContentExit, ActivityContentExecution and Readiness by appending request, issue and content-blocking counts where those values already exist.

## Rejected Scope

- No RuntimeContent execution change.
- No ActivityFlow readiness change.
- No RouteLifecycle sequencing change.
- No SceneLifecycle change.
- No ContentAnchor binding change.
- No content dispatch movement to Common.
- No `ContentDispatchKernel`.
- No `ReadinessKernel`.
- No `GameFlowRequestEnvelope`.
- No smoke button or QA Canvas asset change.
- No removal or rename of older diagnostic fields.

## Validation

Static validation is required for F45. Unity validation remains pending because runtime C# changed and Unity must refresh/import the package.

Manual validation after import/compile:

1. Unity import/compile.
2. Standard Smoke.
3. Activity Baseline Smoke.
4. Route Scene Composition Smoke.
5. Route Release Smoke, if available in the current QA Canvas.
6. Activity Content Anchor Diagnostics Smoke, if available in the current QA Canvas.
7. Composite Lifecycle Release Smoke, if available in the current QA Canvas.
8. Inspect real Route Request completed logs for `lifecycleOperation*`, `lifecycleContent*`, `lifecycleReadiness*` and preserved `loadingAdapterEvidence*`.
9. Inspect real Activity Request completed logs for `lifecycleOperation*`, `lifecycleContent*`, `lifecycleReadiness*` and ActivityClear readiness `None`.

## Next Gate

F45 does not close all lifecycle kernel work. Remaining candidates are content dispatch kernel, readiness kernel, full orchestration kernel and GameFlow envelope. The next gate should be `LIFECYCLE-KERNEL-3` if another safe evidence/kernel cut remains before GameFlow; otherwise GameFlow needs a separate ADR justification.
