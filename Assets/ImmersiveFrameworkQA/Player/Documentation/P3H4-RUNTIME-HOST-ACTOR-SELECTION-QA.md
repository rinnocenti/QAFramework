# P3H.4 Runtime Host Actor Selection QA

## Authoring smoke

Outside Play Mode:

```text
Immersive Framework/QA/Player/P3H.4 Run Game Application Policy Authoring Smoke
```

Expected: `[P3H4_GAME_APPLICATION_POLICY_AUTHORING_SMOKE] status='Passed' cases='6'`.

## Setup

Outside Play Mode:

```text
Immersive Framework/QA/Player/P3H.4 Apply Runtime Host Actor Selection Fixture
```

This command is idempotent. It applies the P3G.4 real join fixture, creates two valid Player Actor Profiles, creates a `UniqueAcrossJoinedSlots` policy, assigns the policy to the active GameApplication, and assigns the first Slot default Actor.

## Runtime smoke

Enter Play Mode with normal Framework startup and wait for boot success. Then run:

```text
Immersive Framework/QA/Player/P3H.4 Run Runtime Host Actor Selection Smoke
```

Expected:

```text
[P3H4_RUNTIME_HOST_ACTOR_SELECTION_SMOKE] status='Passed' cases='13'
```

The smoke proves policy composition, real join without implicit selection, explicit default selection, typed select/replace/clear operations, stable Session identity and host snapshot evidence.

## Regressions

After applying the P3H.4 fixture, the following prior smokes remain applicable:

```text
P3F.2 Run Runtime Host Integration Smoke
P3G.4 Run Runtime Host Real Join Smoke
P3H.3 Run Actor Selection Runtime Smoke
```

Run each in a fresh Play Mode where required by its one-shot join rule.
