# Immersive Framework Documentation

Canonical planning entry point:

```text
Planning/Immersive-Framework-Roadmap-Revisado.md
```

Accepted architectural decisions:

```text
ADRs/ADR-INDEX.md
```

F0-F13 are closed/applied. F14 `Local/Object Reset Foundation` is active: F14A accepted the architecture and F14B is the next implementation cut.

Current reset boundary:

```text
Cycle Reset is Route/Activity reset only.
Object Reset, Component Reset, Player Reset and physical Unity reset adapters are future phases.
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
F14A approved ObjectResetTarget as ObjectEntryId + owner + scope.
Object Reset uses one IObjectResetParticipant contract and an explicit participant source.
Transform/Rigidbody/Animator, pooling, Player/Actor and gameplay reset remain outside F14.
```
