# Q3 — QA-M07-INTERNAL
## Player reconcile authority and idempotence

**Type:** internal Play Mode regression  
**Package prerequisite:** `IF-M07-10 — Player readiness contribution and delta reconcile`  
**QA baseline:** `cdf05e5d12a3eea65a415f153fd33306b77ef210`

## Menus

### 1. Prepare outside Play Mode

```text
Immersive Framework
  > QA
    > Setup
      > Player
        > M07 Prepare Internal Reconcile Regression
```

The setup is idempotent. It:

- restores the canonical QA Hub startup;
- applies the existing P3J.5/P3H.4 real Local Player fixture;
- requires two configured Local Player Slots;
- assigns the existing alternate Actor to Slot 02;
- creates one valid Q3 replacement ActorProfile;
- validates the existing direct-readiness content scene and Build Settings;
- does not create a scene, prefab, runtime manager or ProjectSettings replacement.

### 2. Run in a fresh Play Mode

```text
Immersive Framework
  > QA
    > Regressions
      > Player
        > M07 Run Internal Reconcile Authority Regression
```

Expected terminal log:

```text
[QA_M07_INTERNAL]
status='Passed'
cases='54'
proof='Owner,RevisionCoalescing,OneActorPerSlot,DeltaRollback,ExitWaiting,ExitReady,Replacement,Reentry'
```

## Proven flows

### Exit while Waiting

```text
ObserveOnly
+ Explicit Slot
+ JoinedSlots
+ Slot not Joined
→ lifecycle SucceededEnteredPreparing
→ Player contribution Preparing
→ clear Activity
→ contribution Released
→ no Actor and no Session mutation
```

### Delta rollback

```text
WaitVisible
+ Explicit Slot
+ LogicalActorsPrepared
→ late Join
→ in-memory valid-id ActorProfile with missing logical prefab
→ selection succeeds
→ preparation fails
→ reconcile FailedPreparation
→ rollback clears only the new selection
→ no prepared Actor leak
```

The invalid profile is a transient runtime clone. The persisted PlayerSlotProfile is restored in `finally`.

### Main reconcile

```text
WaitVisible
+ Explicit Slot 01 + Slot 02
+ GameplayReady
→ Slot 01 already Joined
→ Slot 02 unjoined
→ pre-delta reconcile = SucceededNoChange
→ exact foreign Activity/owner/occurrence rejected
→ Session revision changes
→ Slot 01 selected/prepared/admitted
→ Slot 02 remains WaitingForJoin
→ repeated reconcile = SucceededNoChange
→ Slot 01 selection replaced
→ reconcile replaces Actor/gameplay evidence without duplication
→ Slot 02 joins
→ reconcile = SucceededCompleted
→ Activity Ready
→ repeated completed reconcile = SucceededNoChange
→ exit releases Activity-owned Actor/gameplay tokens
→ Host, PlayerInput, Joined Slots and selections remain Session-owned
```

### Reentry

```text
LogicalActorsPrepared
+ both Slots Joined
→ first occurrence prepares two Actors
→ exit releases both
→ same Activity enters again
→ occurrence advances
→ ActorId and preparation token are renewed
→ second exit leaves no preparation
```

## Internal access boundary

This is Q3, not Q4. Reflection is limited to Editor-only QA access to package-internal host-scoped operations:

- preparation/gameplay module resolution from the already resolved `FrameworkRuntimeHost`;
- explicit reconcile;
- exact lifecycle/preparation/gameplay snapshots;
- selection replacement;
- opening/closing the real joining gate.

There is no global object lookup, runtime package reflection, service locator, timeout, frame polling or log parsing.

## Expected blocker behavior

The replacement case intentionally asserts the product contract. If the current
`IF-M07-10` implementation still calls only `TryPrepareSelectedActor` after a
selection revision changes, the regression should fail at:

```text
replacement-reconcile-proved
```

with `FailedPreparation` / prepared-Actor conflict. That is a package defect,
not a QA defect: replacement requires an explicit delta reconcile path that
releases/replaces the old gameplay and Actor ownership safely.

## Required execution order

```text
1. Package import/compile.
2. QA_CAUSAL_ASYNC_FOUNDATION — 20 cases.
3. Q3 setup.
4. Fresh Play Mode.
5. Q3 regression — 54 cases.
6. Exit Play Mode and confirm canonical QA Hub restoration.
```
