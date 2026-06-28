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

## Current Phase Map

| Phase | Name | Status |
|---|---|---|
| F21 | Save / Snapshot / Preferences / Progression Save Foundation | Closed |
| F22 | Loading Operation / Progress / Readiness Boundary | Closed |
| F23 | Pause Content / Overlay / Input Intent Boundary | Closed |
| F24 | Unity Build Surface / Lifecycle Wiring | Closed / validated by QA surface |
| F25 | Activity Content Scene Composition | Closed / final docs aligned in F25J |
| F26 | Activity Scene Discovery Integration | Open |

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

`F24F1 - Activity Loading Reserved Finding` was a historical pre-F25 finding. After F25I1/F25I2, `FadeWithLoading` is no longer reserved; it means Activity operation uses TransitionSurface and LoadingSurface when the operation requests loading presentation.

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

`F25R - Activity Scene Operation Architecture Reset` classifies F25C-D4 as experimental/partial execution evidence and makes `ActivityOperationPlan` the required owner of Activity visual policy, scene composition, scene release, LoadingSurface requirement, TransitionSurface visual envelope requirement, Route startup Activity unification and future Activity scene ledger. `F25I1` corrects the visual-mode rule: `Seamless + Activity scene load/release side-effect` is valid, but it must not open LoadingSurface implicitly.

`F25E - Activity Operation Plan Baseline` adds side-effect-free Activity operation planning/result types under `Runtime/ActivityFlow`. It does not change execution; it only records the canonical planning language and visual validity rules required before the executor cut.

`F25F - Activity Operation Executor Preview` adds `ActivityOperationPlanner` and a validation-only `ActivityOperationExecutor` facade. It can produce unified preview plans from target Activity loads, previous Activity releases and visual policy, but does not replace the legacy runtime execution path yet.

`F25F1 - Activity Operation Runtime Gate` starts consuming the preview plan in Activity request/clear. After `F25I1`, the gate still blocks true declaration/configuration failures, but `Seamless` and `Fade` are valid with Activity scene side-effects. Activity LoadingSurface is shown only when the valid operation plan explicitly requires it.

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
| F25I1 | Activity Operation Visual Mode Scope Correction |
| F25I2 | Loading Skip Diagnostics Refinement |

`F25H - Activity Scene Ledger` replaces the implicit loaded Activity scene list with an explicit internal ledger. The ledger records route instance id, Activity identity, content id, scene path/name, Activity release policy, Activity ownership and Loaded/Released/Stale state. Existing visual/loading behavior is preserved, while Activity/Route logs gain `activitySceneLedger*` snapshot fields.


`F25I1 - Activity Operation Visual Mode Scope Correction` corrects the F25I guard. Activities with Activity content scene declarations may use `Seamless`, `Fade` or `FadeWithLoading`. Existing profile guards for required scene references, cached scene names without scene paths and duplicate content ids remain active. Runtime planning no longer blocks `Seamless/Fade + scene side-effect`; it only controls whether TransitionSurface and LoadingSurface are used.


`F25I2 - Loading Skip Diagnostics Refinement` is diagnostics-only. When an Activity operation executes Activity scene load/release without opening LoadingSurface because the authored visual mode is `Seamless` or `Fade`, request logs now report `loading=SkippedByActivityPolicy` instead of `loading=SkippedNoSceneLoad`. No runtime loading, transition or ledger behavior changes.

`F25J - Activity Operation Final Documentation / Matrix Alignment` closes the F25 documentation baseline. Canonical final rule: visual mode selects presentation, not permission to execute Activity scene composition/release. `Seamless` skips TransitionSurface and LoadingSurface, `Fade` uses only TransitionSurface, and `FadeWithLoading` uses TransitionSurface plus LoadingSurface when the operation requests loading presentation. Cleanup of false/legacy trails is deferred to a dedicated Codex audit.

## F26 Activity Scene Discovery Integration

`F26A - Activity Scene Discovery Integration` connects Activity-owned additive scenes loaded by Activity scene composition to Activity content discovery. After composition records loaded scenes in `ActivitySceneLedger`, Activity discovery scans the Route primary scene plus loaded Activity-owned scenes for the current Route instance and Activity. Route-owned discovery remains separate, and `IActivityContentExecutionParticipantSource` remains the explicit source for execution participants.
