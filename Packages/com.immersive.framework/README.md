# Immersive Framework

Unity package for the Immersive Framework lifecycle architecture.

The canonical framework plan is:

```text
Documentation~/Planning/Immersive-Framework-Roadmap-Revisado.md
```

Accepted architectural decisions are indexed at:

```text
Documentation~/ADRs/ADR-INDEX.md
```


Usage guides are kept under:

```text
Documentation~/Guides/
```

Closed phases must add or update a `Usage` guide there. Current closed usage guides include:

```text
Documentation~/Guides/F17-Gate-Foundation-Usage.md
Documentation~/Guides/F18-Transition-Orchestration-Usage.md
Documentation~/Guides/F19-Transition-Effects-Usage.md
```


Status:

```text
F0-F19 closed/applied. F20 is in progress through F20B.
F17 is Gate Foundation and is closed through F17E. F18 is Transition Orchestration Foundation and is closed through F18F. F19 is Transition Effects and is closed through F19F. F20A accepts the Pause State/Gate implementation plan. F20B introduces passive Pause state primitives under `Runtime/Pause`.
F17A realigned the plan/ADRs; F17B introduced passive Gate primitives; F17C routes existing in-flight request admission through Gate; F17D added a synthetic QA smoke for Gate admission diagnostics; F17E closes the phase without adding Pause, Transition runtime, UI or gameplay. F18A accepts the Transition Orchestration implementation plan. F18B introduces passive Transition primitives. F18C adds a synthetic Transition diagnostics smoke for plan/result/snapshot shapes. F18D adds a passive Transition-to-Gate blocker relationship and smoke. F18E adds a passive Route/Activity orchestration observation policy and smoke. F18F closes the phase with a Transition Orchestration usage guide and hands off to F19. F19A accepts the Transition Effects boundary/implementation plan. F19B introduces passive Transition Effect primitives under `Runtime/TransitionEffects`. F19C adds `Run Transition Effect Diagnostics Smoke`. F19D adds the minimal built-in Unity `UnityFadeCurtainEffectAdapter`, adapter contract and `Run Unity Fade Curtain Effect Adapter Smoke`. F19E adds required/optional effect policy guardrails and `Run Transition Effect Policy Guardrails Smoke`, using explicit adapter lists only. F19F closes the phase with `Documentation~/Guides/F19-Transition-Effects-Usage.md`, preserves the asset-free policy boundary, and compacts QA Canvas by hiding phase diagnostics behind foldouts. F20A accepts the Pause State/Gate implementation plan: Pause is state plus Gate blocker relationship, not Activity, menu, overlay, input system or `Time.timeScale` contract. F20B adds passive Pause primitives: `PauseRequestId`, `PauseState`, `PauseRequestKind`, `PauseRequestStatus`, `PauseIssue`, `PauseRequest`, `PauseResult` and `PauseSnapshot`. Fade/loading/curtain are F19 adapters/effects and are not core Transition.
```

F15-F16 reset adapter closure:

```text
Unity Reset Adapters mínimos are closed with:
- explicit Unity participant source;
- Transform Reset Participant with authored local baseline;
- required adapter/baseline guardrails;
- authoring UX and guide;
- closure smoke.

F16 then added GameObject activeSelf reset as a primitive technical adapter.
```

Current reset boundary:

```text
Cycle Reset covers Route/Activity cycle reset.
Object Reset foundation provides logical orchestration.
F15 added Transform local baseline reset. F16 added GameObject activeSelf baseline reset.
Rigidbody, Animator, Player/Actor, Pooling and Gameplay reset remain future work.
Contextual reset for Player/Actor/NPC/Timer/Door/Pickup is deferred until after Gate, Transition and Pause, and after a mature gameplay object model exists.
```

Current planning axis:

```text
F17 - Gate Foundation / CLOSED
F18 - Transition Orchestration Foundation / CLOSED
F19 - Transition Effects / Loading and Fade Adapters / CLOSED
F20 - Pause State and Pause Gate / IN PROGRESS / F20B PRIMITIVES APPLIED
F21 - Pause Content / Overlay / Input Boundary / PLANNED
F22+ - Advanced Consumers / Gameplay Capabilities
```


F19 transition effects note:

```text
F19A is documentation/plan only. F19B adds passive effect primitives only. F19C adds synthetic diagnostics smoke only.
F19D adds a minimal Unity fade/curtain adapter boundary. The canonical smoke creates a transient QA object, so no project scene asset is required to validate compile/smoke. For manual visual testing, create a scene GameObject with CanvasGroup and UnityFadeCurtainEffectAdapter; see `Documentation~/Guides/F19D-Minimal-Fade-Curtain-Adapter-Setup.md`.
F19E adds policy/authoring guardrails for required/optional adapters. It does not create a ScriptableObject or registry; the policy evaluates an explicit adapter list supplied by the caller/smoke.
F19F closes the phase with `Documentation~/Guides/F19-Transition-Effects-Usage.md` and compacts QA Canvas: only baseline smokes stay visible by default; Gate/Transition/Effect, Route/Content, Foundation and Reset/Object diagnostics are collapsed.
```

F20 pause note:

```text
F20B adds passive Pause primitives under `Runtime/Pause`. No scene object, Canvas, prefab or ScriptableObject is required.
F20 remains the logical Pause core: state, request/result, snapshot/facts, policy and Gate blocker relationship.
Pause visual content, overlay and input binding are F21 boundaries.
F20C is the next cut: Pause Diagnostics Smoke.
```
