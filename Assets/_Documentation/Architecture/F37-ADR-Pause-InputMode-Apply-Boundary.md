# F37-ADR-Pause-InputMode-Apply-Boundary

Status: Accepted / Contract only
Last updated: 2026-07-01
Supersedes: none
Superseded by: none

## Context

F35 accepted the Extension Surface Model. F36 classified `PauseInputModeUnityPlayerInputRuntimeBridge` and `PauseInputActionRuntimeBridgeTrigger` as blocked for broad adapter readiness because the current Unity bridge path concentrates runtime host lookup, Pause snapshot handling, preflight, Pause request submission, PlayerInput application and diagnostics in MonoBehaviour code.

The next runtime cut must not start by moving code mechanically. It first needs an explicit Pause/InputMode apply boundary contract.

## Decision

Accept a contract-first boundary for Pause/InputMode apply.

The boundary separates:

- Bridge: Unity authoring wrapper.
- Trigger: Unity InputAction entry point.
- Operation Service: non-MonoBehaviour apply sequence owner.
- Adapter: explicit Unity `PlayerInput` side-effect executor.
- Validator/Evidence: preflight, issue and original-cause evidence.
- Consumer: Pause runtime or bridge path that requests input mode application.
- QA Smoke Runner: validation evidence only.

This ADR does not implement `PauseInputModeApplyService`, `InputModeApplyService`, `UnityPlayerInputModeApplyAdapter` or any renamed runtime type. It accepts the contract that `INPUT-APPLY-2` must implement or map onto existing experimental types.

## Roles

| Role | Current evidence | Accepted future responsibility | Must not own |
| --- | --- | --- | --- |
| Bridge | `PauseInputModeUnityPlayerInputRuntimeBridge` | Thin authoring wrapper: read serialized fields, validate local authoring, build/delegate an explicit request, expose last result and diagnostics. | Multi-stage orchestration, service discovery policy, Pause state ownership, PlayerInput side effects, fallback behavior. |
| Trigger | `PauseInputActionRuntimeBridgeTrigger` | Receive InputAction evidence and submit intent to the bridge/boundary. It remains a future `FLOWTRIGGER` candidate. | PlayerInput application, Pause/InputMode policy, action-map switching, flow helper extraction. |
| Operation Service | Future `PauseInputModeApplyService` or `InputModeApplyService` | Orchestrate preflight, applicability decision, Pause result/InputMode request mapping, adapter execution, failed stage, result aggregation and diagnostics. | Inspector reads, implicit global lookup, logical Pause mutation by itself, Unity object ownership. |
| Adapter | Future `UnityPlayerInputModeApplyAdapter`, or a clarified wrapper over `InputModeUnityPlayerInputAdapter` | Apply one local Unity `PlayerInput` side effect and return specific evidence. | Lifecycle decisions, Pause state decisions, request mapping, service lookup, silent fallback. |
| Validator/Evidence | Existing InputMode request/preview/plan/result/evidence types | Validate `PlayerInput`, actions, action maps, target map, current map, Pause snapshot and capability availability while preserving original causes. | Side effects or universal status replacement. |
| Consumer | Pause runtime/bridge path | Request application and handle failure, skipped/no-op or unavailable capability explicitly. | Fabricating missing references or hiding required capability absence. |
| QA Smoke Runner | Current InputMode/Pause bridge smokes | Validate the contract after implementation and preserve diagnostics expectations. | Product runtime policy or proof of product consumer readiness. |

## Expected request shape

The future request must be explicit and should include:

- Pause snapshot or InputMode intent.
- Requested mode or requested action map.
- Current `PlayerInput` state/evidence.
- Action map binding evidence.
- Target/capability evidence required by the current InputMode path.
- Source and reason diagnostics.
- Optional/no-op policy, if the caller intentionally allows absence.

The request must not infer missing identity from strings beyond existing typed identity/value objects, must not locate services globally inside the operation service, and must not read gameplay commands.

## Expected flow

1. Bridge reads serialized references and local authoring values.
2. Bridge validates local authoring enough to build a request or return a local configuration failure.
3. Bridge delegates to the non-MonoBehaviour Operation Service.
4. Operation Service resolves explicit runtime inputs passed by the bridge/consumer.
5. Operation Service performs preflight and applicability checks.
6. Operation Service maps Pause result or intent to InputMode request without changing logical Pause state by itself.
7. Operation Service calls the Unity `PlayerInput` apply adapter only after successful preflight.
8. Adapter applies the local side effect or returns local failure evidence.
9. Operation Service aggregates result, failed stage, original evidence and diagnostics.
10. Bridge stores/exposes the result and publishes diagnostics when configured.

