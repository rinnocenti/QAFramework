# Immersive Framework Documentation

This folder contains the canonical documentation for `com.immersive.framework`.

Read the documentation in this order:

1. [Roadmap](Planning/Immersive-Framework-Roadmap-Revisado.md)
2. [ADR index](ADRs/ADR-INDEX.md)
3. [Guides](Guides)

## Canonical Tracks

| Track | Purpose |
|---|---|
| Framework Core / Contracts | Defines pure framework language: contracts, identities, state, diagnostics and boundaries. |
| Unity Build Surface / Lifecycle Wiring | Proves framework contracts through minimal Unity surfaces and lifecycle wiring. |
| Adapter Modules / Gameplay & Subsystems | Adds optional adapters for gameplay and subsystems without moving them into core. |
| Project Assets | Keeps concrete game scenes, prefabs, UI art and product configuration outside canonical package contracts. |
| External Packages | Separates Unity official packages, optional packages, third-party tools and project-specific assets. |

## Current Phase Map

| Phase | Name | Status |
|---|---|---|
| F21 | Save / Snapshot / Preferences / Progression Save Foundation | Closed |
| F22 | Loading Operation / Progress / Readiness Boundary | Closed |
| F23 | Pause Content / Overlay / Input Intent Boundary | Closed |
| F24 | Unity Build Surface / Lifecycle Wiring | Closed / validated by QA surface |
| F25 | Activity Content Scene Composition | Closed / final docs aligned in F25J |
| F26 | Activity Scene Discovery Integration / Loading Progress Integration | Closed / loading progress closed through F26F |
| F27 | Pause UIGlobal Surface, Input Wiring and Gate Reframe | Frozen after F27D / F27E cancelled |
| F28 | Roadmap Reconciliation and Adapter Module Spine | Closed / F28A-F28F complete / F29 selected |
| F29 | Unity Input Target Ownership Proof | Closed / F29A-F29C complete |
| F30 | InputMode Identity and Request Result Model | Closed / F30A-F30E complete |
| F31 | PlayerActor Identity and Unity Input Evidence | Closed / F31A-F31C complete |
| F32 | InputMode Unity Adapter Application | Closed / F32A-F32H complete |
| F33 | Pause Runtime PlayerInput Wiring | Closed / F33A-F33E complete |
| POST-F33-A | Matrix Reconciliation Closeout | Accepted / documentation governance / no next feature selected |
| POST-F33-B | Officialize/Reclassify F28-F33 | Accepted / documentation governance / F28-F33 reclassified |
| F8R-A | RuntimeContent / ContentAnchor Materialization Audit | Draft / audit-only / no implementation selected |
| F8R-B | Runtime Root / Handle / Release Policy Plan | Accepted / Plan / ADR / docs-only |
| F8R-C | Runtime Materialization Adapter Boundary Plan | Accepted / Plan / ADR / docs-only |
| F8R-D | Physical Release Adapter Plan | Accepted / Plan / ADR / docs-only |
| F9R-A | ContentAnchor Runtime Binding Re-entry | Draft / Plan / ADR Proposed / docs-only |
| F8R-E | Unity Prefab Runtime Materialization Adapter Proof | Implemented / first physical adapter proof / QA smoke |

## F8R-E Unity Prefab Runtime Materialization Adapter Proof

F8R-E implements the first explicit Unity physical adapter proof for RuntimeContent. It adds `UnityPrefabRuntimeMaterializationAdapter`, `UnityObjectRuntimeReleaseAdapter`, local adapter-side physical evidence/registry and the QA canvas smoke `Run Runtime Prefab Materialization Smoke`.

F8R-E does not implement ContentAnchor physical placement, Addressables, pooling, scene unload, actor spawn, PlayerInputManager join or gameplay/camera/audio/save consumers.

Project note: `../../Assets/_Documentation/Notes/F8R-E-Unity-Prefab-Runtime-Materialization-Adapter-Proof.md`.

## F23 Boundary

F23 is closed as intent/requirement-only. It keeps Pause language in framework core while deferring every concrete Unity surface.

Canonical F23 contracts:

- `PauseContentRequirement`
- `PausePresentationIntent`
- `PauseInputSignal`
- `PauseInputIntent`

