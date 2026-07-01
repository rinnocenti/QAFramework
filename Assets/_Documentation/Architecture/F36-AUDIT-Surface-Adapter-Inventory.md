# F36-AUDIT-Surface-Adapter-Inventory

Status: Accepted / Doc-static review
Last updated: 2026-07-01
Depends on: `F35-ADR-Extension-Surface-Model.md`
Tracker: `F34-TRACK-Architecture-Consolidation.md`

This audit inventories current Surface, Adapter, Bridge, Operation Service, Consumer, Validator/Evidence and QA Smoke Runner candidates. It does not approve broad adapter or Surface expansion.

## Direct answers

1. Is the Framework ready to receive adapters?
   Partially. There are real adapter candidates in Loading, TransitionEffects, Pause, RuntimeContent and ContentAnchor, but broad adapter expansion is blocked by Pause/InputMode apply ownership, failure/status mapping, and consumer readiness.

2. Is the Framework ready to receive a Surface layer?
   Not broadly. Existing runtime surfaces can be classified and one bounded pilot can be selected later, but there is no accepted general Surface contract beyond the F35 archetype model.

3. Which current modules are candidates?
   Loading, TransitionEffects, Pause surface, RuntimeContent/ContentAnchor materialization, GlobalUi, Participant primitives, CycleReset/ObjectReset, ActivityFlow/RouteLifecycle request paths, Flow triggers and QA Canvas/smoke runners are candidates with different readiness levels.

4. What still blocks readiness?
   `PauseInputModeUnityPlayerInputRuntimeBridge` is still a thick Bridge/apply path; status aggregation still risks losing subsystem evidence; Pause visual materialization is experimental; Route/Activity lifecycle request paths are still partial; QA runners are evidence only, not product consumers.

5. What is the minimal order to reach the ideal point?
   Keep the gate order: `INPUT-APPLY-1`, `STATUS-1`, `SURFACE-PILOT-1`, `PAUSEVIS-1`. The inventory makes `INPUT-APPLY-1` the next recommended gate because it is the clearest active blocker before a Surface pilot.

## Inventory method

Sources reviewed:

- `Assets/_Documentation/Architecture/F34-ADR-Architecture-Consolidation.md`
- `Assets/_Documentation/Architecture/F34-PLAN-Architecture-Consolidation.v1.md`
- `Assets/_Documentation/Architecture/F34-TRACK-Architecture-Consolidation.md`
- `Assets/_Documentation/Architecture/F35-ADR-Extension-Surface-Model.md`
- `Packages/com.immersive.framework/Documentation~/Architecture.md`
- `Packages/com.immersive.framework/Documentation~/Runtime-Surfaces.md`
- Current static runtime inventory under `Packages/com.immersive.framework/Runtime`
- Historical source material under old audit/plan/ADR locations, as non-active evidence only

Validation boundary:

- Documentation and static review only.
- No Unity compile, import, playmode, smoke, scene, prefab, asmdef, runtime or serialized asset change.

## Main inventory

