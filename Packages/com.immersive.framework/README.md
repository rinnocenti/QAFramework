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
```


Status:

```text
F0-F18 closed/applied. F19 is in progress through F19A.
F17 is Gate Foundation and is closed through F17E. F18 is Transition Orchestration Foundation and is closed through F18F.
F17A realigned the plan/ADRs; F17B introduced passive Gate primitives; F17C routes existing in-flight request admission through Gate; F17D added a synthetic QA smoke for Gate admission diagnostics; F17E closes the phase without adding Pause, Transition runtime, UI or gameplay. F18A accepts the Transition Orchestration implementation plan. F18B introduces passive Transition primitives. F18C adds a synthetic Transition diagnostics smoke for plan/result/snapshot shapes. F18D adds a passive Transition-to-Gate blocker relationship and smoke. F18E adds a passive Route/Activity orchestration observation policy and smoke. F18F closes the phase with a Transition Orchestration usage guide and hands off to F19. F19A accepts the Transition Effects boundary/implementation plan. Fade/loading/curtain are F19 adapters/effects and are not core Transition.
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
F19 - Transition Effects / Loading and Fade Adapters / IN PROGRESS
F20 - Pause State and Pause Gate
F21 - Pause Content / Overlay / Input Boundary
F22+ - Advanced Consumers / Gameplay Capabilities
```


F19A transition effects note:

```text
F19A is documentation/plan only.
No scene, GameObject, component setup or ScriptableObject is required yet.
When a later F19 adapter cut needs Unity authoring, the cut must include exact manual setup steps for the QA scene/object/SO and expected smoke logs.
```
