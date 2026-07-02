# F52-ADR-GameFlow-Request-API-Decision

Status: Accepted / ADR-only  
Last updated: 2026-07-02  
Track: GAMEFLOW - Lifecycle Request Envelope  
Depends on:

- `F39-ADR-Status-Mapping-Policy.md`
- `F47-ADR-GameFlow-Request-Envelope-Boundary.md`
- `F48-ADR-GameFlow-Request-Envelope-Shell.md`
- `F49-ADR-GameFlow-Envelope-Runtime-Ownership-Trigger-Migration-Decision.md`
- `F50-ADR-Route-Activity-Trigger-Migration.md`
- `F51-ADR-Route-Activity-Trigger-FlowTrigger-Adoption.md`

Next gate: `F53 - Architecture Consolidation Next-Track Decision`

## Context

F47 accepted the GameFlow request envelope boundary. F48 implemented the passive internal envelope shell and projected `gameFlowEnvelope*` diagnostics into Route and Activity request logs. F49 kept envelope creation/projection in `FrameworkRuntimeHost`. F50 approved Route/Activity trigger helper adoption, and F51 implemented that migration without changing request semantics.

F52 decides whether the framework needs a new public or internal GameFlow request API now.

## Decision

Choose Option A - Keep FrameworkRuntimeHost as Request API.

`FrameworkRuntimeHost` remains the request API boundary for current Route and Activity requests. No public GameFlow request API, internal request API shell, request bus, command dispatcher, service locator, singleton or public `IGameFlowService` is created in F52.

`GameFlowRuntime` remains an internal execution/admission runtime. `GameFlowRequestEnvelope` remains an internal passive diagnostics shell and is not promoted to public API.

## Current Request Entry Points

| Entry point | Current owner | Scope |
| --- | --- | --- |
| `FrameworkRuntimeHost.RequestRouteAsync` | `FrameworkRuntimeHost` | Host-facing Route request boundary; handles state refresh, Loading surface integration and request log projection around `GameFlowRuntime`. |
| `FrameworkRuntimeHost.RequestActivityAsync` | `FrameworkRuntimeHost` | Host-facing Activity request boundary; handles state refresh, Loading diagnostics and request log projection around `GameFlowRuntime`. |
| `FrameworkRuntimeHost.ClearActivityAsync` | `FrameworkRuntimeHost` | Host-facing ActivityClear request boundary; keeps clear semantics explicit and projects ActivityClear diagnostics. |
| `GameFlowRuntime.RequestRouteAsync` | `GameFlowRuntime` | Internal execution/admission/delegation to Route lifecycle. |
| `GameFlowRuntime.RequestActivityAsync` | `GameFlowRuntime` | Internal execution/admission/delegation to ActivityFlow. |
| `GameFlowRuntime.ClearActivityAsync` | `GameFlowRuntime` | Internal ActivityClear execution/admission/delegation. |

## Current Consumers

| Consumer | Current behavior |
| --- | --- |
| `RouteRequestTrigger` | Authored Route target/reason boundary; calls `FrameworkRuntimeHost.RequestRouteAsync`; uses FlowTrigger helper only for local diagnostics. |
| `ActivityRequestTrigger` | Authored Activity target/reason and explicit clear boundary; calls `FrameworkRuntimeHost.RequestActivityAsync` and `ClearActivityAsync`; uses FlowTrigger helper only for local diagnostics. |
| `FrameworkQaCanvas` | QA scenario caller; uses `FrameworkRuntimeHost` request methods for Route/Activity smokes and validates resulting diagnostics. |
| Startup flow | `FrameworkRuntimeHost.StartAsync` creates and owns `GameFlowRuntime` for the application lifetime. |
| ObjectEntry QA/smoke paths | Internal QA consumers use the host request methods when they need Route/Activity transitions. |

No current consumer requires a direct GameFlow service API separate from `FrameworkRuntimeHost`.

## API Options

