# P3K.7G — Same-Route Activity Lifecycle Admission QA

## Menu

```text
Immersive Framework
  > QA
    > Player
      > P3K.7G Run Same-Route Activity Lifecycle Admission Smoke
```

Run in a fresh Play Mode session. The smoke waits for the provisioning runtime
and framework boot to become ready.

## Expected

```text
[P3K7G_SAME_ROUTE_ACTIVITY_LIFECYCLE_ADMISSION_SMOKE]
status='Passed'
cases='40'
```

## Proof

The smoke uses only official host operations for the Activity switch:

```text
FrameworkRuntimeHost.RequestActivityAsync
FrameworkRuntimeHost.ClearActivityAsync
```

It does not manually stage candidates or invoke P3K.7E.

Coverage includes:

```text
real framework boot readiness
real joined local Player
current Activity-owned P3J Actor and P3K.2-P3K.5 chain
real GameplayReady target Activity authoring
same-Route request through GameFlowRuntime
pre-transition ReadyToCommit transaction
transition authorization
target lifecycle activation gate
P3K.7E commit
previous P3J.6 exit superseded by handoff
target P3J.6 adoption
exact promoted preparation/admission tokens
previous Actor is immediately inactive after commit
previous physical Actor destruction finalizes at the next Unity frame boundary
normal target Activity clear
P3K chain release before P3J Actor release
stable Session host and PlayerInput preservation
RuntimeContent scope cleanup
public contracts without Unity references
```

## Unity destruction timing

`PlayerActorMaterializationAdapter` uses the normal Unity destruction path.
The handoff release operation and RuntimeContent handle cleanup complete
synchronously, but the native physical object reaches Unity's null state at the
next frame boundary.

The smoke therefore verifies:

```text
same request frame
  -> previous Actor is no longer active

Awaitable.NextFrameAsync
  -> previous Actor compares null
```

This matches the established P3K.7D physical-release proof and does not weaken
the no-two-active-Players contract.