F23 does not create or promise overlay adapters, Content Anchor binding execution, `RuntimeContentAnchorBinding`, Input System wiring, Canvas, prefabs, scene objects, ScriptableObjects, `Time.timeScale` policy or gameplay adapters.

## F24 Boundary

F24 is the next phase. It is a Unity Build Surface / Lifecycle Wiring phase, not an adapter module phase.

Planned cuts:

| Cut | Name |
|---|---|
| F24A | Unity Build / Lifecycle Wiring ADR Plan |
| F24B | Transition <-> GameFlow Runtime Integration |
| F24C | Transition Curtain Unity Build |
| F24D | Loading Screen Unity Adapter Build |
| F24E | Canonical UIGlobal Surface / Loading Cleanup |
| F24F | Activity Transition Policy |
| F24G | Save Moment / Preferences Authoring Boundary |
| F24H | Closure + Usage Guide |

F24B must be the first technical cut. `Transition` already exists as framework language, but `RouteRequestTrigger` / `GameFlow` must pass through a real `TransitionPlan` before curtain, loading or pause visuals are built.

Project documentation now splits the UIGlobal work into `F24E1 - Surface/Loading Legacy Cleanup` and `F24E2 - Route/Activity Visual Operation Policy`. See `Assets/_Documentation/Plans/F24-PLAN-Unity-Build-Surface.md` for the project cut list.
`F24E3 - Surface Adapter Inspector Cleanup` keeps the same runtime shape and trims only authoring/Inspector exposure.

## F25 Boundary

F25 opens as Activity Content Scene Composition. It is framework lifecycle/content core, not a gameplay adapter track. Later adapter module work can still cover gameplay, camera, audio, input, advanced save authoring, pooling/runtime spawned objects, actor/player/NPC, inventory, combat, projectile and damage adapters after the Activity content boundary is stable.

F25 must consume F24 Unity build surfaces and must not create a parallel lifecycle pipeline or move optional subsystem behavior into framework core.

`F24F - Activity Transition Policy` adds an Activity-level authoring policy for optional Activity transitions. Route transitions remain mandatory; Activity loading remains skipped until real Activity content/scene loading exists.

`F24F1 - Activity Loading Reserved Finding` was a historical pre-F25 finding. After the F25R reset and the F25R1 clarification, `FadeWithLoading` means Activity operation uses TransitionSurface and LoadingSurface when the operation requests loading presentation.

## F27 Pause UIGlobal Surface

F27 implements the deferred Unity-facing Pause surface work after F26 loading progress closeout.

F27A adds a Pause surface adapter boundary, collects `IPauseSurfaceAdapter` from the canonical UIGlobal scene, applies `PauseSnapshot` updates after logical Pause requests and exposes a QA `PauseRequestTrigger` for manual validation.

F27A does not bind keyboard/controller input, does not change `Time.timeScale` and does not own Route/Activity lifecycle.

F27B added `UnityPauseInputActionAdapter` as a historical direct Pause InputAction adapter. F33C retires it as an active runtime path because the canonical path must synchronize Pause, InputMode and Unity `PlayerInput`. Use `PauseInputActionRuntimeBridgeTrigger` plus `PauseInputModeUnityPlayerInputRuntimeBridge`.

F27C-F27D reframe Gate away from component blocking, but F27E is cancelled: ordinary input consumers should not each query Gate as the primary Pause/Input strategy. InputMode and adapter ownership must be planned first.

Project plan: `Assets/_Documentation/Plans/F27-PLAN-Pause-UIGlobal-And-Input.md`.

Current closed input plans:

- `Assets/_Documentation/Plans/F30-PLAN-InputMode-Identity-And-Request-Result.md`
- `Assets/_Documentation/Plans/F31-PLAN-PlayerActor-Identity-And-Unity-Input-Evidence.md`

F29A adds Unity Input target declaration vocabulary, validator and ownership smoke. F29B adds canonical QA StartupScene declarations and a loaded-scene smoke step. F29C closes the phase and selects F30. F29 remains declaration-only: no InputMode runtime, action-map switching, PlayerInput ownership, player movement or actor spawning.