| Area | Concrete type/file or doc reference | Archetype | Readiness | Consumer status | Evidence | Risk | Next action |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Loading surface contract | `Runtime/Loading/LoadingSurfaceContracts.cs` | Surface, Adapter, Validator/Evidence | Candidate | Framework internal consumer | Defines request/result/status and adapter contracts without scene or lifecycle ownership. | Status vocabulary can become a generic mapping sink if reused broadly. | Keep as strongest `SURFACE-PILOT-1` candidate after prior gates. |
| Loading surface runtime | `Runtime/Loading/LoadingSurfaceRuntime.cs` | Surface, Operation Service | Partial | Framework internal consumer | Executes show/update/hide across explicit adapters and allows no visible surface. | Aggregation must preserve adapter evidence. | Recheck during `STATUS-1`; keep pilot candidate. |
| Loading Unity adapter | `Runtime/Loading/UnityLoadingSurfaceAdapter.cs` | Adapter | Candidate | Framework internal consumer | MonoBehaviour adapter mutates local CanvasGroup/GameObject/progress UI only. | Serialized UX could drift if promoted too quickly. | Candidate for bounded Surface Adapter pilot. |
| Loading observation | `Runtime/Loading/LoadingObservationAdapter.cs` | Validator/Evidence, Consumer | Partial | Framework internal consumer | Maps SceneLifecycle/Transition observations into Loading progress/status. | Status laundering risk through synthetic step mapping. | Move mapping policy to `STATUS-1`. |
| Transition surface | `Runtime/Transition/TransitionEffectOrchestrator.cs` | Surface, Operation Service | Partial | Framework internal consumer | Orchestrates transition phase effect requests and adapter execution. | Needs clear failure preservation before becoming general Surface policy. | Keep as secondary pilot candidate after `STATUS-1`. |
| Transition effect contracts | `Runtime/TransitionEffects/*TransitionEffect*` | Adapter, Validator/Evidence | Candidate | Framework internal consumer | Effect request/result/policy shape exists and remains visual-effect scoped. | Policy can become broad orchestration if expanded outside Transition. | Keep scoped to Transition. |
| Fade curtain adapter | `Runtime/TransitionEffects/UnityFadeCurtainEffectAdapter.cs` | Adapter | Candidate | Framework internal consumer | Unity adapter mutates local curtain root/CanvasGroup and returns explicit effect result. | Visual timing/result mapping needs failure policy review. | Candidate for bounded pilot, ranked after Loading. |
| Pause runtime surface | `Runtime/Pause/PauseSurfaceRuntime.cs` | Surface, Operation Service | Partial | Product/runtime consumer | Applies Pause snapshots to explicit surface adapters and records diagnostics. | Adapter contract is mostly apply-oriented; failure evidence is less rich than Loading/Transition. | Keep partial until `INPUT-APPLY-1` and `STATUS-1`. |
| Pause surface adapter contract | `Runtime/Pause/IPauseSurfaceAdapter.cs` | Adapter | Candidate | Product/runtime consumer | Adapter receives `PauseSurfaceSnapshot` and does not own pause state, input, gates or time scale. | Void apply shape limits canonical result evidence. | Revisit in `STATUS-1` or `PAUSEVIS-1`. |
| Pause resident surface adapter | `Runtime/Pause/UnityPauseResidentSurfaceAdapter.cs` | Adapter | Candidate | Product/runtime consumer | Local resident surface adapter controls GameObject/CanvasGroup visibility from snapshot. | Should not be used to solve input or materialization readiness. | Candidate only after apply/status gates. |
| Pause/InputMode runtime bridge | `Runtime/InputMode/PauseInputModeUnityPlayerInputRuntimeBridge.cs` | Bridge | Blocked | Product/runtime consumer | Resolves host, pause snapshot, preflight plan and PlayerInput application in one MonoBehaviour path. | Thick Bridge/apply boundary blocks broad adapter readiness. | Next gate: `INPUT-APPLY-1`. |
| Pause InputAction trigger | `Runtime/InputMode/PauseInputActionRuntimeBridgeTrigger.cs` | Bridge, Consumer | Blocked | Product/runtime consumer | Trigger delegates InputAction event into the Pause/InputMode bridge. | Depends on the blocked bridge and can hide flow trigger policy concerns. | Keep behind `INPUT-APPLY-1`; later compare with `FLOWTRIGGER`. |
| Pause visual materialization | `Runtime/Pause/PauseVisualSurfaceMaterializationExecutor.cs` | Consumer, Surface candidate, QA evidence | Experimental | No real consumer / QA-style proof | Composes RuntimeContent materialization, ContentAnchor binding and physical placement as an experimental proof. | Not a broad Surface; consumer readiness and ownership are unproven. | Freeze until `PAUSEVIS-1`. |
| Global UI host | `Runtime/GlobalUi/GlobalUiSceneRuntime.cs` and docs | Runtime Surface Host | Candidate | Framework internal consumer | Hosts explicit UI runtime references consumed by Loading/Pause-like paths. | Could become a universal UI manager if expanded by convenience. | Keep host-only; do not promote to Surface layer. |
| RuntimeContent runtime | `Runtime/RuntimeContent/RuntimeContentRuntime.cs` and contracts | Operation Service, Validator/Evidence | Partial | Framework internal consumer | RuntimeContent owns logical handles, materialization/release requests and typed results. | `RuntimeContentRuntime` split remains deferred; not a Surface by itself. | Keep MAT core closed; no broad consumer expansion. |
| RuntimeContent adapters | `IRuntimeMaterializationAdapter`, `IRuntimeReleaseAdapter`, `UnityPrefabRuntimeMaterializationAdapter`, `UnityObjectRuntimeReleaseAdapter` | Adapter | Candidate | Framework internal consumer | Adapter contracts separate physical materialization/release from RuntimeContent core. | Adapter shape is useful but tied to materialization semantics. | Keep as materialization-specific adapter evidence. |
| ContentAnchor runtime | `Runtime/ContentAnchor/*Binding*`, declaration, placement and lifecycle files | Operation Service, Validator/Evidence | Partial | Framework internal consumer | Binding/placement/result vocabulary exists with typed identity evidence. | Physical placement and lifecycle cleanup remain narrow; not a general Surface. | Keep as evidence for MAT and later consumer gates. |
| Materialization service | `Runtime/ContentAnchor/ContentAnchorMaterializationService.cs` | Operation Service | Ready | Framework internal consumer | MAT core service is extracted and validated by tracker evidence. | Broad materialization consumers are still gated. | Keep closed for MAT core; do not reopen for Surface expansion. |
| ContentAnchor materialization bridge | `Runtime/ContentAnchor/UnityContentAnchorMaterializationBridge.cs` | Bridge | Partial | Framework internal / authored consumer | Unity bridge composes authoring/runtime host evidence into materialization execution. | Must remain thin and not regain orchestration ownership. | Watch in future MAT consumer cuts. |
| Placement/materialization/release adapters | `UnityContentAnchorPlacementAdapter`, Unity prefab materialization and Unity object release adapters | Adapter | Candidate | Framework internal consumer | Physical side effects are isolated behind explicit adapter/result types. | Strong adapter evidence, but only inside materialization domain. | Keep as scoped adapter candidates. |
| Participant executor | `Runtime/Common/Participants/ParticipantExecutor.cs` | Operation Service, Validator/Evidence | Ready | Framework internal consumer | Closed participant mechanics execute explicit participants and preserve requiredness/evidence. | No broad expansion to unrelated participant-like domains. | Keep as stable reference, not Surface pilot. |
| CycleReset participant path | `Runtime/CycleReset/*` | Consumer, Validator/Evidence | Ready | Product/runtime or QA consumer | Closed participant pilot evidence exists for reset-style execution. | Should not justify generic participant expansion. | Keep closed; use as consumer reference only. |
| ObjectReset participant path | `Runtime/ObjectReset/*` | Consumer, Adapter, Validator/Evidence | Ready | Product/runtime or QA consumer | Unity participant source and participant behaviours resolve explicit references only. | Concrete reset adapters remain domain-specific. | Keep closed; no Surface expansion. |
| Activity Content Execution path | `Runtime/ActivityFlow/IActivityContentExecutionParticipant*` | Consumer, Bridge candidate | Experimental | Framework internal consumer | Contracts define explicit participant source/participant execution with no discovery runtime. | Discovery/runtime wiring and consumer semantics are not stable. | Do not expand; revisit after lifecycle gates. |
| Route/Activity lifecycle paths | `Runtime/RouteLifecycle/*`, `Runtime/ActivityFlow/*` | Operation Service, Consumer, Validator/Evidence | Partial | Framework internal consumer | Route/Activity lifecycle owns scene/content operations and result evidence. | Operation kernel, readiness and evidence policy remain partial. | Keep behind `LIFECYCLE-KERNEL-REMAINING`. |
| Flow triggers | Route/Activity/Pause trigger paths in runtime | Bridge, Consumer | Blocked | Product/runtime consumer | Repeated trigger/request/state shape exists but no shared helper is accepted. | Helper extraction before input/status gates would mask ownership. | Keep behind `FLOWTRIGGER`. |
| Status/result mapping hot spots | Loading observation, Transition aggregation, Pause/InputMode result mapping, materialization pipeline status mapping | Validator/Evidence | Blocked | Framework internal consumer | Multiple domains map local statuses into aggregate results. | Losing original subsystem evidence is the main broad-readiness risk. | Next after input: `STATUS-1`. |
| QA Canvas and smoke runners | `Runtime/Diagnostics/FrameworkQaCanvas.cs`, `*QaSmokeRunner.cs` | QA Smoke Runner | Ready for evidence only | QA-only consumer | Diagnostics runners provide validation evidence for current contracts. | QA-only consumers must not be counted as product readiness. | Keep as validation surface only. |

