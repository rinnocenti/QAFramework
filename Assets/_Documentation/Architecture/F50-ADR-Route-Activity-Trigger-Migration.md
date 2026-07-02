# F50-ADR-Route-Activity-Trigger-Migration

Status: Accepted / ADR-only  
Last updated: 2026-07-02  
Track: GAMEFLOW - Lifecycle Request Envelope  
Depends on:

- `F42-ADR-FlowTrigger-Request-State-Helper.md`
- `F47-ADR-GameFlow-Request-Envelope-Boundary.md`
- `F48-ADR-GameFlow-Request-Envelope-Shell.md`
- `F49-ADR-GameFlow-Envelope-Runtime-Ownership-Trigger-Migration-Decision.md`

Next gate: `F51 - GAMEFLOW-4 - Route/Activity Trigger FlowTrigger Adoption`

## Context

F42 accepted a neutral non-MonoBehaviour FlowTrigger helper under `Runtime/Common/FlowTriggers` for local trigger bookkeeping. It was adopted by Pause trigger paths, while Route and Activity triggers were left untouched because they live under `Runtime/GameFlow`.

F49 keeps GameFlow envelope creation and `gameFlowEnvelope*` projection in `FrameworkRuntimeHost`. That stabilizes envelope ownership before deciding whether Route and Activity triggers may share the neutral helper.

F50 is decision-only. It does not change runtime, serialized assets, public API, request logs, smoke buttons, package metadata or assemblies.

## Decision

Choose Option A - Approve Future Migration.

A future GameFlow-scoped implementation cut may migrate `RouteRequestTrigger` and `ActivityRequestTrigger` to the shared FlowTrigger helper for local state and diagnostics only.

The future migration must keep Route and Activity domain ownership intact:

- authored target selection stays in each trigger;
- Activity clear semantics stay in `ActivityRequestTrigger`;
- `FrameworkRuntimeHost` remains the explicit runtime request entry point;
- domain result mapping stays local to each trigger;
- `GameFlowRuntime` request admission and execution are unchanged;
- `gameFlowEnvelope*` creation/projection stays in `FrameworkRuntimeHost`.

No runtime patch is made in F50.

## Trigger Audit Summary

| Trigger | Current duplicated local mechanics | Must remain trigger-owned |
| --- | --- | --- |
| `RouteRequestTrigger` | `_requestInFlight`, `_lastEventPhase`, `_lastOutcome`, `_lastReason`, `_lastMessage`, submitted/completed publication and local ignore/fail state. | `targetRoute`, `reason`, `DefaultSource`, `ResolveReason`, call to `FrameworkRuntimeHost.RequestRouteAsync`, `MapOutcome`. |
| `ActivityRequestTrigger` | `_requestInFlight`, `_lastEventPhase`, `_lastOutcome`, `_lastReason`, `_lastMessage`, `_lastRequestClearedActivity`, submitted/completed publication and local ignore/fail state. | `targetActivity`, `reason`, `DefaultSource`, `DefaultClearReason`, `ResolveRequestReason`, `ResolveClearReason`, calls to `FrameworkRuntimeHost.RequestActivityAsync` and `ClearActivityAsync`, `MapOutcome`, clear-vs-request diagnostics. |

The duplicated mechanics match the neutral helper's intended responsibility: submitted/completed state, succeeded/ignored/failed flags, reason/source/message and issue counts. The helper must not absorb Route or Activity policy.

## Allowed Future Migration Scope

F51 may:

- replace private local in-flight/state fields with `FrameworkFlowTriggerState` where behavior remains equivalent;
- use `FrameworkFlowTriggerSubmission` and helper diagnostics for submitted/completed state;
- keep the existing public diagnostic properties stable by reading from the helper-backed state;
- preserve local mapping from Route/Activity result kinds to `FlowRequestOutcome`;
- preserve authored source/reason resolution.

## Forbidden Migration Scope

F51 must not:

- rename or remove serialized fields;
- change `RouteRequestTrigger` target selection;
- change `ActivityRequestTrigger` request/clear semantics;
- infer identity by parsing strings;
- change `GameFlowRuntime`, Route lifecycle, Activity flow, SceneLifecycle, RuntimeContent, ContentAnchor, Loading, Transition, Pause or InputMode behavior;
- move or duplicate `gameFlowEnvelope*` ownership;
- change request ordering, admission rules, logs or smoke buttons;
- create public API or compatibility rails.

## Serialization Safety

The future implementation must preserve serialized field names and meanings:

- `RouteRequestTrigger.targetRoute`
- `RouteRequestTrigger.reason`
- `ActivityRequestTrigger.targetActivity`
- `ActivityRequestTrigger.reason`

Private non-serialized backing fields may change only if the observable properties and Unity Inspector behavior remain stable. No `FormerlySerializedAs` attribute is needed if serialized field names are not changed.

## Relationship With GameFlow Envelope

The FlowTrigger helper is local trigger bookkeeping. It is not a GameFlow request envelope.

F49 remains the active envelope ownership decision: `FrameworkRuntimeHost` builds the envelope at log projection time because it has Route/Activity domain result, lifecycle projection and Loading diagnostics evidence. Trigger migration must not create, store, move, rename or reinterpret `gameFlowEnvelope*` fields.

## Relationship With FlowTrigger Helper

The helper may be reused because Route and Activity triggers expose the same local state shape already adopted by Pause triggers: in-flight guard, submitted/completed phase, outcome flags, reason, source and message.

The helper remains internal common infrastructure. Its use does not make Route, Activity and Pause semantically identical.

## Validation Plan

Static validation for F50:

- `git diff --check`;
- confirm no runtime C# files changed;
- confirm no scenes, prefabs, serialized assets, `ProjectSettings`, `package.json`, asmdefs or `.csproj` changed;
- confirm `RouteRequestTrigger`, `ActivityRequestTrigger` and `Runtime/Common/FlowTriggers` were not altered;
- confirm no active `FXX` placeholder file exists;
- confirm `Assets/_Documentation/Architecture` remains flat.

Manual Unity validation:

- Not required for F50 because it is ADR-only.

Future F51 runtime validation should include Unity import/compile, Standard Smoke, Activity Baseline Smoke, Route Scene Composition Smoke, Route Release Smoke when present, Composite Lifecycle Release Smoke when present, Activity Content Anchor Diagnostics Smoke when present and Content Anchor Diagnostics Smoke when present.

Pause/InputMode smokes are not required for F51 unless the shared helper itself changes.

## Next Gate

`F51 - GAMEFLOW-4 - Route/Activity Trigger FlowTrigger Adoption`

F51 may implement the approved local migration for both Route and Activity triggers, provided the cut stays inside the allowed scope above.