F30 is closed through F30E. It adds passive InputMode identity/state/request/result contracts, Pause-to-InputMode request mapping and the Unity Input boundary correction. Unity `PlayerInput` and `PlayerInputManager` are the official execution components; the framework supplies lifecycle language, validation and adapters, not a replacement input manager. F31 is closed through F31C and adds the canonical PlayerActor plus Session PlayerInputManager references required before later InputMode application.


## F32 InputMode Unity Adapter Application

F32 is closed through F32H.

It adds the explicit Unity `PlayerInput` application lane for typed `InputMode` requests:

```text
Gameplay -> ActivateInput + Player action map
PauseOverlay -> ActivateInput + UI action map
FrontendMenu -> ActivateInput + UI action map
InputLocked -> DeactivateInput
```

F32 also bridges completed logical `PauseResult` values to the explicit `InputMode -> PlayerInput` path in QA-facing code. It does not automatically wire into `PauseRuntime` or `FrameworkRuntimeHost`.

F32 does not own `PlayerInputManager`, call join, spawn players, move `PlayerActor`, read gameplay commands or create a framework input manager.

Closeout note: `../../Assets/_Documentation/Notes/F32H-InputMode-Unity-PlayerInput-Application-Closeout.md`.

## F28 Roadmap Reconciliation and Adapter Module Spine

F28 is documentation-first. It turns the F27D freeze into an ordered completion roadmap before new runtime work resumes.

F28 owns the planning spine:

```text
frozen framework core
  -> completion dependency map
  -> adapter/module ownership map
  -> ordered implementation tracks
  -> next runtime cut with clear entry criteria
```

The phase positions InputMode inside the broader adapter graph instead of treating InputMode as the whole next phase. Player/Actor ownership and Unity Input target ownership must be decided before Pause drives action-map behavior.

F28 output:

```text
F28A baseline reconciliation — closed docs-only
F28B completion dependency map — closed docs-only
F28C adapter module taxonomy — closed docs-only
F28D player/actor/input ownership plan — closed docs-only
F28E InputMode and Pause integration plan — closed docs-only
F28F next implementation closeout — closed docs-only; selects F29
```

ADR: `ADRs/F28-ADR-INPUT-001-InputMode-Adapter-Boundary.md`.

F28A note: `../../Assets/_Documentation/Notes/F28A-Frozen-Baseline-Reconciliation.md`.

F28B note: `../../Assets/_Documentation/Notes/F28B-Completion-Dependency-Map.md`.

F28C note: `../../Assets/_Documentation/Notes/F28C-Adapter-Module-Taxonomy.md`.

F28D note: `../../Assets/_Documentation/Notes/F28D-Player-Actor-Input-Ownership-Plan.md`.

F28E note: `../../Assets/_Documentation/Notes/F28E-InputMode-Pause-Integration-Plan.md`.

F28F note: `../../Assets/_Documentation/Notes/F28F-Next-Implementation-Closeout.md`.

F29 plan: `../../Assets/_Documentation/Plans/F29-PLAN-Unity-Input-Target-Ownership-Proof.md`.


## F29 Unity Input Target Ownership Proof

F29 is the first implementation phase selected by F28F.

Purpose:

```text
prove explicit Unity Input target ownership before InputMode behavior or Pause-driven action-map changes are implemented
```

F29A closes Unity Input target declaration evidence and diagnostics for valid, missing and duplicate target configurations. F29B closes authored QA fixture evidence. F29C closes the phase and selects F30. F29 must not implement full InputMode, action-map switching, player/actor runtime spawning, camera, audio, save or gameplay adapters.

Project plan: `../../Assets/_Documentation/Plans/F29-PLAN-Unity-Input-Target-Ownership-Proof.md`.


## F30 InputMode Identity and Request Result Model

F30 is closed through F30E.

F30 creates passive InputMode vocabulary and request/result diagnostics, validates official Unity Input evidence and maps logical Pause state/result to passive InputMode requests. It does not switch action maps, own Unity input, join players or spawn actors.

Primary artifacts:

```text
InputModeKind
InputModeId
InputModeDefinition
InputModeState
InputModeRequest
InputModeRequestResult
InputModeRequestEvaluator
PauseInputModeRequestMapper
```

Closed smokes:

```text
InputMode Contract Smoke
Unity Input Official Component Evidence Smoke
Pause InputMode Request Boundary Smoke
```

