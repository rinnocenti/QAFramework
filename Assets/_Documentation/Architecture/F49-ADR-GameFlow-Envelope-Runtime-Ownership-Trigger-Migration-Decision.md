# F49-ADR-GameFlow-Envelope-Runtime-Ownership-Trigger-Migration-Decision

Status: Accepted / decision-only  
Last updated: 2026-07-02  
Track: GAMEFLOW - Lifecycle Request Envelope  
Depends on:

- `F47-ADR-GameFlow-Request-Envelope-Boundary.md`
- `F48-ADR-GameFlow-Request-Envelope-Shell.md`

Next gate: `F50 - GAMEFLOW-3 - Route/Activity Trigger Migration ADR`

## Context

F47 accepted the GameFlow request envelope boundary. F48 created the passive internal envelope shell and projected `gameFlowEnvelope*` fields into real Route, Activity and ActivityClear request logs.

F48 has been owner-validated through Unity import/compile and the existing Standard, Route/Activity lifecycle and ContentAnchor diagnostics smokes. The validated shape is diagnostic/passive and does not change Route, Activity, SceneLifecycle, RuntimeContent, ContentAnchor, Loading, Transition, Pause, InputMode or trigger behavior.

F49 decides ownership of envelope creation. It does not migrate triggers and does not alter runtime behavior.

## Decision

Choose Option A - Keep Host Projection.

`FrameworkRuntimeHost` remains the current owner of envelope creation and `gameFlowEnvelope*` log projection. `GameFlowRuntime` remains the owner of request admission, in-flight state, transition calls and delegation to Route/Activity lifecycle runtimes. Route/Activity result types remain domain result carriers and do not store the envelope in F49.

No runtime patch is made in F49.

## Current Ownership Map

| Area | Current owner | F49 decision |
| --- | --- | --- |
| Route request execution and admission | `GameFlowRuntime` | Keep |
| Activity request/clear execution and admission | `GameFlowRuntime` | Keep |
| Route/Activity request result kinds and messages | `FrameworkRouteRequestResult`, `FrameworkActivityRequestResult` | Keep |
| Envelope shell types | `Runtime/GameFlow/Requests` | Keep |
| Envelope construction | `FrameworkRuntimeHost.BuildRouteRequestFields`, `BuildActivityRequestFields` | Keep |
| `gameFlowEnvelope*` log projection | `FrameworkRuntimeHost` | Keep |
| F44 lifecycle operation evidence | `FrameworkRuntimeHost` projection over domain results | Keep |
| F45 content/readiness evidence | `FrameworkRuntimeHost` projection over domain results | Keep |
| Loading adapter evidence | `FrameworkRuntimeHost` request-level loading diagnostics | Keep |
| Route/Activity triggers | `RouteRequestTrigger`, `ActivityRequestTrigger` | Not migrated |
| FlowTrigger helper | `Runtime/Common/FlowTriggers` | Unchanged |

## Selected Option

Option A is selected because the validated envelope combines evidence that is not wholly produced inside `GameFlowRuntime`:

- `FrameworkLoadingDiagnostics` is assembled by `FrameworkRuntimeHost` around request-level Loading surface execution and skipped/no-op policies.
- `FrameworkLifecycleOperationEvidence` is built in `FrameworkRuntimeHost` from Route/Activity domain results and Loading diagnostics.
- `FrameworkLifecycleContentEvidence` and `FrameworkLifecycleReadinessEvidence` are F45 projections built in `FrameworkRuntimeHost`.
- `gameFlowEnvelopeValidationMode` is currently `Unknown` because the F48 builder does not receive a local validation mode value; correcting that is a small projection improvement candidate, not a reason to move ownership.
- Route, Activity and ActivityClear are symmetric enough for logging, but not enough to move envelope creation without changing result shapes or duplicating host-side evidence assembly.

Keeping host projection preserves F48's validated behavior and avoids premature movement of diagnostics into request execution.

## Rejected Options

### Option B - Move Result Ownership

Rejected for F49.

Moving envelope storage into `FrameworkRouteRequestResult` and `FrameworkActivityRequestResult` would either:

- require `GameFlowRuntime` to know about Loading diagnostics and lifecycle/content/readiness projections that are currently assembled by the host; or
- require the host to build the envelope and then mutate/wrap the result after execution; or
- expand result object responsibilities without a runtime behavior need.

Those shapes add coupling or duplication before a concrete consumer needs result-owned envelopes.

### Option C - Partial Ownership

Rejected.

Introducing Activity-only or Route-only envelope ownership would create new asymmetry. The current system is already symmetric at the log projection boundary, so there is no reason to split ownership.

## Trigger Migration Decision

Route and Activity triggers remain unmigrated in F49.

Future adoption of the FlowTrigger helper may be considered only after envelope ownership remains stable. Even then, the helper may own only local submitted/completed/failed/succeeded/ignored bookkeeping. It must not:

- choose Route or Activity policy;
- infer targets;
- change request semantics;
- alter in-flight/admission behavior;
- hide domain failures;
- replace domain result mapping.

RouteRequestTrigger and ActivityRequestTrigger continue to own authored target selection and calls into `FrameworkRuntimeHost`.

## Runtime Patch

None.

F49 is decision-only. It intentionally does not move envelope construction, change result objects, migrate triggers, change log fields, change request ordering or add smoke buttons.

## Validation Plan

Static validation:

- `git diff --check`;
- confirm no active `FXX` file exists;
- confirm `Assets/_Documentation/Architecture` remains flat;
- confirm no scenes, prefabs, serialized assets, `ProjectSettings`, `package.json`, asmdefs or `.csproj` changed by F49;
- confirm `RouteRequestTrigger` and `ActivityRequestTrigger` were not altered by F49;
- confirm `Runtime/Common/FlowTriggers` was not altered by F49.

Manual Unity validation:

- Not required for F49 because it is decision-only.
- Use the F48 owner-validated smoke baseline for the current runtime shell.

## Next Gate

`F50 - GAMEFLOW-3 - Route/Activity Trigger Migration ADR`

F50 should decide whether Route/Activity triggers should adopt the FlowTrigger helper for local state only, now that envelope ownership remains stable as host projection.
