# FXX-AUDIT - General Architecture Pattern Review

Status: Draft / audit-only / documentation governance
Scope: broad architecture pattern review for `Packages/com.immersive.framework`.
Last updated: 2026-06-30

This audit does not implement or authorize code changes. It uses the known Participant duplication and RuntimeContent / ContentAnchor materialization orchestration findings as pattern definitions, then maps other framework areas where the same correction style may apply.

## 1. Executive summary

All requested runtime areas exist in the current package snapshot:

```text
ActivityFlow
RouteLifecycle
GameFlow
RuntimeContent
ContentAnchor
SceneLifecycle
Loading
Transition
TransitionEffects
Pause
Snapshot
CycleReset
ObjectReset
LocalContribution
Common
```

The main findings outside the two seed cases are:

```text
1. RouteLifecycle and ActivityFlow mirror scene/content lifecycle operations and cleanup flow.
2. GameFlowRuntime is accumulating lifecycle request envelope responsibilities.
3. PauseInputModeUnityPlayerInputRuntimeBridge is a thick MonoBehaviour bridge with real side effects.
4. Loading, Transition, InputMode and Pause remap result/status enums through multiple layers.
5. Flow request triggers repeat state/outcome/event publication patterns.
6. Pause visual materialization APIs are ahead of a stable product consumer.
7. Runtime/Common is too small for the amount of repeated internal mechanics.
```

## 2. Ranked candidate backlog

| Rank | Candidate | Score | Evidence | Pattern | Risk | Suggested action | First safe cut |
|---:|---|---:|---|---|---|---|---|
| 1 | Route/Activity lifecycle operation mirror | 5 | `RouteLifecycleRuntime.StartRouteAsync`, `ActivityFlowRuntime.StartActivityCoreAsync`, `RouteSceneComposition*`, `ActivitySceneComposition*` | Route/Activity mirror classes evolving separately; manual release/load/bind/scope cleanup sequencing | Behavioral drift in scene release, scope cleanup and loading progress | Write ADR for internal lifecycle operation kernel | Audit-only sequence diagram and parity matrix |
| 2 | Pause/InputMode apply boundary | 5 | `PauseInputModeUnityPlayerInputRuntimeBridge.SubmitInternal` resolves host, snapshot, references, preflight, `RequestPause`, PlayerInput application and status remap | MonoBehaviour bridge above 250 lines doing orchestration | Logical Pause can be applied while PlayerInput application fails later | Write ADR for non-MonoBehaviour apply boundary | Failure-state table and transactional boundary proposal |
| 3 | GameFlow lifecycle request envelope | 4 | `GameFlowRuntime` owns route/activity/clear/reset in-flight flags, gate admission and transition before/after calls | Runtime coordinator above 400 lines with multiple lifecycle responsibilities | Every new lifecycle request may copy gate/transition/result wrapping | Write ADR for internal lifecycle request envelope | Document route/activity/clear/reset common sequence |
| 4 | Status mapping policy | 4 | `LoadingObservationAdapter`, `InputModeUnityPlayerInputRequestApplication`, `PauseInputModeUnityPlayerInputApplication`, `PauseInputActionRuntimeBridgeTrigger` | Result/status remapped through wrapper layers | Original failure cause can be hidden by aggregate status | Define internal status mapping policy | Mapping inventory table only |
| 5 | Flow trigger helper | 3 | `RouteRequestTrigger`, `ActivityRequestTrigger`, `PauseRequestTrigger` all keep last outcome, request state and outcome mapping | Repeated flow request state/event pattern | Inspector-facing components drift separately | Plan helper, not generic MonoBehaviour base | Non-MonoBehaviour state helper design note |
| 6 | Pause visual consumer readiness | 3 | `PauseVisualSurfaceAuthoring`, binding factory/executor and materialization executor are mostly used by docs, editor and QA canvas | Experimental public API before real consumer | API may become compatibility baggage before product UX is proven | Freeze expansion until consumer is selected | Consumer readiness decision note |
| 7 | Common internal mechanics | 3 | `Runtime/Common` only contains `FrameworkStringExtensions`; enum validation, copy helpers and issue counting repeat elsewhere | Validation/result mechanics copied manually | Small consistency bugs and onboarding noise | Add internal Common helpers only after two concrete uses | Inventory repeated helpers before implementation |