Project plan: `../../Assets/_Documentation/Plans/F30-PLAN-InputMode-Identity-And-Request-Result.md`.

Closeout note: `../../Assets/_Documentation/Notes/F30E-InputMode-Unity-Input-Boundary-Closeout.md`.

## F31 PlayerActor Identity and Unity Input Evidence

F31 is closed through F31C.

F31 adds the canonical references required before later Unity Input application:

```text
PlayerActor : IActor + PlayerInput evidence
SessionPlayerInputManagerDeclaration + PlayerInputManager evidence
```

Closed smokes:

```text
PlayerActor Identity Smoke
Session PlayerInputManager Boundary Smoke
```

F31 does not create movement, join behavior, player prefab spawn, action-map switching or a custom input manager.

Project plan: `../../Assets/_Documentation/Plans/F31-PLAN-PlayerActor-Identity-And-Unity-Input-Evidence.md`.

Closeout note: `../../Assets/_Documentation/Notes/F31C-PlayerActor-Session-Input-Reference-Closeout.md`.

## F25 Activity Content Scene Composition

F25 now opens with Activity content scene composition before broader adapter modules.

`F25A - Activity Content Profile Contract` introduced Activity content authoring:

- `ActivityContentProfileAsset`
- `ActivityContentSceneEntry`
- `ActivityContentSceneLoadMode`
- `ActivityContentReleasePolicy`
- `ActivityAsset.ActivityContentProfile`

F25A itself did not load Activity scenes. Later F25 cuts added composition, release, operation planning, ledger tracking and visual-mode diagnostics.

Project plan: `Assets/_Documentation/Plans/F25-PLAN-Activity-Content-Scene-Composition.md`.


`F25B - Activity Scene Composition Plan/Result` adds side-effect-free Activity scene composition diagnostics. Activity requests can now report planned Activity content scenes, required/optional counts and execution-ready declarations, but Activity scene loading and release remain deferred to later F25 cuts.

`F25C - Activity Scene Composition Execution` loads execution-ready Activity content scenes additively. When a canonical `LoadingSurface` exists, Activity scene composition runs inside the loading window. Progress remains indeterminate and Activity content release is deferred to F25D.

`F25R - Activity Scene Operation Architecture Reset` classifies F25C-D4 as experimental/partial execution evidence and makes `ActivityOperationPlan` the required owner of Activity visual policy, scene composition, scene release, LoadingSurface requirement, TransitionSurface visual envelope requirement, Route startup Activity unification and future Activity scene ledger. `F25R1` corrects the visual-policy contradiction and documents the async executor boundary. The reset sequence continues through `F25E` to `F25I`.

`F25E - Activity Operation Plan Baseline` adds side-effect-free Activity operation planning/result types under `Runtime/ActivityFlow`. It does not change execution; it only records the canonical planning language and visual validity rules required before the executor cut.

`F25F - Activity Operation Executor Preview` adds `ActivityOperationPlanner` and a validation-only `ActivityOperationExecutor` facade. It can produce unified preview plans from target Activity loads, previous Activity releases and visual policy, but does not replace the legacy runtime execution path yet.

`F25F1 - Activity Operation Runtime Gate` starts consuming the preview plan in Activity request/clear. After `F25R1`, the gate still blocks true declaration/configuration failures, but `Seamless` and `Fade` are valid with Activity scene side-effects. Activity LoadingSurface is shown only when the valid operation plan explicitly requires it.

`F25F2 - Activity Operation Blocked Diagnostics Fix` preserves the resolved operation visual mode in blocked/failed Activity request diagnostics, so a blocked `Fade` plan no longer reports `activityTransitionMode=Seamless` in the final result fields.

`F25G - Startup Activity Path Unification` previews Route startup Activity as `ActivityOperationKind.RouteStartup`, carries the operation result into `ActivityFlowStartResult`, and adds Route request diagnostics for startup Activity operation and Activity scene composition/release. Route transition/loading remains the outer visual envelope; F25H later adds the final ledger.

### IF-FW-F25D — Activity Content Release

