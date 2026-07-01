# FXX-PLAN - Architecture Consolidation Roadmap

Status: Proposed / docs-only roadmap
Scope: planning for the Architecture Consolidation Track.
Last updated: 2026-06-30

This roadmap does not implement code. It organizes accepted and candidate consolidation work into tracks. Every future implementation must be internal, additive and smoke-parity driven.

## 1. Sources

```text
Assets/_Documentation/Audits/FXX-AUDIT-General-Architecture-Pattern-Review.md
Packages/com.immersive.framework/Documentation~/ADRs/FXX-ADR-CONSOLIDATION-001-Participant-And-Flow-Pattern-Consolidation.md
Assets/_Documentation/Plans/FXX-PLAN-Participant-And-Flow-Pattern-Consolidation.md
Assets/_Documentation/ADRs/FXX-ADR-CONSOLIDATION-002-RuntimeContent-ContentAnchor-Materialization-Orchestration.md
Assets/_Documentation/Plans/FXX-PLAN-RuntimeContent-ContentAnchor-Materialization-Orchestration.md
```

## 2. Track order

| Order | Track | Current status | Reason for order |
|---:|---|---|---|
| 1 | Common internal mechanics | Candidate foundation | Enables small shared mechanics without domain migration. |
| 2 | Participant consolidation | Planned pilot | Already has ADR/plan; pilots with `CycleReset` and `ObjectReset`. |
| 3 | Route/Activity lifecycle operation kernel | Closed | Highest-scoring non-seed lifecycle mirror. |
| 4 | RuntimeContent/ContentAnchor materialization service | In progress / MAT-2 | Already has ADR/plan; real side effects require small cuts. |
| 5 | Pause/InputMode apply boundary | Audit/ADR needed | Thick bridge with logical + Unity side effects. |
| 6 | GameFlow lifecycle request envelope | Audit/ADR needed | Coordinator pressure; should follow lifecycle kernel analysis. |
| 7 | Status mapping policy | Audit/table needed | Cross-cutting diagnostic policy after concrete tracks expose mappings. |
| 8 | Flow trigger helper | Deferred helper | Lower risk if kept non-MonoBehaviour; follows participant closeout. |
| 9 | Pause visual consumer readiness | Decision note | Prevents expansion before a real consumer is selected. |

## 3. Global rules for all tracks

```text
No runtime implementation in this roadmap cut.
No asmdef changes.
No scene or prefab edits.
No public abstraction creation.
No serialized field rename.
No gameplay/F34 selection.
No service locator, singleton or reflection.
No hidden fallback.
Implementation, when later approved, must be internal, additive and smoke-parity driven.
```

## 4. Track 1 - Common internal mechanics

### Objective

Create a bounded internal home for repeated framework mechanics without moving domain semantics into `Common`.

### Proposed cuts

| Cut | Name | Type |
|---|---|---|
| COMMON-A | Repetition inventory | Documentation |
| COMMON-B | Enum/status validation helpers | Internal additive implementation |
| COMMON-C | Defensive copy and issue counting helpers | Internal additive implementation |
| COMMON-D | Operation result container pilot | Internal additive implementation |
| COMMON-E | Closeout | Documentation |

### Included scope

```text
Internal helpers for defined-not-Unknown enum validation.
Internal helpers for defensive array/list copy when shapes are identical.
Internal helpers for issue/blocking issue counting when shapes are identical.
Optional internal OperationResult<TStatus> only if two concrete call sites use it.
Synthetic Common smokes.
```

### Excluded scope

```text
No public Common API.
No domain status replacement.
No domain identity replacement.
No lifecycle ownership in Common.
No service locator or registry.
No behavior change.
```

### Risks

```text
Common can become a junk drawer if it accepts domain semantics.
Generic helpers can reduce readability if introduced before concrete users.
```

### Acceptance criteria