## Stable candidates

- `ContentAnchorMaterializationService`: Ready for MAT core service, not broad consumer expansion.
- `ParticipantExecutor`: Ready as participant execution evidence.
- CycleReset/ObjectReset participant paths: Ready as bounded consumer examples.
- RuntimeContent materialization/release adapter contracts: Candidate adapter evidence inside materialization.
- Loading/Transition/Pause Unity visual adapters: Candidate adapter evidence with real runtime consumers.

## Partial but usable evidence

- Loading runtime and observation path prove a Surface-like contract plus adapter execution, but mapping must be reviewed in `STATUS-1`.
- Transition effect orchestration proves an operation service over visual adapters, but it should remain Transition-scoped.
- Pause surface runtime proves snapshot-to-visual adapter application, but Pause/InputMode apply ownership is still blocked.
- GlobalUi is useful as a runtime host, not as a broad Surface layer.
- RouteLifecycle/ActivityFlow provide operation and consumer evidence, but lifecycle readiness remains partial.

## Blocked before expansion

- `PauseInputModeUnityPlayerInputRuntimeBridge` and `PauseInputActionRuntimeBridgeTrigger` block broad adapter readiness until `INPUT-APPLY-1`.
- Status/result mapping across Loading, Transition, Pause/InputMode and materialization blocks broad Surface readiness until `STATUS-1`.
- Flow triggers remain blocked until higher-priority input/status/lifecycle gates clarify ownership.
- Activity Content Execution remains experimental and should not drive Surface expansion.