No step may use silent fallback for required configuration.

## Conceptual result contract

The implementation should expose a result with at least:

- `status`
- `requestedMode` or `requestedActionMap`
- `appliedActionMap`
- `previousActionMap`
- `failedStage`
- `adapterResult`
- `blockingIssues`
- `source`
- `reason`

The result may preserve existing domain result objects such as Pause, InputMode preview, InputMode plan and PlayerInput adapter results. This ADR does not create a universal enum and does not replace existing domain results.

## Conceptual failure stages

Accepted conceptual stages:

- `None`
- `MissingRuntimeHost`
- `MissingPauseRuntime`
- `MissingPlayerInput`
- `MissingActionMap`
- `InvalidRequest`
- `PreflightRejected`
- `AdapterApplyFailed`
- `DiagnosticsFailed`, only if diagnostics publication is part of the implemented boundary

These stages are boundary-local. `STATUS-1` still owns cross-boundary mapping policy.

## Absence and no-op behavior

- If Pause/InputMode apply is mandatory for the requested flow, absence must fail visibly with diagnostics.
- If the caller explicitly configures optional/no-op behavior, the result must be explicit skipped/no-op evidence.
- Missing action maps must not be silently replaced by another map.
- The boundary must never call `PlayerInputManager.JoinPlayer`.
- The boundary must never create or spawn a player.
- The boundary must never read gameplay commands.
- The boundary must never invent a framework input manager.

## Current implementation coverage

Existing evidence:

- `PauseInputModeUnityPlayerInputRuntimeBridge` already exposes serialized authoring fields, last result and diagnostics, but currently owns too much operation sequence.
- `PauseInputActionRuntimeBridgeTrigger` receives InputAction evidence and delegates to the bridge; it remains a `FLOWTRIGGER` candidate.
- `InputModeRequestEvaluator`, `InputModeUnityApplicationPreviewEvaluator`, `InputModeUnityActionMapPreviewEvaluator` and `InputModeUnityApplicationPlanEvaluator` provide useful preflight/evidence pieces.
- `InputModeUnityPlayerInputAdapter` is already close to the accepted adapter role because it applies local `PlayerInput` side effects and reports local result evidence.
- Current QA smokes assert that PlayerInput apply paths do not join players or spawn actors.

Coverage is contract evidence only. The current runtime path is not accepted as ready until `INPUT-APPLY-2` extracts or maps the boundary.

## Criteria for INPUT-APPLY-2

The next runtime cut may start only if it preserves these answers:

- Operation Service: a non-MonoBehaviour service owns preflight, apply sequence, failed stage and aggregate result.
- Adapter: a Unity `PlayerInput` adapter owns only local `PlayerInput` side effects and adapter evidence.
- Bridge: `PauseInputModeUnityPlayerInputRuntimeBridge` remains an authoring wrapper and preserves existing serialized fields unless a later cut explicitly approves migration.
- Trigger: `PauseInputActionRuntimeBridgeTrigger` remains a trigger/consumer and does not become a flow helper.
- Diagnostics: current observable fields such as status, Pause status, requested mode, operation, action-map switching, input behavior, player join and actor spawning remain observable.
- Smokes: Pause Runtime PlayerInput Bridge, Pause InputAction Bridge Trigger, InputMode Unity PlayerInput Adapter, InputMode Unity Application Plan and related negative cases must remain valid after runtime implementation.
- Negative failures: missing runtime host, missing Pause runtime/snapshot, missing PlayerInput, missing action asset/map, failed preflight and adapter failure must continue blocking when mandatory.

## Rejected scope

- No runtime implementation in this cut.
- No class rename in this cut.
- No new service or adapter file in this cut.
- No `PlayerInput` or action map modification.
- No scene, prefab, serialized field, QA Canvas or smoke runner change.
- No `FLOWTRIGGER`, `STATUS-1`, `SURFACE-PILOT-1` or `PAUSEVIS-1` closure.
- No direct Pause input through the retired adapter path.

## Consequences

`INPUT-APPLY-1` is accepted as an architectural contract, not as implementation readiness. Adapter readiness and Surface readiness remain partial. The next technical gate is `INPUT-APPLY-2 - Runtime Boundary Extraction`.

## Pending decisions

- Whether the runtime implementation names the service `PauseInputModeApplyService` or `InputModeApplyService`.
- Whether the adapter is a renamed/new `UnityPlayerInputModeApplyAdapter` or a clarified wrapper over `InputModeUnityPlayerInputAdapter`.
- Which exact result type carries the conceptual `failedStage` without replacing existing domain result objects.