## 3. Candidate details

### Candidate 1 - Route/Activity lifecycle operation mirror

- Evidence files/classes/methods:
  - `Packages/com.immersive.framework/Runtime/RouteLifecycle/RouteLifecycleRuntime.cs`
  - `Packages/com.immersive.framework/Runtime/ActivityFlow/ActivityFlowRuntime.cs`
  - `RouteSceneCompositionPlan`, `RouteSceneCompositionResult`, `RouteSceneCompositionRuntime`
  - `ActivitySceneCompositionPlan`, `ActivitySceneCompositionResult`, `ActivitySceneCompositionRuntime`
- What repeats or over-orchestrates:
  - Route and Activity both build plans, execute scene operations, report loading progress, discover content/anchors, clean bindings and remove runtime scope roots.
- Why it matters:
  - Route and Activity are valid separate user concepts, but the algorithmic lifecycle mechanics are likely to drift if maintained in parallel.
- What must remain domain-specific:
  - Route primary scene semantics, Activity additive scene ledger, Activity transition policy and owner identity language.
- Suggested ADR:
  - `FXX-ADR-ARCH-0002 - Route/Activity Lifecycle Operation Kernel`.
- Suggested first safe cut:
  - Audit-only diagram and parity matrix; no runtime extraction.
- Non-goals:
  - No public API change, no serialized field change, no Route/Activity concept merge.

### Candidate 2 - Pause/InputMode apply boundary

- Evidence files/classes/methods:
  - `Packages/com.immersive.framework/Runtime/InputMode/PauseInputModeUnityPlayerInputRuntimeBridge.cs`
  - `SubmitInternal` coordinates host lookup, snapshot lookup, reference resolution, preflight, logical Pause request, InputMode application and status mapping.
- What repeats or over-orchestrates:
  - A scene-authored MonoBehaviour does runtime orchestration and Unity `PlayerInput` side effects.
- Why it matters:
  - The operation crosses logical state and Unity side effects; failures need an explicit boundary and diagnostics.
- What must remain domain-specific:
  - Pause to InputMode mapping, action map names, explicit `PlayerInput`, Unity Input evidence.
- Suggested ADR:
  - `FXX-ADR-ARCH-0003 - Pause/InputMode Apply Boundary`.
- Suggested first safe cut:
  - Failure-state table and operation sequence; no implementation.
- Non-goals:
  - No PlayerInputManager ownership, no player join/spawn, no service locator.

### Candidate 3 - GameFlow lifecycle request envelope

- Evidence files/classes/methods:
  - `Packages/com.immersive.framework/Runtime/GameFlow/GameFlowRuntime.cs`
  - Route, Activity, ClearActivity and CycleReset requests share in-flight flags, gate admission, transition before/after and result wrapping.
- What repeats or over-orchestrates:
  - Lifecycle envelope behavior is embedded repeatedly inside one coordinator.
- Why it matters:
  - The next lifecycle request type will likely copy the same pattern.
- What must remain domain-specific:
  - Public result types and request-specific semantics.
- Suggested ADR:
  - `FXX-ADR-ARCH-0004 - GameFlow Lifecycle Request Envelope`.
- Suggested first safe cut:
  - Document envelope states and mapping; no code.
- Non-goals:
  - No global manager, singleton or bootstrap rewrite.

### Candidate 4 - Status mapping policy

- Evidence files/classes/methods:
  - `LoadingObservationAdapter.MapAggregationStatus`
  - `LoadingObservationAdapter.MapTransitionStepStatus`
  - `LoadingObservationAdapter.MapTransitionResultToSyntheticStepStatus`
  - `InputModeUnityPlayerInputRequestApplication.Apply`
  - `PauseInputModeUnityPlayerInputApplication.Apply`
- What repeats or over-orchestrates:
  - One operation is often represented by several status enums and result wrappers.
- Why it matters:
  - Diagnostics can hide whether the original fault was rejected input, failed preview, failed adapter application or failed side effect.
- What must remain domain-specific:
  - Public status enums and user-facing diagnostic vocabulary.
- Suggested ADR:
  - `FXX-ADR-ARCH-0005 - Status Mapping Policy`.
