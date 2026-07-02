# F46-ADR-Lifecycle-Kernel-Readiness-Decision

Status: Accepted / decision-only
Last updated: 2026-07-02
Supersedes: none
Superseded by: none

## Context

F44 created the lifecycle-local operation evidence kernel and projected `lifecycleOperation*` fields into real Route and Activity request logs.

F45 added lifecycle-local content/readiness evidence projection through `lifecycleContent*` and `lifecycleReadiness*` fields. F45 preserved Route/Activity dispatch, readiness policy, RuntimeContent behavior, ContentAnchor behavior, SceneLifecycle behavior and GameFlow behavior.

The remaining lifecycle kernel candidates are broad:

- content dispatch kernel
- readiness kernel
- full orchestration kernel
- GameFlow envelope

F46 decides whether another bounded runtime kernel patch is required before GameFlow, or whether the initial lifecycle evidence stabilization is sufficient to open the GameFlow envelope ADR.

## Decision

Choose Option B: Lifecycle kernel initial stabilization is sufficiently stabilized for now.

Lifecycle kernel initial stabilization is closed for now. GameFlow may proceed to ADR, not implementation, using the Route/Activity evidence produced by F44/F45.

No runtime patch is made in F46.

## Evidence Used

F44/F45 request logs already cover the operational evidence needed to reason about Route and Activity before GameFlow ADR work:

| Evidence | Coverage |
| --- | --- |
| Operation kind | `lifecycleOperationKind` |
| Stage count | `lifecycleOperationStages` |
| Stage names | `lifecycleOperationStageNames` |
| Stage statuses | `lifecycleOperationStageStatuses` |
| Blocking issues | `lifecycleOperationBlockingIssues` |
| Issue count | `lifecycleOperationIssues` |
| Side effects | `lifecycleOperationSideEffects` |
| Content status | `lifecycleContentStatus` |
| Content enter/exit | `lifecycleContentEnter`, `lifecycleContentExit` |
| Content request counts | `lifecycleContentEnterRequests`, `lifecycleContentExitRequests` |
| Content participants | `lifecycleContentParticipants`, `lifecycleContentParticipantSource` |
| Content handles | `lifecycleContentHandles` |
| Readiness | `lifecycleReadiness` |
| Readiness reason | `lifecycleReadinessReason` |
| Readiness issue count | `lifecycleReadinessIssues` |
| Readiness content blocker | `lifecycleReadinessBlockedByContent` |
| Loading adapter evidence | `loadingAdapterEvidence*` |

ActivityClear preserves readiness as `None` through `ActivityReadinessState`. It does not invent an active Activity. Content enter can remain a skipped or unexecuted phase when there is no target Activity; F46 does not infer a new domain status for a phase the domain did not execute.

Route content and Activity content remain distinct: Route logs project Route content enter/exit, while Activity logs project Activity content participant/lifecycle evidence. Older fields remain present for deeper domain detail.

## Pending Classification

| Pending work | F46 status | Justification |
| --- | --- | --- |
| content dispatch kernel | Deferred | Current dispatch is owned by RouteLifecycle/ActivityFlow. Moving it to Common would change ownership and risks behavior drift. Reopen only if repeated dispatch bugs or duplicated mechanics prove a concrete need. |
| readiness kernel | Deferred | Readiness policy is still domain-owned by ActivityFlow. Moving it now would turn diagnostics into policy. Reopen only if multiple readiness policies duplicate or a real readiness bug proves the need. |
| full orchestration kernel | Deferred | A broad lifecycle orchestrator would merge Route/Activity/Scene/Content concerns prematurely. F44/F45 evidence is enough for the next architectural decision. |
| GameFlow envelope | Next | The next safe step is an ADR defining the envelope boundary, not implementation. |

## Rejected Now

- No `GameFlowRequestEnvelope`.
- No GameFlow implementation.
- No content dispatch kernel.
- No readiness kernel.
- No full lifecycle orchestration kernel.
- No universal enum.
- No universal `Result<T>`.
- No `FrameworkResult`.
- No service locator, singleton or reflection workaround.
- No Route/Activity ordering change.
- No readiness decision change.
- No content enter/exit behavior change.
- No smoke button or QA Canvas asset change.

## Criteria To Reopen Lifecycle Kernel

Reopen lifecycle kernel only with a concrete, bounded problem:

- a real log inconsistency that hides existing evidence;
- duplicated lifecycle evidence projection that causes drift;
- a proven readiness bug across more than one local owner;
- a proven content dispatch bug that cannot be fixed inside RouteLifecycle or ActivityFlow;
- a GameFlow ADR requirement that cannot be satisfied by the F44/F45 evidence fields.

Do not reopen it just to normalize names, create a broader abstraction, or move policy into Common.

## Validation

F46 is decision-only. It does not require a new smoke and does not change runtime.

Static validation:

- `git diff --check`
- confirm no active `FXX` file was created
- confirm `Assets/_Documentation/Architecture` remains flat
- confirm no scenes, prefabs, serialized assets, `ProjectSettings`, `package.json`, asmdefs or `.csproj` changed

Manual validation remains the F45 owner-validated baseline:

- Standard Smoke
- Activity Baseline Smoke
- Route Scene Composition Smoke
- Route Release Smoke
- Composite Lifecycle Release Smoke
- Activity Content Anchor Diagnostics Smoke, when available

## Next Gate

F47 - GAMEFLOW-ADR-1 - GameFlow Request Envelope Boundary.
