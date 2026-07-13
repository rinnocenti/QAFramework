# P3F.1 — Session Slot Runtime QA

## Menu

```text
Immersive Framework > QA > Player > P3F Run Session Slot Runtime Smoke
```

Run in Edit Mode. The smoke creates only transient in-memory `PlayerSlotProfile` fixtures.

## Expected log

```text
[P3F_SESSION_SLOT_RUNTIME_SMOKE] status='Passed' cases='17'
```

## Coverage

```text
ordered roster initialization
closed join rejection
explicit join opening
first-available configured order
capacity consumed by Reserved and Joined
reservation release
stale and foreign reservation rejection
mark Joined
non-destructive capacity reduction
capacity increase
invalid duplicate Profile references
invalid duplicate PlayerSlotId
invalid initial capacity
Profile immutability
```

## Out of scope

```text
FrameworkRuntimeHost composition
PlayerInputManager
local Player provisioning
Actor selection/materialization
Activity admission
FIRSTGAME
```