Activity-owned additive scenes loaded through Activity scene composition are now released on Activity change when their scene entry uses `ReleaseOnActivityChange`.
The release operation runs inside the Activity loading window when a LoadingSurface is available and is reported through `activitySceneRelease*` diagnostics.
`KeepOnActivityChange` is valid only across Activity changes; Route changes always force-release Activity-owned scenes.

### IF-FW-F25D1 — Activity release policy semantics

Activity content release policy is scoped to Activity changes only. `ReleaseOnActivityChange` unloads Activity-owned scenes when the Activity is replaced or cleared. `KeepOnActivityChange` keeps them loaded across Activity changes.

Route changes always force-release Activity-owned scenes, regardless of Activity policy. Route content has no release policy; content that survives Route changes must be modeled as Session content.

### IF-FW-F25R - Activity Scene Operation Architecture Reset

Canonical ADR: [F25R Activity Scene Operation Architecture Reset](ADRs/F25R-ADR-ACTIVITY-001-Activity-Scene-Operation-Architecture-Reset.md).

Follow-up cuts:

| Cut | Name |
|---|---|
| F25E | Activity Operation Plan Baseline |
| F25F | Activity Operation Executor Preview |
| F25F1 | Activity Operation Runtime Gate |
| F25F2 | Activity Operation Blocked Diagnostics Fix |
| F25G | Startup Activity Path Unification |
| F25H | Activity Scene Ledger |
| F25I | Activity Operation Validator Guards |

`F25R1 - Activity Visual Policy / Awaitable Clarification` resolves the contradiction between the early F25R wording and the consolidated F25/F25J reading: `Seamless` and `Fade` may execute Activity scene side-effects without `LoadingSurface`, while `FadeWithLoading` owns `LoadingSurface`. `ActivityOperationPlan` stays synchronous and side-effect-free; the future executor boundary may use `UnityEngine.Awaitable`.

Diagnostic mapping:

- `loading=SkippedNoSceneLoad` means no Activity scene load/release side-effect happened.
- `loading=SkippedByActivityPolicy` means a scene load/release side-effect happened, but the authored visual mode did not request `LoadingSurface`.
- `loading=SucceededWithUnitySurface` is only expected for Activity when `ActivityVisualTransitionMode = FadeWithLoading` and `LoadingSurface` is available/resolved.

`F25H - Activity Scene Ledger` replaces the implicit loaded Activity scene list with an explicit internal ledger. The ledger records route instance id, Activity identity, content id, scene path/name, Activity release policy, Activity ownership and Loaded/Released/Stale state. Existing visual/loading behavior is preserved, while Activity/Route logs gain `activitySceneLedger*` snapshot fields.


`F25I1 - Activity Operation Visual Mode Scope Correction` corrects the F25I guard. Activities with Activity content scene declarations may use `Seamless`, `Fade` or `FadeWithLoading`. Existing profile guards for required scene references, cached scene names without scene paths and duplicate content ids remain active. Runtime planning no longer blocks `Seamless/Fade + scene side-effect`; it only controls whether TransitionSurface and LoadingSurface are used.


`F25I2 - Loading Skip Diagnostics Refinement` is diagnostics-only. When an Activity operation executes Activity scene load/release without opening LoadingSurface because the authored visual mode is `Seamless` or `Fade`, request logs now report `loading=SkippedByActivityPolicy` instead of `loading=SkippedNoSceneLoad`. No runtime loading, transition or ledger behavior changes.

`F25J - Activity Operation Final Documentation / Matrix Alignment` closes the F25 documentation baseline. Canonical final rule: visual mode selects presentation, not permission to execute Activity scene composition/release. `Seamless` skips TransitionSurface and LoadingSurface, `Fade` uses only TransitionSurface, and `FadeWithLoading` uses TransitionSurface plus LoadingSurface when the operation requests loading presentation. Cleanup of false/legacy trails is deferred to a dedicated Codex audit.

`F25H1 - Activity Scene Ledger Route Instance Key Fix` stabilizes Activity scene ledger ownership by matching loaded entries with `RouteInstanceId + Activity + ContentIdentity`, keeping Activity-owned content scoped to the route instance that created it.

## F26 Activity Scene Discovery Integration

