# P3M5B — Route Transition and Expanded Negative Matrix QA

## Type

```text
QA technical integration
```

## Objective

Prove Scene Local Player admission and external Actor adoption across real Route
replacement and Route re-entry, then validate the automatic-authoring negative
matrix without changing the active Route or Activity.

## Product/runtime path

```text
FrameworkRuntimeHost.RequestRouteAsync
-> RouteLifecycleRuntime
-> previous Activity exit and reverse release
-> previous Route primary scene replacement
-> target Route Startup Activity
-> target Activity scene composition
-> Scene Local Player admission
-> Actor selection and ExternalSceneOwned adoption
-> canonical Player Actor preparation
```

The smoke does not call `IActivityContentExecutionParticipant` directly.

## Fixture

Outside Play Mode run:

```text
Immersive Framework
> QA
> Player
> P3M5B Apply Route Transition and Negative Matrix Fixture
```

The setup creates two real Routes:

```text
P3M5B Route A
  primary scene with one valid but undeclared Scene Player surface
  Startup Activity with one declared Scene Player Activity scene

P3M5B Route B
  empty primary scene
  Startup Activity with one declared Scene Player Activity scene
```

It also creates four negative Activity scenes:

```text
duplicate automatic surfaces for one Slot
missing Scene Logical Player Actor
Actor evidence incompatible with selected ActorProfile
same Local Player Host reused by two Slot surfaces
```

The Route A primary scene surface is used to prove that loaded surfaces outside
an Activity's declared content are ignored rather than admitted.

## Activity identity

Every generated Activity asset receives one explicit stable `activityId`. The
fixture updates existing assets idempotently, so reapplying it repairs assets
created by the initial P3M5B setup. Display names and asset filenames are not
used as Activity scene ownership identity.

```text
qa.p3m5b.activity.route-a.startup
qa.p3m5b.activity.route-b.startup
qa.p3m5b.activity.negative.duplicate-slot
qa.p3m5b.activity.negative.missing-actor
qa.p3m5b.activity.negative.mismatched-profile
qa.p3m5b.activity.negative.reused-host
qa.p3m5b.activity.negative.undeclared-surface
```

## Smoke

Run in a fresh Play Mode session:

```text
Immersive Framework
> QA
> Player
> P3M5B Run Route Transition and Negative Matrix Smoke
```

Expected:

```text
[P3M5B_ROUTE_TRANSITION_NEGATIVE_MATRIX_SMOKE]
status='Passed'
cases='27'
```

## Coverage

```text
real Route A request
Route A Startup Activity automatic admission
Activity-owned preparation authority
Route A -> Route B replacement
previous Activity scenes released
previous Activity RuntimeContent owner removed
Route B automatic admission
fresh Actor/adoption/RuntimeContent identities
exactly one active admission after Route replacement
Route B -> Route A re-entry
fresh Route A re-entry identities
negative duplicate Slot rejection
negative missing Actor rejection
negative mismatched ActorProfile evidence rejection
negative reused Host rejection
loaded undeclared surface ignored
negative scenes leave no admission
active Route/Activity/token preserved after negative matrix
original Route restored
final Session state clean
```

## Scope boundary

This cut does not change or validate the broader Route/Activity teardown-order
audit. It does not introduce `Transitioning`, commit-after-exit, reverse
participant order, or a new Route rollback model.

## Acceptance

```text
Every generated Activity has a valid explicit ActivityId.
Route replacement compiles and completes through FrameworkRuntimeHost.
Only one Scene Local Player admission is active after each Route switch.
Previous Activity scene, preparation, adoption and RuntimeContent owner do not remain.
Route re-entry creates new runtime identities.
Every negative case is rejected explicitly at automatic-authoring resolution.
Undeclared loaded surfaces are ignored.
The original Route is restored before PASS.
```
