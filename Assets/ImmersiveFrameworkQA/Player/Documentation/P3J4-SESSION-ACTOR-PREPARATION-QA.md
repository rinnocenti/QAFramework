# P3J.4 — Session Actor Preparation QA

Run outside Play Mode:

```text
Immersive Framework
  > QA
    > Player
      > P3J.4 Run Session Actor Preparation Smoke
```

Expected:

```text
[P3J4_SESSION_ACTOR_PREPARATION_SMOKE]
status='Passed'
cases='22'
```

The smoke uses transient ScriptableObjects and GameObjects. It proves:

```text
typed Session preparation contracts
plain scoped runtime authority
selection routed through preparation guard
selected Actor prepare and activation
per-Slot immutable summary and functional token
idempotent prepare and release
two independent prepared Slots
foreign and stale token rejection
release preserving joined host and selection
new Actor identity after rematerialization
transactional replacement preserving PlayerInput host
failed duplicate replacement rolling back staging
owner-scope stability
selection mutation restored after release
explicit selection required before prepare
no RuntimeContent or Logical Actor leaks
```