`F26A - Activity Scene Discovery Integration` connects Activity-owned additive scenes loaded by Activity scene composition to Activity content discovery. After composition records loaded scenes in `ActivitySceneLedger`, Activity discovery scans the Route primary scene plus loaded Activity-owned scenes for the current Route instance and Activity. Route-owned discovery remains separate, and `IActivityContentExecutionParticipantSource` remains the explicit source for execution participants.

`F26A1 - Activity Content Execution Diagnostics Clarification` keeps the existing `activityContentExecution*` fields for compatibility and adds explicit `activityContentParticipant*` diagnostics. Local Activity content is represented by `activityContentHandles`, `activityContentBindings` and `activityContentLifecycle`; participant execution represents only participants supplied by `IActivityContentExecutionParticipantSource`.

`F26B - Loading Progress Contract` adds the internal loading progress model and explicit diagnostics fields for `loadingProgressSupported`, `loadingProgressMode`, `loadingProgressValue`, `loadingProgressPercent`, `loadingProgressPhase` and `loadingProgressMessage`. It does not add a loading bar, does not change the visual LoadingSurface contract, and does not wire real scene loading progress yet.

`F26C - Loading Surface Progress Bar Receiver` wires the Unity loading surface to receive progress requests without inventing a determinate source. The QA surface in `QA_UIGlobal` exposes a progress bar and progress-capable adapters make `LoadingSurfaceRuntime.ProgressSupported` true.

`F26D - Determinate Loading Progress Source` wires concrete `SceneManager.LoadSceneAsync` and `SceneManager.UnloadSceneAsync` progress into the loading surface reporter used by route and activity lifecycle operations. It preserves transition/loading ordering and reports determinate diagnostics only when a real scene operation emits progress.


`F26E - Aggregated Loading Progress` maps local SceneLifecycle progress into weighted Route/Activity operation progress. `SceneLifecycleRuntime` still reports concrete scene load/unload progress, while Route/Activity lifecycle owners wrap those reports into `RouteTransition` or `ActivityTransition` aggregate phases so multi-step loading no longer restarts the progress value per scene.

`F25H2 - Activity Scene Ledger Route-Scoped Queries` removes unused route-less loaded-entry collection methods from `ActivitySceneLedger`. Canonical Activity-owned scene ledger reads must include `RouteInstanceId`, preventing future cross-route stale tracking regressions.


`F26F - Loading Progress Polish / Documentation Closeout` closes the loading progress thread after F26C-F26E validation. The accepted baseline is: core/lifecycle owns technical progress and diagnostics; visual adapters may smooth the player-facing fill without changing diagnostic values. F26F also renames the QA Activity content scene typo from `AtivityAdditionalConent` to `ActivityAdditionalContent` and records the delete manifest required for zip-based application.


## F27 Pause UIGlobal / Input / Gate Reframe

`F27A` validates the Pause UIGlobal surface. `F27B` validates the narrow Unity Input System `PauseToggle` adapter. `F27C` audits the Gate/Input boundary and accepts the correction that Gate is capability/admission language, not a component blocker. `F27D` applies that correction in runtime by changing Pause-derived blockers from broad gameplay language to `Input/InputAcceptance` and `Interaction/InteractionAcceptance`.

Canonical F27C rule:

```text
Pause produces blockers.
Input and command adapters consume Gate.
Gameplay components are not paused directly by Gate.
TimeScale freeze policy is separate.
```

See:

```text
ADRs/F27-ADR-GATE-INPUT-001-Capability-Gate-Boundary.md
Assets/_Documentation/Notes/F27C-Gate-Input-Capability-Audit.md
Assets/_Documentation/Notes/F27D-Pause-Capability-Gate-Reframe.md
```

## Current Planning Status

- F29A — Unity Input target declaration proof: PASS by user smoke.
- F29B — Input target QA authoring fixture: closed as QA scene evidence + loaded-scene smoke step.



## F32 InputMode Unity Adapter Application

F32 starts after F30E/F31C. `F31D — PlayerInput Reference Set` is cancelled and not part of the official sequence.

F32A adds `InputModeUnityApplicationPreviewEvaluator`, a pure preview that checks whether a successful `InputModeRequestResult` has enough Unity Input evidence to be applied later by an adapter:

```text
Gameplay -> GameplayCommands target + PlayerActor evidence + Session PlayerInputManager evidence
PauseOverlay -> GlobalUiPause target
FrontendMenu -> GlobalUiPause target
InputLocked -> no target required in F32A
```