```text
Each helper has at least two concrete call sites.
No public API changes.
No domain result/status enum is replaced.
Synthetic smoke covers helper behavior.
Existing domain smokes remain unchanged.
```

### Affected smokes

```text
New Common helper smoke.
Existing affected module smokes only when a module adopts a helper.
```

## 5. Track 2 - Participant consolidation

### Objective

Reduce duplicated participant execution mechanics by piloting `CycleReset` and `ObjectReset` against internal Common participant primitives.

### Proposed cuts

| Cut | Name | Type |
|---|---|---|
| CONS-A | Common Results & Validation Primitives | Internal additive implementation |
| CONS-B | Common Participant Executor | Internal additive implementation |
| CONS-C | CycleReset Pilot Migration | Internal refactor |
| CONS-D | ObjectReset Pilot Migration | Internal refactor |
| CONS-E0 | Flow Request Trigger Consolidation Risk Note | Documentation |
| CONS-F | Closeout / Decision Point | Documentation |

### Included scope

```text
Internal participant descriptor/entry/executor mechanics.
Internal requiredness normalization with explicit domain mappers.
CycleReset internal composition.
ObjectReset internal composition.
Synthetic CommonParticipantExecutor smoke.
```

### Excluded scope

```text
No Snapshot migration in the pilot.
No ActivityContentExecution migration in the pilot.
No LocalContribution migration as Participant.
No public enum replacement.
No public API signature change.
No MonoBehaviour trigger base class.
No smoke text rewrite.
```

### Risks

```text
Over-generic executor may hide domain semantics.
ObjectReset bug-fix for invalid participant result may create intentional behavior difference.
Snapshot and ActivityContentExecution may be migrated too early if closeout is skipped.
```

### Acceptance criteria

```text
CycleReset smoke output remains equivalent.
ObjectReset smoke output remains equivalent except explicitly approved invalid-result bug fix.
CommonParticipantExecutor smoke covers success, optional failure, required failure, exception and invalid result.
Public typed identities and public requiredness enums remain unchanged.
```

### Affected smokes

```text
CommonParticipantExecutorSmokeRunner
CycleResetQaSmokeRunner
ObjectResetQaSmokeRunner
RouteCycleResetTrigger smoke
ActivityCycleResetTrigger smoke
```

## 6. Track 3 - Route/Activity lifecycle operation kernel

### Objective

Reduce drift between Route and Activity scene/content lifecycle operations without merging the public concepts.

### Proposed cuts

| Cut | Name | Type |
|---|---|---|
| LIFECYCLE-A | Route/Activity sequence audit | Documentation |
| LIFECYCLE-B | Kernel ADR | Documentation |
| LIFECYCLE-C | Internal operation model shell | Internal additive implementation |
| LIFECYCLE-D | Route composition pilot | Internal refactor |
| LIFECYCLE-E | Activity composition pilot | Internal refactor |
| LIFECYCLE-F | Closeout | Documentation |

### Included scope

```text
Internal lifecycle operation vocabulary.
Shared sequencing for load/release/progress/scope cleanup where proven identical.
Route and Activity domain mappers.
Parity matrix for Route and Activity lifecycle outputs.
```

### Excluded scope

```text
No Route/Activity concept merge.
No serialized field rename.
No public API change.
No auto-materialization.
No auto-release policy change.
No scene asset change.
```

### Risks

```text
Route primary scene and Activity additive scene ledger are similar but not identical.
Loading progress diagnostics may drift if ranges are refactored.
Scene side effects are high-risk without Unity validation.
```

### Acceptance criteria

```text
Route lifecycle smoke output remains equivalent.
Activity baseline and Activity scene composition outputs remain equivalent.
Loading progress diagnostics remain equivalent.
RuntimeContent scope and ContentAnchor binding cleanup counts remain equivalent.
```

### Affected smokes

