# F48-ADR-GameFlow-Request-Envelope-Shell

Status: Implemented locally / pending Unity validation  
Last updated: 2026-07-02  
Track: GAMEFLOW - Lifecycle Request Envelope  
Depends on: `F47-ADR-GameFlow-Request-Envelope-Boundary.md`  
Next gate: `F49 - GAMEFLOW-2 - Envelope Runtime Ownership / Trigger Migration Decision`

## Context

F47 accepted the GameFlow request envelope as a future request/diagnostics boundary. The accepted boundary requires a passive shell that preserves existing Route, Activity, lifecycle, content/readiness and Loading evidence without executing flow or deciding policy.

Current Route and Activity request logs are built in `FrameworkRuntimeHost` after `GameFlowRuntime` returns domain results. That point already has the domain status plus F44/F45 lifecycle projections and Loading adapter evidence. F48 therefore creates the first shell at log-projection time instead of moving ownership deeper into `GameFlowRuntime`.

## Decision

Implement the minimal runtime shell authorized by F47.

Created internal/package-local runtime types under `Runtime/GameFlow/Requests/`:

- `GameFlowRequestEnvelope`
- `GameFlowRequestOperationKind`
- `GameFlowRequestAdmission`
- `GameFlowRequestEnvelopeDiagnostics`
- `GameFlowRequestEnvelopeBuilder`

The shell is passive:

- no `MonoBehaviour`;
- no `UnityEngine` dependency;
- no Unity serialization;
- no side effects;
- no lifecycle execution;
- no request admission decision;
- no target selection;
- no replacement for Route or Activity result/status types.

## Runtime Shape

`FrameworkRuntimeHost` now builds a `GameFlowRequestEnvelope` after Route or Activity domain results already exist.

The integration is additive. Existing `kind`, `lifecycleOperation*`, `lifecycleContent*`, `lifecycleReadiness*`, `loadingAdapterEvidence*`, Route and Activity fields remain in place.

ActivityClear is preserved as a local GameFlow operation kind through `FrameworkActivityRequestResult.OperationKind`. This is diagnostics-only and avoids inferring clear semantics only from a missing target Activity.

## Envelope Fields

The shell copies text/counters only:

- operation kind;
- admission;
- source;
- reason;
- target/previous Route names;
- target/previous Activity names;
- transition status text;
- loading status text;
- validation mode text when known;
- original domain status text;
- lifecycle operation kind/stage/blocking/failed/skipped counts;
- lifecycle content status/blocking/content-handle counts;
- lifecycle readiness status/reason/issue count;
- Loading adapter evidence count/applied/skipped/failed/blocking counts.

Unavailable target names remain `<none>`. Unknown evidence remains `Unknown`. No domain object is retained by the envelope.

## Ownership Boundary

| Concern | Owner |
| --- | --- |
| Passive GameFlow request diagnostics shell | `Runtime/GameFlow/Requests` |
| Current shell creation and log projection | `FrameworkRuntimeHost` |
| Route request execution/result/status | GameFlow + Route lifecycle runtime |
| Activity request/clear execution/result/status | GameFlow + ActivityFlow runtime |
| Lifecycle operation/content/readiness evidence | F44/F45 lifecycle evidence projections |
| Loading adapter evidence | Loading diagnostics |
| Trigger local state | Existing triggers and FlowTrigger helper where already adopted |

F48 does not move execution ownership. A future gate may decide whether envelope creation should move deeper into `GameFlowRuntime`.

## Rejected Scope

F48 does not:

- create a new GameFlow orchestrator;
- create a request pipeline;
- change request admission behavior;
- change Route/Activity target selection;
- change RouteLifecycle, ActivityFlow, SceneLifecycle, RuntimeContent, ContentAnchor, Loading, Transition, Pause or InputMode behavior;
- migrate `RouteRequestTrigger` or `ActivityRequestTrigger`;
- create public API;
- create `FrameworkResult`;
- create a universal `Result<T>`;
- create a framework-wide status enum;
- create a smoke button or QA Canvas asset change;
- close content dispatch kernel, readiness kernel or full orchestration kernel work.

## Diagnostics Contract

Route and Activity request logs now include `gameFlowEnvelope*` fields. The new fields summarize request-level diagnostics only. Domain status remains primary.

Use:

- `kind` and Route/Activity domain fields for the authoritative result;
- `gameFlowEnvelope*` for the bounded request envelope summary;
- `lifecycleOperation*` for stage ledger details;
- `lifecycleContent*` and `lifecycleReadiness*` for content/readiness projections;
- `loadingAdapterEvidence*` for adapter-level Loading evidence.

## Validation Plan

Static validation:

- `git diff --check`;
- confirm no active `FXX` file exists;
- confirm `Assets/_Documentation/Architecture` remains flat;
- confirm no scenes, prefabs, serialized assets, `ProjectSettings`, `package.json`, asmdefs or `.csproj` changed;
- confirm new shell files do not use `UnityEngine` or `MonoBehaviour`;
- confirm no `FrameworkResult`, universal `Result<T>` or framework-wide status enum was created;
- confirm Route/Activity triggers were not migrated.

Manual Unity validation remains pending:

1. Unity import/compile.
2. Standard Smoke.
3. Activity Baseline Smoke.
4. Route Scene Composition Smoke.
5. Route Release Smoke, if available.
6. Composite Lifecycle Release Smoke, if available.
7. Activity Content Anchor Diagnostics Smoke, if available.
8. Inspect Route and Activity request logs for `gameFlowEnvelope*` plus preserved existing fields.

## Next Gate

`F49 - GAMEFLOW-2 - Envelope Runtime Ownership / Trigger Migration Decision`.

F49 should decide whether the envelope stays as a `FrameworkRuntimeHost` diagnostics projection, moves deeper into `GameFlowRuntime` without behavior changes, or gates a later Route/Activity trigger adoption cut.
