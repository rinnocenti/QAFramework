# Immersive Framework Documentation

Canonical planning entry point:

```text
Planning/Immersive-Framework-Roadmap-Revisado.md
```

Accepted architectural decisions:

```text
ADRs/ADR-INDEX.md
```


Usage guides:

```text
Guides/F15-Unity-Object-Reset-Adapters-Usage.md
Guides/F16-GameObject-Active-Reset-Usage.md
Guides/F17-Gate-Foundation-Usage.md
Guides/F18-Transition-Orchestration-Usage.md
```

Closure rule: when a framework phase is closed, add or update its `Usage` guide under `Documentation~/Guides/`.


F0-F18 are closed/applied. F17 is Gate Foundation. F18 is Transition Orchestration Foundation. F19 is in progress through F19A.
F17A realigned the plan/ADRs; F17B introduced passive Gate primitives; F17C integrates those primitives with existing request-admission guards; F17D added a synthetic QA smoke for Gate admission diagnostics; F17E closes the phase and hands off to F18. F18A accepts the Transition Orchestration implementation plan. F18B introduces passive Transition primitives. F18C adds a synthetic Transition diagnostics smoke for plan/result/snapshot shapes without runtime visual effects. F18D adds a passive Transition-to-Gate blocker relationship and synthetic smoke without registering runtime Gate state. F18E adds passive Route/Activity orchestration observation and smoke without executing requests. F18F closes the phase with `Guides/F18-Transition-Orchestration-Usage.md` and hands off to F19 Transition Effects. F19A accepts the Transition Effects boundary/implementation plan and records that no scene/object/SO setup is required yet.

Current reset boundary:

```text
Cycle Reset is Route/Activity reset only.
Object Reset foundation is closed as logical orchestration only.
Player Reset, Actor Reset and contextual gameplay reset are future phases. Minimal physical Unity reset adapters are Transform local baseline reset and GameObject activeSelf reset.
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

Cycle Reset authoring note:

```text
RouteCycleResetTrigger and ActivityCycleResetTrigger are the primary components.
Unity Event Bridges are optional and are only needed for Inspector/UnityEvent callbacks.
```

Closed Object Entry boundary:

```text
Object Entry is a passive lifecycle-owned logical catalog/snapshot.
It is not GameObject binding, Object Reset, a mutable registry or a service locator.
Route/Activity owners, scoped collection and snapshot lifecycle were closed in F13.
```

Current Object Reset boundary:

```text
F14 closed ObjectResetTarget as ObjectEntryId + owner + scope from the current Object Entry snapshot.
Object Reset has request/policy/result, target resolver, participant contract/source, deterministic plan/runtime executor, Runtime Host integration, public trigger and optional UnityEvent bridge.
Transform/Rigidbody/Animator, pooling, Player/Actor and gameplay reset remain outside F14. F15 closed the minimal technical Unity reset adapter path with an explicit participant source, Transform local baseline reset, required guardrails, authoring UX and closure smoke. F16 added GameObject activeSelf reset as a second primitive adapter. Gameplay reset remains outside F15/F16.
```


Closed Object Reset usage note:

```text
Author a current Object Entry Declaration.
Add Object Reset Trigger and reference that declaration.
UGUI/Button may call ObjectResetTrigger.RequestObjectReset() directly.
Object Reset Trigger Unity Event Bridge is optional and only adapts trigger events to Inspector UnityEvents.
With F15 adapters, a valid authored trigger can execute Transform reset when a participant source and Transform Reset Participant are configured. SucceededNoParticipants remains valid only when policy allows no participants; required adapter/baseline absence must be explicit.
```


Closed F15 adapter boundary note:

```text
Unity Reset Adapters are technical IObjectResetParticipant implementations. F15 includes Transform Reset Participant. F16 includes GameObject Active Reset Participant.
They must target Object Entry identity, not GameObject.name/path.
Required adapter/source absence must be explicit and cannot be hidden by SucceededNoParticipants.
Player, Actor, Pooling, Save/Checkpoint and gameplay reset stay outside F15/F16 and remain deferred past F17-F21.
```


F19A authoring note:

```text
No Unity scene object, component setup or ScriptableObject is required in F19A.
F19D+ may require QA scene authoring for a concrete Unity adapter. When that happens, the cut must explain exactly what to create and which smoke/logs validate it.
```