F32A does not switch action maps, activate/deactivate `PlayerInput`, call `PlayerInputManager.JoinPlayer`, spawn actors or create a framework input manager.

Project plan: `../../Assets/_Documentation/Plans/F32-PLAN-InputMode-Unity-Adapter-Application.md`.

F32A note: `../../Assets/_Documentation/Notes/F32A-InputMode-Unity-Application-Preview.md`.

## F30D — Pause InputMode Request Boundary

F30D closes a passive Pause-to-InputMode request bridge. Pause can now produce a logical `InputModeRequest` for `PauseOverlay` or `Gameplay` through a mapper, but Unity `PlayerInput` / `PlayerInputManager` remain official execution components and no action-map behavior is introduced.


## F31 — PlayerActor Identity

- `Assets/_Documentation/Plans/F31-PLAN-PlayerActor-Identity-And-Unity-Input-Evidence.md`
- `Assets/_Documentation/Notes/F31A-PlayerActor-Identity-PlayerInput-Evidence.md`
- `Assets/_Documentation/Notes/F31B-Session-PlayerInputManager-Boundary.md`


## F30/F31 Input Closeout

F30 is closed as passive InputMode and Unity Input boundary language. F31 is closed as canonical PlayerActor and Session PlayerInputManager reference evidence.

Accepted prerequisites for later Unity Input adapter work:

```text
UnityInputTargetDeclaration;
InputModeRequest / InputModeRequestResult;
PauseInputModeRequestMapper;
PlayerActor : IActor + PlayerInput evidence;
SessionPlayerInputManagerDeclaration + PlayerInputManager evidence.
```

Rejected direction:

```text
framework-owned input manager;
hidden action-map switching inside InputMode or Pause;
Activity-owned canonical PlayerInputManager.
```

Reference notes:

```text
Assets/_Documentation/Notes/F30E-InputMode-Unity-Input-Boundary-Closeout.md
Assets/_Documentation/Notes/F31C-PlayerActor-Session-Input-Reference-Closeout.md
```


- F32B — InputMode Unity Action Map Preview: `Assets/_Documentation/Notes/F32B-InputMode-Unity-Action-Map-Preview.md`.

- F32C — InputMode Unity Application Plan: dry-run adapter plan only; no Unity Input side effects.

- F32D — InputMode Unity PlayerInput Adapter: first explicit PlayerInput adapter side effect; `SwitchCurrentActionMap`/`DeactivateInput` only through the adapter, no PlayerInputManager join/spawn/custom manager.

- F32E — InputMode Unity PlayerInput Application: explicit application wrapper that activates `PlayerInput` before selecting action maps, delegates lock to the F32D adapter, and still never owns `PlayerInputManager`, join, spawn or movement.

- F32F — InputMode Unity PlayerInput Request Application: composed explicit request-to-PlayerInput application path; no PlayerInputManager join/spawn/movement.


- F32G: Pause result to explicit Unity PlayerInput application bridge remains QA-facing and is not auto-wired into PauseRuntime/FrameworkRuntimeHost.

## F33 Pause Runtime PlayerInput Wiring

F33 is closed through F33E. It introduces the opt-in authored Pause input path from Unity `InputAction` to logical Pause, `InputMode` and explicit Unity `PlayerInput` application. F33C retires the older direct `UnityPauseInputActionAdapter` as an active runtime path. F33D flattens trigger/bridge diagnostics so smoke logs remain readable.

It is not automatic `FrameworkRuntimeHost` wiring and still does not own `PlayerInputManager`, call `JoinPlayer`, spawn player prefabs, move actors or read gameplay commands.

Project plan: `../../Assets/_Documentation/Plans/F33-PLAN-Pause-Runtime-PlayerInput-Wiring.md`.

F33A note: `../../Assets/_Documentation/Notes/F33A-Pause-Runtime-PlayerInput-Bridge.md`.

F33B note: `../../Assets/_Documentation/Notes/F33B-Pause-InputAction-Runtime-Bridge-Trigger.md`.

F33C note: `../../Assets/_Documentation/Notes/F33C-Legacy-Pause-InputAction-Adapter-Retirement.md`.

