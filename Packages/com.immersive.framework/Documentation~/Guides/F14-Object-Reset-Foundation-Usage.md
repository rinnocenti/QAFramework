# F14 — Object Reset Foundation Usage

Status: F14 closed/applied.

This guide describes only the logical Object Reset foundation. It does not describe Transform, Rigidbody, Animator, Player, Actor, pool or gameplay reset adapters.

## Authoring path

Use this shape for a GameObject that should be a future reset target:

```text
GameObject
  Object Entry Declaration
  Object Reset Trigger
  Optional: Object Reset Trigger Unity Event Bridge
```

Recommended trigger setup:

```text
Object Reset Trigger
  Target Declaration = the Object Entry Declaration on the same object
  Reason = a stable diagnostic reason
  Allow No Participants = true while F15 adapters do not exist
```

A UGUI Button can call:

```text
ObjectResetTrigger.RequestObjectReset()
```

The UnityEvent bridge is optional. Add it only when Inspector callbacks are needed.

## Expected result before F15

Until physical Unity adapters exist, a valid trigger may complete as:

```text
SucceededNoParticipants
```

That is not a failure. It means the target was found in the current Object Entry snapshot, but no concrete reset participants were registered.

## Common authoring failure

If the trigger reports:

```text
Object Entry target was not found in the current snapshot
```

Check:

```text
Object Entry Declaration scope matches the current lifecycle owner;
Activity entries point to the active Activity asset;
Route entries point to the active Route asset;
the trigger references the current declaration instead of a stale Object Entry Id;
the declaration is in a loaded scene collected by the current snapshot.
```

## Final F14 smoke

Run:

```text
Run Object Reset Foundation Closure Smoke
```

The expected final evidence is:

```text
snapshotAvailable='True'
targetCollected='True'
targetResolved='True'
hostStatus='Succeeded'
hostParticipants='2'
hostParticipantSucceeded='1'
hostParticipantSkipped='1'
triggerStatus='SucceededNoParticipants'
bridgeSubmitted='1'
bridgeSucceeded='1'
bridgeSucceededNoParticipants='1'
bridgeCompleted='1'
blockingIssues='0'
nonBlockingIssues='0'
```