- Suggested first safe cut:
  - Mapping inventory table.
- Non-goals:
  - No universal public status enum.

### Candidate 5 - Flow trigger helper

- Evidence files/classes/methods:
  - `RouteRequestTrigger`, `ActivityRequestTrigger`, `PauseRequestTrigger`
  - `FlowRequestOutcome`, `FlowRequestEventPhase`, route/activity trigger events.
- What repeats or over-orchestrates:
  - Request in-flight state, last outcome, submitted/completed publication and outcome mapping.
- Why it matters:
  - The same Inspector-facing state pattern evolves in separate components.
- What must remain domain-specific:
  - Serialized fields, UnityEvent bridges, target route/activity references and `ClearActivity`.
- Suggested ADR:
  - Extend `FXX-ADR-CONSOLIDATION-001` with a separate Flow Trigger helper phase.
- Suggested first safe cut:
  - Non-MonoBehaviour helper design note.
- Non-goals:
  - No generic `MonoBehaviour` base in the first cut.

### Candidate 6 - Pause visual consumer readiness

- Evidence files/classes/methods:
  - `PauseVisualSurfaceAuthoring`
  - `PauseVisualSurfaceBindingRequestFactory`
  - `PauseVisualSurfaceBindingExecutor`
  - `PauseVisualSurfaceMaterializationExecutor`
  - QA/documentation consumers in `FrameworkQaCanvas` and guides.
- What repeats or over-orchestrates:
  - Public experimental contracts exist before a stable product consumer proves the runtime path.
- Why it matters:
  - The framework may lock in API shape before actual Pause UX selects resident UI vs runtime materialization.
- What must remain domain-specific:
  - Pause visual intent, authored contract and user-facing language.
- Suggested ADR:
  - `FXX-ADR-ARCH-0006 - Pause Visual Consumer Readiness`.
- Suggested first safe cut:
  - Decision note; freeze expansion.
- Non-goals:
  - No removal and no materialization rewrite.

### Candidate 7 - Common internal mechanics

- Evidence files/classes/methods:
  - `Runtime/Common/FrameworkStringExtensions.cs`
  - Repeated `Enum.IsDefined`, `Unknown` rejection, `CopyIssues`, `IssueCount`, `BlockingIssueCount`, `Array.Empty` patterns in Loading, Pause, InputMode, LocalContribution and participant domains.
- What repeats or over-orchestrates:
  - Basic result/validation mechanics are copied instead of shared internally.
- Why it matters:
  - Small behavior inconsistencies appear when every domain owns the same mechanics.
- What must remain domain-specific:
  - Public status enums, public result types, typed identities and domain wording.
- Suggested ADR:
  - Covered by `FXX-ADR-CONSOLIDATION-001` and this governance roadmap.
- Suggested first safe cut:
  - Inventory exact repeated helpers and select two concrete call sites.
- Non-goals:
  - No public Common API and no domain semantics in Common.

## 4. Rejected candidates

| Candidate | Reason |
|---|---|
| CycleReset/ObjectReset participant pilot | Already governed by `FXX-ADR-CONSOLIDATION-001`; not a new finding here. |
| Snapshot participant migration | Already deferred by ADR 001 until pilot closeout. |
| RuntimeContent/ContentAnchor materialization seed | Already governed by `FXX-ADR-CONSOLIDATION-002`; not reconfirmed as the main result here. |
| `UnityFadeCurtainEffectAdapter` | Large but currently narrow: concrete CanvasGroup adapter. Defer unless a second effect adapter copies the same mechanics. |
| `UnityLoadingSurfaceAdapter` | Similar UI adapter shape, but still domain-specific enough. Consider only private shared UI helpers later. |
| Frozen technical packages | No changes recommended to `com.immersive.foundation`, `com.immersive.logging` or `com.immersive.pooling`. |

## 5. Recommended next steps

```text
1. Create an Architecture Consolidation Governance ADR.
2. Create a roadmap grouping the candidates into ordered tracks.
3. Keep every future implementation internal, additive and smoke-parity driven.
4. Do not select gameplay/F34, adapters or public API changes from this audit.
5. Start with Common mechanics and the already-scoped Participant pilot only after explicit approval.
```

