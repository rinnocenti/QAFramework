# Immersive Framework Documentation

Canonical planning entry point:

```text
Planning/Immersive-Framework-Roadmap-Revisado.md
```

Accepted architectural decisions:

```text
ADRs/ADR-INDEX.md
```

F0-F12 are closed/applied. F13 `Object Entry Foundation` is in progress through F13H; F13I reconciles the implemented passive catalog/snapshot with the remaining ownership and lifecycle requirements.

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

Current Object Entry boundary:

```text
Object Entry is a passive logical catalog/snapshot.
It is not GameObject binding, Object Reset, a mutable registry or a service locator.
F13 remains open until ownership, scoped collection and snapshot invalidation are explicit.
```
