# Cut A — InputMode Runtime Authority QA

## Objective

Prove the IC2/IC3 architecture:

```text
one resident logical InputMode state
one in-flight posture transaction
exact commit / rollback
one active Pause InputAction submitter
IC1 physical writer boundary preserved
```

## Scope

- `InputModeRuntimeContext` pure runtime contract;
- monotonic transaction sequence;
- concurrent request rejection;
- idempotent same-mode request;
- exact stale transaction rejection;
- rollback without logical state mutation;
- canonical Pause trigger delegation;
- legacy direct Pause trigger inert and hidden;
- source-level preservation of the IC1 writer boundary.

## Out of scope

- changing Player gameplay binding ownership;
- PlayerInputManager provisioning;
- gameplay command reading;
- frontend menu product authoring;
- deleting the removed trigger tombstone;
- FIRSTGAME integration.

## Run

Use Edit Mode:

```text
Immersive Framework
> QA
> Input Mode
> IC2 Run Runtime Authority Smoke
```

Expected:

```text
[IC2_INPUT_MODE_RUNTIME_AUTHORITY_SMOKE]
status='Passed'
cases='18'
```

The smoke creates only pure runtime state and reads package source for ownership
assertions. It does not modify scenes, assets or Project Settings.

## Required regression after IC2

Enter Play Mode and rerun:

```text
Immersive Framework
> QA
> Unity Input
> IC1 Run PlayerInput Single Writer Smoke
```

Expected:

```text
[IC1_PLAYER_INPUT_SINGLE_WRITER_SMOKE]
status='Passed'
cases='14'
```

In the same fresh Play Mode session, run:

```text
Immersive Framework
> QA
> Input Mode
> IC2 Run Pause Runtime Regression Smoke
```

Expected:

```text
[IC2_PAUSE_INPUT_MODE_RUNTIME_REGRESSION_SMOKE]
status='Passed'
cases='3'
```

This wrapper invokes the existing package `Pause Runtime PlayerInput Bridge`
regression. It still proves Pause, Resume, idempotency, preflight failures and
PlayerInput action-map application through the current canonical bridge.

## Acceptance

```text
IC2 smoke passes 18 cases
IC1 smoke remains 14/14
framework and QA compile in Unity 6.5
no package-owned direct PlayerInput mutation outside UnityPlayerInputStateWriter
legacy PauseInputActionTrigger subscribes to no InputAction
canonical PauseInputActionRuntimeBridgeTrigger delegates to the bridge
resident state remains coherent after Pause and Resume regression
```
