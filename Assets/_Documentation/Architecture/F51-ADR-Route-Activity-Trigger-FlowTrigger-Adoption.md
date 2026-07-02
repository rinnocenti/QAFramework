# F51-ADR-Route-Activity-Trigger-FlowTrigger-Adoption

Status: Implemented locally / pending Unity validation  
Last updated: 2026-07-02  
Track: GAMEFLOW - Lifecycle Request Envelope  
Depends on:

- `F42-ADR-FlowTrigger-Request-State-Helper.md`
- `F49-ADR-GameFlow-Envelope-Runtime-Ownership-Trigger-Migration-Decision.md`
- `F50-ADR-Route-Activity-Trigger-Migration.md`

Next gate: `F52 - GAMEFLOW-5 - Trigger Migration Owner Validation / Public Request API Decision`

## Context

F42 created the neutral FlowTrigger helper under `Runtime/Common/FlowTriggers`. F50 approved using that helper in Route and Activity triggers for local state and diagnostics only.

F51 implements that approved local migration. It does not change target selection, Activity clear semantics, request admission, `FrameworkRuntimeHost` request calls, `GameFlowRuntime`, result mapping or `gameFlowEnvelope*` projection.

## Decision

Adopt `FrameworkFlowTriggerState` inside:

- `RouteRequestTrigger`
- `ActivityRequestTrigger`

The helper now owns last submitted/completed phase, last outcome, reason, source, message and succeeded/ignored/failed flags for these triggers.

The existing `_requestInFlight` guard remains local. The helper can represent a normal in-flight submission, but it cannot preserve the current observable behavior where a second request can be ignored while the first request remains in flight. Keeping the guard local avoids changing `IsRequestInFlight` or request ordering.

## Migration Scope

F51 migrated only private duplicated bookkeeping:

- `_lastEventPhase`
- `_lastOutcome`
- `_lastReason`
- `_lastMessage`
- submitted/completed local state publication
- succeeded/ignored/failed local state

F51 did not modify `Runtime/Common/FlowTriggers`.

## Preserved Serialized Fields

The serialized field names and meanings remain unchanged:

- `RouteRequestTrigger.targetRoute`
- `RouteRequestTrigger.reason`
- `ActivityRequestTrigger.targetActivity`
- `ActivityRequestTrigger.reason`

No `FormerlySerializedAs` attribute is required because no serialized field was renamed or removed.

## Preserved Request Semantics

F51 preserves:

- `DefaultSource` values;
- Route reason resolution from explicit Inspector reason or `RouteAsset.RouteName`;
- Activity request reason resolution from explicit Inspector reason or `ActivityAsset.ActivityName`;
- Activity clear reason resolution with fallback `Clear Activity`;
- explicit calls to `FrameworkRuntimeHost.RequestRouteAsync`;
- explicit calls to `FrameworkRuntimeHost.RequestActivityAsync`;
- explicit calls to `FrameworkRuntimeHost.ClearActivityAsync`;
- local mapping from Route/Activity result kinds to `FlowRequestOutcome`;
- existing event publication shape.

## Route Trigger Changes

Audit summary:

| Area | F51 result |
| --- | --- |
| Serialized fields | `targetRoute` and `reason` preserved. |
| Duplicated private state | Last phase/outcome/reason/message moved to `FrameworkFlowTriggerState`. |
| Public diagnostics | `IsRequestInFlight`, `LastEventPhase`, `LastOutcome`, `LastReason`, `LastMessage`, `LastRequestSucceeded`, `LastRequestIgnored` and `LastRequestFailed` preserved. |
| Request method | `RequestRoute` still resolves reason, validates target, resolves `FrameworkRuntimeHost` and calls `RequestRouteAsync`. |
| Success/failure/ignored flow | Same local messages and event publication path. |
| Result mapping | `FrameworkRouteRequestKind.Succeeded` maps to succeeded; `IgnoredAlreadyActive` and `IgnoredAlreadyInFlight` map to ignored; all other kinds map to failed. |

## Activity Trigger Changes

Audit summary:

| Area | F51 result |
| --- | --- |
| Serialized fields | `targetActivity` and `reason` preserved. |
| Duplicated private state | Last phase/outcome/reason/message moved to `FrameworkFlowTriggerState`. |
| Public diagnostics | `IsRequestInFlight`, `LastEventPhase`, `LastOutcome`, `LastReason`, `LastMessage`, `LastRequestClearedActivity`, `LastRequestSucceeded`, `LastRequestIgnored` and `LastRequestFailed` preserved. |
| Request activity flow | `RequestActivity` still resolves reason, validates target, resolves `FrameworkRuntimeHost` and calls `RequestActivityAsync`. |
| Clear activity flow | `ClearActivity` remains explicit, uses the clear reason fallback and calls `ClearActivityAsync`. |
| Request vs clear distinction | `_lastRequestClearedActivity` remains local to preserve clear-vs-request diagnostics. |
| Result mapping | `FrameworkActivityRequestKind.Succeeded` maps to succeeded; ignored result kinds map to ignored; all other kinds map to failed. |

## Rejected Scope

F51 does not:

- change `GameFlowRuntime`;
- change `FrameworkRuntimeHost` request API;
- change `FrameworkRuntimeHost` log projection;
- change `gameFlowEnvelope*`;
- change Route, Activity, SceneLifecycle, RuntimeContent, ContentAnchor, Loading, Transition, Pause or InputMode behavior;
- change scenes, prefabs, serialized assets, package metadata, asmdefs or `.csproj`;
- create public API;
- create smoke buttons;
- introduce universal result/status types.

## Validation Plan

Static validation:

- `git diff --check`;
- confirm no active `FXX` placeholder file exists;
- confirm `Assets/_Documentation/Architecture` remains flat;
- confirm no scenes, prefabs, serialized assets, `ProjectSettings`, `package.json`, asmdefs or `.csproj` changed;
- confirm serialized fields `targetRoute`, `reason`, `targetActivity` and `reason` remain present;
- confirm `GameFlowRuntime` did not change;
- confirm `FrameworkRuntimeHost` public request API did not change;
- confirm `gameFlowEnvelope*` projection did not change;
- confirm no universal `FrameworkResult`, `Result<T>` or universal status type was created.

Manual Unity validation:

1. Unity import/compile.
2. Standard Smoke.
3. Activity Baseline Smoke.
4. Route Scene Composition Smoke.
5. Route Release Smoke, if present.
6. Composite Lifecycle Release Smoke, if present.
7. Activity Content Anchor Diagnostics Smoke, if present.
8. Content Anchor Diagnostics Smoke, if present.

Pause/InputMode smokes are not required for F51 because `Runtime/Common/FlowTriggers` was not changed.

## Next Gate

`F52 - GAMEFLOW-5 - Trigger Migration Owner Validation / Public Request API Decision`

F52 should either collect owner validation for the F51 runtime migration or decide whether the next GameFlow step is a public/internal request API boundary.
