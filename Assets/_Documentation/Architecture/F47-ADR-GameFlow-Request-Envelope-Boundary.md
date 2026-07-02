# F47-ADR-GameFlow-Request-Envelope-Boundary

Status: Accepted / ADR-only  
Last updated: 2026-07-02  
Track: GAMEFLOW - Lifecycle Request Envelope  
Next gate: `F48 - GAMEFLOW-1 - Request Envelope Shell`

## Context

F44 created lifecycle-local operation evidence for Route and Activity request logs. F45 added content/readiness evidence projection over the same existing domain evidence. F46 accepted that the initial lifecycle evidence stabilization is closed for now and authorized opening GameFlow boundary discussion before any runtime implementation.

Current GameFlow runtime owns request admission and delegates Route/Activity execution, but it does not expose a request envelope type. `FrameworkRuntimeHost` still owns the public request entry points and request log projection. Route, ActivityFlow, SceneLifecycle, RuntimeContent, ContentAnchor, Loading and Transition keep their domain result/status ownership.

F42 accepted a shared FlowTrigger helper only for local trigger submission state/diagnostics. Route and Activity triggers remain under GameFlow and were intentionally left for a future GameFlow-scoped cut.

## Decision

Accept a future GameFlow request envelope as a boundary-local request and diagnostics object, not as a new domain result system.

The envelope may be introduced only by a future implementation cut. This ADR does not create `GameFlowRequestEnvelope`, `GameFlowOperation`, runtime C# code, scenes, prefabs, assets, package metadata, asmdefs or QA Canvas changes.

The future envelope must:

- identify the requested GameFlow operation without replacing the requested domain;
- preserve original Route/Activity/Scene/Content/Loading/Transition results and statuses;
- carry lifecycle operation, content/readiness and loading adapter evidence as projections when already available;
- expose admission, source, reason, target and diagnostics in a bounded GameFlow request shape;
- fail visibly when required request data is missing instead of fabricating defaults;
- stay internal or package-local until a stable public API need is proven.

## Boundary

The GameFlow request envelope owns the request-level view of a GameFlow operation. It does not own domain execution.

| Concern | Owner |
| --- | --- |
| Request admission, request source/reason, target summary and request-level diagnostics | Future GameFlow request envelope |
| Public request entry points and current log projection | `FrameworkRuntimeHost` until a runtime cut moves ownership |
| Route lifecycle semantics and result/status | Route lifecycle runtime |
| Activity lifecycle semantics and result/status | ActivityFlow runtime |
| Scene load/release semantics and result/status | SceneLifecycle runtime |
| Logical content identity, handles and release/materialization request language | RuntimeContent |
| Anchor declaration, binding, placement and release evidence | ContentAnchor |
| Loading surface execution and named adapter evidence | Loading runtime/adapters |
| Transition execution/effects | Transition runtime/adapters |
| Trigger local submission state/diagnostics | `Runtime/Common/FlowTriggers` helper where already adopted |

The envelope may summarize domain outcomes, but must not become a universal status enum, universal `Result<T>` or framework-wide result base type. F39 remains the governing policy: domain status stays primary, boundary-local aggregate status is allowed only when it preserves original cause and failed stage evidence.

## Allowed Future Runtime Shape

F48 may add a minimal request envelope shell if it stays inside the accepted boundary. The shell may represent:

- Route request;
- Activity request;
- Activity clear;
- cycle reset only if a concrete GameFlow operation exists in the touched implementation path;
- startup Route/Activity only if the current runtime path can report it without inventing behavior.

The shell may carry:

- operation kind;
- source and reason;
- route/activity target identity as authored/requested data, not parsed identity;
- request admission state;
- optional transition/loading policy evidence;
- lifecycle operation evidence projection;
- lifecycle content/readiness evidence projection;
- loading adapter evidence projection;
- original domain result/status reference or copied diagnostics.

The shell must be passive. It should not execute Route, Activity, Scene, Loading, Transition, RuntimeContent or ContentAnchor side effects.

## Rejected Scope

This ADR rejects the following for F47 and for the first shell unless a later ADR explicitly widens the boundary:

- implementing `GameFlowRequestEnvelope` during F47;
- adding `GameFlowOperation` as a runtime enum during F47;
- replacing Route/Activity result/status contracts;
- creating a universal `FrameworkResult`, universal `Result<T>` or shared status enum;
- moving RouteLifecycle, ActivityFlow, SceneLifecycle, RuntimeContent, ContentAnchor, Loading or Transition ownership into GameFlow;
- changing `GameFlowRuntime`, `FrameworkRuntimeHost`, triggers, scenes, prefabs, assets, package metadata, asmdefs, csproj files or QA Canvas in F47;
- migrating Route/Activity triggers before the envelope shell exists;
- declaring a content dispatch kernel, readiness kernel or full orchestration kernel ready.

## Trigger Migration

Route and Activity triggers are not migrated by this ADR. A future GameFlow-scoped cut may evaluate whether their local submission state/diagnostics should use the FlowTrigger helper after the envelope shell exists.

Even in that future cut, trigger target selection and domain request semantics must remain explicit. The helper may own local submitted/completed/ignored/failed/succeeded bookkeeping only; it must not choose Route/Activity policy or infer request identity.

## Validation Plan

F47 validation is documentation/static only:

- `git diff --check`;
- confirm only allowed documentation files changed;
- confirm runtime C# does not contain `GameFlowRequestEnvelope`, `GameFlowOperation`, `FrameworkResult` or universal `Result<T>`;
- confirm no scenes, prefabs, assets, package metadata, asmdefs, csproj files or QA Canvas files changed.

Future F48 runtime validation must include Unity import/compile and the relevant existing smokes after implementation. At minimum, use Standard Smoke and the existing Route/Activity lifecycle smokes when runtime code changes. Do not add or rename smoke buttons only to validate this ADR.

## Consequences

GameFlow now has an accepted request-envelope boundary, but no implementation. Lifecycle evidence remains the input ledger for future GameFlow work. Content dispatch kernel, readiness kernel and full orchestration kernel remain deferred unless future factual duplication or defects prove a need.
