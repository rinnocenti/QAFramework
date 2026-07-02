# F45-ADR-Lifecycle-Content-Readiness-Evidence-Projection

Status: Closed / content-readiness evidence projection owner-validated
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

F45 is owner-validated. Validation covered Unity import/compile through the user-run Editor workflow and the available QA smokes:

1. Standard Smoke.
2. Activity Baseline Smoke.
3. Route Scene Composition Smoke.
4. Route Release Smoke.
5. Composite Lifecycle Release Smoke.
6. Activity Content Anchor Diagnostics Smoke.

Validation confirmed real Route Request completed logs include `lifecycleOperation*`, `lifecycleContent*`, `lifecycleReadiness*` and preserved `loadingAdapterEvidence*` fields.

Validation also confirmed real Activity Request completed logs include `lifecycleOperation*`, `lifecycleContent*` and `lifecycleReadiness*`, including ActivityClear readiness `None` without inventing an active Activity.

## Next Gate

F45 does not close all lifecycle kernel work by itself. F46 subsequently accepted that initial lifecycle evidence stabilization is closed for now and classified content dispatch kernel, readiness kernel and full orchestration kernel as deferred.

The active next gate is `F47 - GAMEFLOW-ADR-1 - GameFlow Request Envelope Boundary`.
