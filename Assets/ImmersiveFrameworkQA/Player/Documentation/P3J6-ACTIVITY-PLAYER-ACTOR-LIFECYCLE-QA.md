# P3J.6 — Activity Player Actor Lifecycle QA

## Setup

Outside Play Mode:

```text
Immersive Framework
  > QA
    > Player
      > P3J.6 Apply Activity Player Actor Lifecycle Fixture
```

The setup is idempotent. It reuses the P3J.5 real join fixture and creates:

```text
positive Activity
  AllJoinedSlots
  zero participants rejected
  LogicalActorsPrepared

negative Activity
  ExplicitSlots: configured Slot 2
  LogicalActorsPrepared
  Slot 2 intentionally remains unjoined
```

## Runtime smoke

Enter Play Mode using normal Framework startup, wait for boot completion and run:

```text
Immersive Framework
  > QA
    > Player
      > P3J.6 Run Activity Player Actor Lifecycle Smoke
```

Expected:

```text
[P3J6_ACTIVITY_PLAYER_ACTOR_LIFECYCLE_SMOKE]
status='Passed' cases='20'
```

The smoke proves:

```text
real PlayerInputManager join
join remains unselected and unprepared
real Activity request selects the default Actor
Activity owner prepares and activates the Logical Actor
required participant contributes Ready state
restart releases old Actor before re-entry
restart creates a new ActorId, RuntimeContent identity and preparation token
stale preparation token is rejected without disturbing the restarted Actor
clear releases Activity-owned Actor
Session host, PlayerInput, Slot and selection are preserved
no preparation or retained release leaks
explicit unjoined required Slot produces NotReady
failed entry leaves no partial Logical Actor
```

Run in a fresh Play Mode because local Player leave remains outside this cut.
