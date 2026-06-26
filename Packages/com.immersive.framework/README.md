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

Closed phases must add or update a `Usage` guide there. F17 Gate Foundation usage is documented at:

```text
Documentation~/Guides/F17-Gate-Foundation-Usage.md
```


Status:

```text
F0-F17 closed/applied.
F17 is Gate Foundation and is closed through F17E. F18 is now in progress as Transition Orchestration Foundation.
F17A realigned the plan/ADRs; F17B introduced passive Gate primitives; F17C routes existing in-flight request admission through Gate; F17D added a synthetic QA smoke for Gate admission diagnostics; F17E closes the phase without adding Pause, Transition runtime, UI or gameplay. F18A accepts the Transition Orchestration implementation plan. F18B introduces passive Transition primitives and keeps fade/loading/curtain out of core.
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
F18 - Transition Orchestration Foundation / IN PROGRESS - F18B APPLIED
F19 - Transition Effects / Loading and Fade Adapters
F20 - Pause State and Pause Gate
F21 - Pause Content / Overlay / Input Boundary
F22+ - Advanced Consumers / Gameplay Capabilities
```