```text
Standard Smoke
Activity Baseline Smoke
Route Scene Composition Smoke
Route Release Smoke
Activity Content Anchor diagnostics
Local Contribution smoke when Activity discovery is touched
```

## 7. Track 4 - RuntimeContent/ContentAnchor materialization service

### Objective

Move prefab-at-anchor orchestration behind a reusable non-MonoBehaviour service while preserving existing bridge behavior.

### Proposed cuts

| Cut | Name | Type |
|---|---|---|
| MAT-A | Materialization Flow Audit Closeout | Documentation |
| MAT-B | Service Shell + Request/Stage Result | Internal additive implementation |
| MAT-C | Service Delegates to Existing Pipeline | Internal refactor |
| MAT-D | Bridge Thin Wrapper Migration | Internal refactor |
| MAT-E | Stage-Oriented Result Consolidation | Internal refactor |
| MAT-F | RuntimeContentRuntime Split Plan | Documentation |
| MAT-G | Closeout / Next Decision | Documentation |

### Included scope

```text
ContentAnchorMaterializationService.
Stage-oriented aggregate result.
Bridge delegates to service while preserving serialized fields.
Existing pipeline may remain internal during migration.
Rollback diagnostics remain explicit.
```

### Excluded scope

```text
No pooling integration.
No actor materialization.
No Pause/Loading/Transition consumer integration.
No public Inspector field changes.
No broad RuntimeContentRuntime split in the first service cut.
No generic Saga/CompensatingStepRunner without a second use case.
```

### Risks

```text
Real Unity side effects make parity fragile.
Bridge, pipeline and registry diagnostics may change unintentionally.
Temporary code count may increase while service and old pipeline coexist.
```

### Acceptance criteria

```text
Existing materialization smoke output remains equivalent.
Non-MonoBehaviour caller can invoke the service path.
Bridge remains authoring/diagnostics only.
Failed stage and original subsystem result are visible.
No public serialized field changes.
```

### Affected smokes

```text
Runtime Prefab Materialization Smoke
Content Anchor Materialization Pipeline Smoke
Content Anchor Materialization Bridge Smoke
Content Anchor Materialization Bridge Set Smoke
Bridge Set Rollback Smoke
Composite Lifecycle Release Smoke
```

## 8. Track 5 - Pause/InputMode apply boundary

### Objective

Separate the logical Pause request and Unity `PlayerInput` side-effect application into an explicit internal boundary that can be reasoned about and validated.

### Proposed cuts

| Cut | Name | Type |
|---|---|---|
| INPUT-APPLY-A | Failure-state audit | Documentation |
| INPUT-APPLY-B | Apply boundary ADR | Documentation |
| INPUT-APPLY-C | Internal apply service shell | Internal additive implementation |
| INPUT-APPLY-D | Bridge delegates to apply boundary | Internal refactor |
| INPUT-APPLY-E | Closeout | Documentation |

### Included scope

```text
Preflight/apply/report sequence definition.
Explicit failure-state table.
Internal non-MonoBehaviour apply service.
Bridge remains Inspector-facing wrapper.
Preserve current explicit PlayerInput evidence requirements.
```

### Excluded scope

```text
No PlayerInputManager ownership.
No player join.
No actor spawn.
No gameplay command reading.
No action map default rename.
No serialized field rename.
No automatic global wiring.
```

### Risks

```text
Logical Pause state and Unity PlayerInput state can diverge if sequencing changes.
Status remapping currently carries QA diagnostics that must remain readable.
```

### Acceptance criteria

```text
Pause runtime request smoke remains equivalent.
InputMode PlayerInput application smoke remains equivalent.
PauseInputMode runtime bridge smoke remains equivalent.
No new global input ownership is introduced.
```

### Affected smokes

```text
PauseRuntimeRequestQaSmokeRunner
InputModeUnityPlayerInputApplicationQaSmokeRunner
InputModeUnityPlayerInputRequestApplicationQaSmokeRunner
PauseInputModeUnityPlayerInputRuntimeBridgeQaSmokeRunner
PauseInputActionRuntimeBridgeTriggerQaSmokeRunner
```

