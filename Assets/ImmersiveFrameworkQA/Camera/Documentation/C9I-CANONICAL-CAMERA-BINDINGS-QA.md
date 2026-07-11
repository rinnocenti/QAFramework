# C9I — Canonical Route/Activity Camera Bindings QA

## Objective

Prove the real scene-authored integration:

```text
RouteContentBinding
-> RouteCameraRequestBinding

ActivityLocalVisibilityAdapter
-> ActivityCameraRequestBinding
```

No C9H adapter is called manually.

## Installer

```text
Immersive Framework QA/Camera/C9I Install Canonical Camera Bindings QA
```

## Runtime sequence

```text
1. Route enters through canonical lifecycle.
2. Startup Activity enters through canonical lifecycle.
3. Activity clear uses ActivityRequestTrigger.
4. Route camera is restored.
5. Activity request restores Activity.
6. Activity clear leaves Route-only state.
7. RouteRequestTrigger returns to Hub.
8. Route exit probe verifies binding release and empty output.
9. Persistent completion emits final PASS.
```

## Expected PASS

```text
[QA][C9I Canonical Camera Bindings] PASS. status='Passed' cases='7'
```

The PASS is emitted in the Hub after Route exit evidence is captured.

## Suggested commit

```text
QA: prove C9I canonical Route Activity camera bindings
```

## Synchronization rule

The fixture does not assume `Start()` runs after Route startup. It waits until all canonical bindings have left `NotEntered`, with an explicit timeout.

## Route exit ordering

`RouteContentRuntime` dispatches enter parent-first and exit in reverse order. The installer creates the exit probe before the Route camera binding so reverse dispatch releases the request first and records evidence afterward.


## Route completion synchronization

The fixture does not start from `Start()`.

```text
Hub RouteRequestTrigger Submitted
-> coordinator persists

Hub RouteRequestTrigger Completed/Succeeded
-> transition gate is released
-> coordinator resolves target fixture
-> fixture.Begin()
```

This prevents Activity requests from being submitted while the Route transition blocker is active.


## Root persistence correction

The route completion coordinator is installed on a dedicated root GameObject:

```text
QA_C9I_RouteCompletionCoordinator
```

It holds an explicit reference to the Hub `RouteRequestTrigger`.

This is required because Unity only permits `DontDestroyOnLoad` for root GameObjects or components on root GameObjects.


## Closure and obsolete QA removal

C9I passed all eight canonical lifecycle cases.

The previous C9H synthetic lifecycle-adapter smoke is removed because:

```text
C9I exercises the canonical RouteContentRuntime dispatcher
C9I exercises the canonical ActivityContentRuntime dispatcher
C9I proves real Activity clear/re-enter
C9I proves real Route exit cleanup
```

C9H must not remain as a parallel or preferred lifecycle integration path.