| Option | Decision | Reason |
| --- | --- | --- |
| Option A - Keep `FrameworkRuntimeHost` as Request API | Accepted | Current consumers already call the host, and the host owns the surrounding state refresh, Loading diagnostics, lifecycle evidence projection and envelope log projection. |
| Option B - Approve Future Internal GameFlow Request API | Rejected for now | There is no proven duplication that a package-local API would remove without duplicating the host or moving projection ownership prematurely. |
| Option C - Approve Future Public GameFlow Request API | Rejected | There is no real immediate game-facing consumer. Public API now would freeze names and semantics before the framework has a stable product-level GameFlow contract. |

## Selected Option

Option A is selected because:

- Route and Activity triggers already use `FrameworkRuntimeHost` correctly;
- QA Canvas already uses `FrameworkRuntimeHost` correctly;
- F49 keeps `gameFlowEnvelope*` as host projection;
- `FrameworkRuntimeHost` is the only current owner with Route/Activity domain results, Loading diagnostics, lifecycle projections and application state refresh at the same boundary;
- `GameFlowRuntime` is intentionally internal and already owns admission/execution without exposing itself as a service;
- creating a new API now would duplicate entry points rather than remove real complexity.

## Rejected Options

### Option B - Future Internal API

Rejected for F52.

A future internal/package-local API may be reconsidered only if a real consumer or repeated duplication appears. Evidence must show that the new API removes host-specific duplication without moving `gameFlowEnvelope*` ownership by stealth and without replacing domain result types.

### Option C - Public API

Rejected.

There is no immediate external game-facing consumer. Public API would prematurely stabilize request names, target language, result shape and Inspector expectations. The current public-facing authored path remains the scene-authored triggers.

## Allowed Future Scope

A future API decision may reopen only with concrete evidence:

- multiple real internal consumers need the same request boundary outside `FrameworkRuntimeHost`;
- `FrameworkRuntimeHost` request code becomes the specific source of duplicated request orchestration;
- a product-facing consumer needs a stable public GameFlow request contract;
- the future API preserves domain result types and does not introduce a universal result/status model.

If reopened, the first safe shape is internal/package-local, not public.

## Forbidden Scope

F52 does not allow:

- public `IGameFlowService`;
- service locator or global singleton;
- request bus or command dispatcher;
- public `GameFlowRequestEnvelope`;
- movement of request entry points away from `FrameworkRuntimeHost`;
- changes to `GameFlowRuntime`;
- changes to Route/Activity triggers;
- changes to `Runtime/Common/FlowTriggers`;
- changes to `gameFlowEnvelope*`;
- new smoke buttons;
- universal `FrameworkResult`, `Result<T>` or shared status enum.

## Relationship With Request Envelope

`GameFlowRequestEnvelope` remains internal/passive diagnostics. It summarizes request-level evidence in logs and does not become a request object, public API, result carrier or service contract.

F49 remains the active ownership decision: `FrameworkRuntimeHost` builds and projects the envelope at log time.

## Relationship With Triggers

`RouteRequestTrigger` and `ActivityRequestTrigger` remain authored Unity-facing request boundaries. They own target selection, reason resolution, Activity clear semantics and local event publication.

F51's FlowTrigger helper adoption stays local to trigger diagnostics and does not imply a public GameFlow request API.

## Validation Plan

Static validation:

- `git diff --check`;
- confirm no active `FXX` placeholder file exists;
- confirm `Assets/_Documentation/Architecture` remains flat;
- confirm no runtime C# files changed by F52;
- confirm no scenes, prefabs, serialized assets, `ProjectSettings`, `package.json`, asmdefs or `.csproj` changed;
- confirm `FrameworkRuntimeHost`, `GameFlowRuntime`, Route/Activity triggers and `Runtime/Common/FlowTriggers` were not altered.

Manual Unity validation:

- Not required for F52 because it is ADR-only.

## Next Gate

`F53 - Architecture Consolidation Next-Track Decision`

F53 should leave GameFlow API work unless new consumer evidence appears, and choose the next productive architecture track.