## Experimental/freeze

- Pause visual materialization stays frozen as experimental evidence until `PAUSEVIS-1`.
- Activity Content Execution participant source/participant contracts stay experimental until lifecycle ownership is stable.
- Materialization pipeline compatibility evidence must not be used to create broad Surface contracts.

## Likely pilot candidates

| Rank | Candidate | Why | Blocking condition |
| --- | --- | --- | --- |
| 1 | Loading surface + `UnityLoadingSurfaceAdapter` | Cleanest Surface/Adapter split, real internal consumer, local Unity side effects. | Must wait for `INPUT-APPLY-1` and `STATUS-1` policy context. |
| 2 | Transition effect surface + `UnityFadeCurtainEffectAdapter` | Good visual adapter candidate with explicit effect results. | Needs status/failure preservation review. |
| 3 | Pause surface + `UnityPauseResidentSurfaceAdapter` | Real product/runtime consumer and clear visual adapter boundary. | Blocked by Pause/InputMode apply boundary and pause visual readiness. |
| 4 | RuntimeContent/ContentAnchor materialization adapters | Strong adapter/service evidence and MAT core is closed. | Broad materialization consumers are not ready. |
| 5 | Pause/InputMode apply path | High-value boundary problem, but not a Surface pilot. | Must be handled first by `INPUT-APPLY-1`. |

## Do-not-expand-yet list

- Do not create a general Surface layer from the current inventory.
- Do not create new adapters from these candidates before the pilot contract is accepted.
- Do not use QA Canvas or smoke runners as proof of product consumer readiness.
- Do not promote GlobalUi into a universal UI manager.
- Do not use Pause visual materialization as a broad materialization Surface.
- Do not extract shared flow trigger helpers before input/status gates.
- Do not reopen MAT core unless future runtime work changes that path.

## Required gates before broad expansion

1. `INPUT-APPLY-1` - define the Pause/InputMode apply boundary outside the thick Unity bridge.
2. `STATUS-1` - define failure/status mapping policy that preserves subsystem evidence without a universal enum.
3. `SURFACE-PILOT-1` - accept one bounded Surface Adapter Contract pilot; Loading is the current best candidate.
4. `PAUSEVIS-1` - decide whether Pause visual materialization has a real consumer and readiness boundary.

The findings do not close any of these gates. They only change the immediate recommendation: run `INPUT-APPLY-1` before selecting the Surface pilot.

## Validation checklist

- Documentation-only cut.
- Architecture folder remains flat.
- No `FXX` active file created.
- Adapter readiness remains partial.
- Surface readiness remains partial.
- No runtime, editor, asmdef, package, scene, prefab or serialized asset modified.
- No Unity compile/import/smoke executed.