## 9. Track 6 - GameFlow lifecycle request envelope

### Objective

Extract the repeated request envelope mechanics around gate admission, in-flight state, transition before/after and result wrapping from `GameFlowRuntime`.

### Proposed cuts

| Cut | Name | Type |
|---|---|---|
| GAMEFLOW-A | Envelope sequence audit | Documentation |
| GAMEFLOW-B | Envelope ADR | Documentation |
| GAMEFLOW-C | Internal envelope model | Internal additive implementation |
| GAMEFLOW-D | Route request pilot | Internal refactor |
| GAMEFLOW-E | Activity/Clear request pilot | Internal refactor |
| GAMEFLOW-F | CycleReset request review | Documentation or internal refactor |
| GAMEFLOW-G | Closeout | Documentation |

### Included scope

```text
Internal request envelope state.
Gate admission wrapper.
Transition before/after wrapper.
In-flight state normalization.
Request-specific result adapters.
```

### Excluded scope

```text
No new GameFlow public API.
No lifecycle bootstrap rewrite.
No global manager.
No route/activity behavior change.
No transition policy change.
```

### Risks

```text
GameFlow is a central runtime path; small diagnostic drift can hide behavior changes.
Route and Activity request kinds have similar envelopes but different result types.
```

### Acceptance criteria

```text
Route request, Activity request, ClearActivity and CycleReset behaviors remain equivalent.
Transition diagnostics remain equivalent.
Gate blocked request behavior remains equivalent.
```

### Affected smokes

```text
TransitionQaSmokeRunner
TransitionOrchestrationObservationQaSmokeRunner
GateAdmissionQaSmokeRunner
Route request trigger smoke
Activity request trigger smoke
CycleResetQaSmokeRunner
```

## 10. Track 7 - Status mapping policy

### Objective

Make cross-layer status mapping explicit so aggregate results preserve original failure causes.

### Proposed cuts

| Cut | Name | Type |
|---|---|---|
| STATUS-A | Mapping inventory | Documentation |
| STATUS-B | Mapping policy ADR | Documentation |
| STATUS-C | Internal mapping helpers for one module | Internal additive implementation |
| STATUS-D | Second module adoption | Internal refactor |
| STATUS-E | Closeout | Documentation |

### Included scope

```text
Mapping tables for Loading/Transition/InputMode/Pause.
Policy for failed stage + original result preservation.
Internal helper only after two modules prove identical mechanics.
```

### Excluded scope

```text
No universal public status enum.
No public result replacement.
No status value renumbering.
No smoke text change just for naming cleanup.
```

### Risks

```text
Over-standardizing status can erase useful domain vocabulary.
Leaving mappings undocumented continues diagnostic drift.
```

### Acceptance criteria

```text
Every aggregate status maps to a documented source status.
Original subsystem result remains available where failure is aggregated.
Existing diagnostics remain equivalent unless a documented bug fix is accepted.
```

### Affected smokes

```text
LoadingResultQaSmokeRunner
LoadingProgressQaSmokeRunner
LoadingObservationQaSmokeRunner
TransitionQaSmokeRunner
InputModeContractQaSmokeRunner
PauseInputModeUnityPlayerInputRuntimeBridgeQaSmokeRunner
```

## 11. Track 8 - Flow trigger helper

### Objective

Reduce duplicated request state/outcome/event mechanics in flow triggers without changing Unity serialization.

### Proposed cuts

| Cut | Name | Type |
|---|---|---|
| FLOWTRIGGER-A | Serialization risk note | Documentation |
| FLOWTRIGGER-B | Internal helper ADR | Documentation |
| FLOWTRIGGER-C | Non-MonoBehaviour state helper | Internal additive implementation |
| FLOWTRIGGER-D | Route trigger pilot | Internal refactor |
| FLOWTRIGGER-E | Activity trigger pilot | Internal refactor |
| FLOWTRIGGER-F | Pause trigger review | Documentation or internal refactor |
| FLOWTRIGGER-G | Closeout | Documentation |

