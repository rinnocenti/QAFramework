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

Status:

```text
F0-F16 closed/applied.
F17 is Gate Foundation; F17D adds the initial request-admission diagnostics smoke.
F17A realigned the plan/ADRs; F17B introduced passive Gate primitives; F17C routes existing in-flight request admission through Gate; F17D adds a synthetic QA smoke for Gate admission diagnostics without adding Pause, Transition, UI or gameplay.
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
F17 - Gate Foundation / request admission integration
F18 - Transition Orchestration Foundation
F19 - Transition Effects / Loading and Fade Adapters
F20 - Pause State and Pause Gate
F21 - Pause Content / Overlay / Input Boundary
F22+ - Advanced Consumers / Gameplay Capabilities
```