F33D note: `../../Assets/_Documentation/Notes/F33D-Pause-Input-Diagnostics-Flattening.md`.

F33E closeout note: `../../Assets/_Documentation/Notes/F33E-Pause-Runtime-PlayerInput-Wiring-Closeout.md`.

F33E1 correction note: `../../Assets/_Documentation/Notes/F33E1-Next-Phase-Selection-Correction.md`. F33 is closed, but F33E does not select the following implementation phase.

## POST-F33 Matrix Reconciliation

POST-F33-A closes the matrix reconciliation as documentation / roadmap governance. F33 remains closed, but it does not select F34, gameplay or any other feature phase.

F28-F33 are official only as controlled anticipation of the Input / Pause / Unity `PlayerInput` axis. RuntimeContent, ContentAnchor, materialization, runtime roots, handles and release policy must be re-audited before camera, audio, save/progression, pooling/runtime-spawned, actor materialization or gameplay consumers.

References:

- `Assets/_Documentation/Notes/POST-F33-A-Matrix-Reconciliation-Closeout.md`
- `Assets/_Documentation/Notes/POST-F33-B-Officialize-Reclassify-F28-F33.md`
- `Assets/_Documentation/Plans/POST-F33-PLAN-Matrix-Reconciliation.md`
- `Assets/_Documentation/Audits/F8R-A-RuntimeContent-ContentAnchor-Materialization-Audit.md`
- `Assets/_Documentation/Plans/F8R-B-PLAN-Runtime-Root-Handle-Release-Policy.md`
- `Assets/_Documentation/Notes/F8R-B1-Runtime-Root-Handle-Release-Policy-Acceptance.md`
- `Packages/com.immersive.framework/Documentation~/ADRs/F8R-B-ADR-Runtime-Root-Handle-Release-Policy.md`
- `Assets/_Documentation/Plans/F8R-C-PLAN-Runtime-Materialization-Adapter-Boundary.md`
- `Assets/_Documentation/Notes/F8R-C1-Runtime-Materialization-Adapter-Boundary-Acceptance.md`
- `Packages/com.immersive.framework/Documentation~/ADRs/F8R-C-ADR-Runtime-Materialization-Adapter-Boundary.md`
- `Assets/_Documentation/Plans/F8R-D-PLAN-Physical-Release-Adapter.md`
- `Assets/_Documentation/Notes/F8R-D1-Physical-Release-Adapter-Acceptance.md`
- `Packages/com.immersive.framework/Documentation~/ADRs/F8R-D-ADR-Physical-Release-Adapter.md`
- `Assets/_Documentation/Plans/F9R-A-PLAN-ContentAnchor-Runtime-Binding-Reentry.md`
- `Packages/com.immersive.framework/Documentation~/ADRs/F9R-A-ADR-ContentAnchor-Runtime-Binding-Reentry.md`

POST-F33-B officially reclassifies F28-F33: F28-F31 are official planning/evidence/language phases, while F32-F33 are controlled anticipation of the Input / Pause / Unity `PlayerInput` axis with Unity side effects limited to explicit adapters.

F8R-A is the current audit-only RuntimeContent / ContentAnchor materialization state check. It does not authorize F34, gameplay, camera, audio, save/progression, pooling/runtime-spawned or actor materialization.

F8R-B is accepted as the ownership rule for the next planning decision: RuntimeContent core keeps runtime roots, handles and release policy as logical framework language, while future Unity adapters may own physical materialization only after a separate accepted cut.

F8R-C is accepted as the boundary for a future Unity materialization adapter. It does not implement a materializer and keeps physical object evidence inside the adapter boundary without leaking it into pure RuntimeContent core.

F8R-D is accepted as the boundary for future physical release adapters. It does not implement cleanup and keeps `Destroy`, Pooling return, Addressables release and scene unload outside RuntimeContent core until a later accepted implementation cut.

F9R-A is proposed as the ContentAnchor runtime binding re-entry plan after F8R-B/C/D. It keeps ContentAnchor binding logical: declaration/discovery evidence, binding request/result, logical content handles, diagnostics and logical cleanup only. It does not implement physical placement and does not select materializer, release adapter, camera, audio, save/progression, gameplay, pooling/runtime-spawned, actor materialization or F34.