### Included scope

```text
Internal state/outcome helper.
Internal event publication helper if it does not touch serialized fields.
Route and Activity trigger pilots after serialization review.
```

### Excluded scope

```text
No generic MonoBehaviour base in first implementation.
No serialized field move.
No UnityEvent bridge change.
No Inspector field order change.
No prefab/scene migration.
```

### Risks

```text
Unity serialization can break if fields move into a base class.
Activity has ClearActivity semantics that Route does not have.
```

### Acceptance criteria

```text
Serialized field layout remains unchanged.
Route and Activity trigger smokes remain equivalent.
No prefab or scene needs reserialization.
```

### Affected smokes

```text
RouteRequestTrigger smoke
ActivityRequestTrigger smoke
PauseRequestTrigger smoke if adopted later
UnityEvent bridge smokes if present
```

## 12. Track 9 - Pause visual consumer readiness

### Objective

Prevent further expansion of Pause visual materialization APIs until the real consumer path is selected.

### Proposed cuts

| Cut | Name | Type |
|---|---|---|
| PAUSEVIS-A | Consumer inventory | Documentation |
| PAUSEVIS-B | Readiness ADR | Documentation |
| PAUSEVIS-C | Decision: resident UI vs materialized UI | Documentation |
| PAUSEVIS-D | Closeout | Documentation |

### Included scope

```text
Inventory current Pause visual contracts and consumers.
Clarify which APIs are proof-only, experimental or consumer-ready.
Decision criteria for resident UIGlobal surface vs RuntimeContent/ContentAnchor materialization.
```

### Excluded scope

```text
No removal of existing proof APIs.
No new Pause visual runtime.
No automatic materialization.
No InputMode or Time.timeScale behavior change.
No gameplay/F34 selection.
```

### Risks

```text
Public experimental API may become compatibility baggage.
Materialized Pause UI may be selected before UX proves it is the right default.
```

### Acceptance criteria

```text
Readiness decision documents current consumers.
No new runtime behavior.
Future consumer implementation must state why resident or materialized path is selected.
```

### Affected smokes

```text
PauseVisualSurfaceAuthoring smoke
Pause ContentAnchor Binding Request smoke
Pause ContentAnchor Binding Execution smoke
Pause Visual Materialization smoke
Pause Resident Surface smoke
Pause Logical Toggle Resident Surface smoke
```

## 13. Roadmap decision gates

| Gate | Decision |
|---|---|
| After COMMON-A | Which helpers have at least two concrete identical uses? |
| After CONS-F | Continue Participant migration to Snapshot/ActivityContentExecution or stop? |
| After LIFECYCLE-B | Is a shared kernel justified, or are Route/Activity differences too large? |
| After MAT-D | Does the old pipeline remain useful behind the service? |
| After INPUT-APPLY-B | Is a transactional apply boundary required before refactor? |
| After GAMEFLOW-B | Should GameFlow wait for lifecycle kernel extraction first? |
| After STATUS-A | Which mappings are policy and which are domain-specific? |
| After FLOWTRIGGER-A | Is helper extraction safe without serialized field movement? |
| After PAUSEVIS-C | Is Pause visual default resident, materialized, or still undecided? |

## 14. Manual validation policy

This roadmap is documentation-only, so no Unity compile/import/smoke was run for this cut.

Any future implementation must request or obtain:

```text
Unity import/compile validation.
Affected QA smoke output.
Before/after diagnostic comparison for affected smokes.
Manual Inspector check when a MonoBehaviour wrapper is touched, even if fields are preserved.
```

Do not claim PASS from static checks alone.
