# F42-ADR-FlowTrigger-Request-State-Helper

Status: Implemented locally / pending Unity validation
Last updated: 2026-07-01
Depends on:

- `Assets/_Documentation/Architecture/F34-ADR-Architecture-Consolidation.md`
- `Assets/_Documentation/Architecture/F35-ADR-Extension-Surface-Model.md`
- `Assets/_Documentation/Architecture/F36-AUDIT-Surface-Adapter-Inventory.md`
- `Assets/_Documentation/Architecture/F39-ADR-Status-Mapping-Policy.md`
- `Assets/_Documentation/Architecture/F41-ADR-Pause-Visual-Consumer-Readiness.md`

## Context

Current authored triggers repeat local submission bookkeeping: source/reason normalization, last outcome, last message, completed/ignored/failed flags, blocking issue counts and diagnostic string assembly.

That repetition is real, but it is not permission to create a universal trigger. Route, Activity, Pause, Object Reset, Cycle Reset and InputAction triggers each own domain-specific request construction, runtime calls and result mapping.

## Decision

Accept a small non-MonoBehaviour helper under:

`Packages/com.immersive.framework/Runtime/Common/FlowTriggers/`

The helper owns only neutral mechanics:

- local submission state.
- last phase/outcome text.
- source/reason/message storage.
- issue and blocking issue count storage.
- simple diagnostic field formatting.
- local in-flight guard support.

The helper must not own:

- route selection.
- activity selection.
- pause intent policy.
- input action evidence policy.
- PlayerInput apply.
- lifecycle dispatch.
- GameFlow envelope.
- scene load/release.
- transition/loading decision.
- fallback behavior.

The helper deliberately does not depend on `GameFlow`, `Pause`, `InputMode`, `RouteLifecycle`, `ActivityFlow`, `UnityEngine` or domain result enums.

## Trigger inventory

| Trigger | Domain | Archetype | Current responsibilities | Duplicated mechanics | Domain-specific responsibilities | Safe-to-extract | Must stay local | Decision |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `Runtime/GameFlow/RouteRequestTrigger.cs` | Route/GameFlow | Bridge, Consumer | Author route request, guard in-flight, call `RequestRouteAsync`, publish `RouteRequestTriggerEvent`. | in-flight, last phase/outcome/reason/message, submitted/completed event bookkeeping. | target route selection, route request result mapping, runtime route call. | local state and diagnostics only. | route selection and `FrameworkRuntimeHost.RequestRouteAsync`. | Inspect only; not migrated because F42 forbids GameFlow runtime changes. |
| `Runtime/GameFlow/ActivityRequestTrigger.cs` | Activity/GameFlow | Bridge, Consumer | Author activity/clear request, guard in-flight, call `RequestActivityAsync`/`ClearActivityAsync`, publish event. | in-flight, last phase/outcome/reason/message, submitted/completed event bookkeeping. | target activity, clear activity semantics, activity request result mapping. | local state and diagnostics only. | activity selection/clear policy and runtime calls. | Inspect only; not migrated because F42 forbids GameFlow runtime changes. |
| `Runtime/Pause/PauseRequestTrigger.cs` | Pause | Bridge, Consumer | Author Pause/Resume/Toggle request and record last result state. | outcome/reason/message and success/ignored/failed flags. | Pause request kind, `PauseResult` mapping, previous/current Pause state. | last submission state, reason/message, issue counts. | Pause intent and `FrameworkRuntimeHost.RequestPause`. | Migrated now. |
| `Runtime/InputMode/PauseInputActionRuntimeBridgeTrigger.cs` | Pause/InputMode | Bridge, Consumer | Resolve InputAction evidence, submit to Pause/InputMode bridge, log result. | source/reason normalization, last trigger outcome diagnostics, blocking issue count projection. | action evidence policy, bridge discovery, Pause/InputMode request kind, PlayerInput bridge result mapping. | source/reason normalization, last submission projection and diagnostics formatting. | action evidence, bridge submit, InputMode/PlayerInput semantics. | Migrated now, only for neutral bookkeeping. |
| `Runtime/CycleReset/RouteCycleResetTrigger.cs` | Cycle Reset | Bridge, Consumer | Author route cycle reset request, guard in-flight, publish result event. | in-flight, last phase/outcome/reason/message/result summary. | route cycle reset request and participant result mapping. | local state and diagnostics. | reset semantics and runtime call. | Inspect only; outside F42 allowed file scope. |
| `Runtime/CycleReset/ActivityCycleResetTrigger.cs` | Cycle Reset | Bridge, Consumer | Author activity cycle reset request, guard in-flight, publish result event. | in-flight, last phase/outcome/reason/message/result summary. | activity cycle reset request and participant result mapping. | local state and diagnostics. | reset semantics and runtime call. | Inspect only; outside F42 allowed file scope. |
| `Runtime/ObjectReset/ObjectResetTrigger.cs` | Object Reset | Bridge, Consumer | Author object reset request, validate target identity, guard in-flight, publish result event. | in-flight, last phase/outcome/reason/message/result summary. | object identity resolution, snapshot lookup, object reset policy, participant result mapping. | local state and diagnostics. | object identity and request policy. | Inspect only; outside F42 allowed file scope. |
| `Runtime/Pause/UnityPauseInputActionAdapter.cs` | Retired Pause input | Removed compatibility stub | Report removed adapter and prevent active direct input usage. | small last outcome/status fields. | retired migration warning and non-use policy. | none useful now. | removed behavior. | Leave untouched. |

## Runtime implementation

Created:

- `FrameworkFlowTriggerSubmission`
- `FrameworkFlowTriggerState`
- `FrameworkFlowTriggerDiagnostics`

Migrated:

- `PauseRequestTrigger`
- `PauseInputActionRuntimeBridgeTrigger`
- `PauseInputActionRuntimeBridgeTriggerResult` diagnostics formatting

The migration preserves serialized fields and public methods. Domain request building and domain runtime calls remain local.

## Consequences

`FLOWTRIGGER-1` extracts a small local helper and proves it in two allowed Pause/Pause-InputMode trigger paths. It does not close `GAMEFLOW` or `LIFECYCLE-KERNEL-REMAINING`.

Route/Activity request triggers remain the better future migration targets, but they require an explicit GameFlow-scoped cut because their files live under `Runtime/GameFlow` and F42 forbids GameFlow runtime changes.

## Validation

Static validation is required in F42. Unity validation remains pending because runtime C# changed.

Manual validation after import/compile:

1. Unity compile/import.
2. Standard Smoke.
3. Pause InputAction Runtime Bridge Trigger Smoke.
4. Pause Runtime PlayerInput Bridge Smoke.
