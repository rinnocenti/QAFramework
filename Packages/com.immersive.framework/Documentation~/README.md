# Immersive Framework Documentation

Canonical planning entry point:

```text
Planning/Immersive-Framework-Roadmap-Revisado.md
```

Accepted architectural decisions:

```text
ADRs/ADR-INDEX.md
```

F0-F14 are closed/applied. F15A accepted the `Unity Reset Adapters mínimos` ADR; F15B is the next implementation cut.

Current reset boundary:

```text
Cycle Reset is Route/Activity reset only.
Object Reset foundation is closed as logical orchestration only.
Component Reset, Player Reset and physical Unity reset adapters are future phases.
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
Transform/Rigidbody/Animator, pooling, Player/Actor and gameplay reset remain outside F14. F15 starts only with technical Unity reset adapters, not gameplay reset.
```


Closed Object Reset usage note:

```text
Author a current Object Entry Declaration.
Add Object Reset Trigger and reference that declaration.
UGUI/Button may call ObjectResetTrigger.RequestObjectReset() directly.
Object Reset Trigger Unity Event Bridge is optional and only adapts trigger events to Inspector UnityEvents.
Until F15 adapters exist, a valid authored trigger can complete as SucceededNoParticipants.
```


F15 adapter boundary note:

```text
Unity Reset Adapters are technical IObjectResetParticipant implementations.
They must target Object Entry identity, not GameObject.name/path.
Required adapter/source absence must be explicit and cannot be hidden by SucceededNoParticipants.
Player, Actor, Pooling, Save/Checkpoint and gameplay reset stay outside F15.
```
