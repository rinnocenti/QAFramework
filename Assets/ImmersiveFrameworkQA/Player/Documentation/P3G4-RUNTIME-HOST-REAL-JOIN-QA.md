# P3G.4 — Runtime Host real local Player join QA

## Setup

Run outside Play Mode:

```text
Immersive Framework/QA/Player/P3G.4 Apply Real Join Fixture
```

The idempotent setup creates or updates:

```text
Assets/ImmersiveFrameworkQA/Player/P3G4/P3G4_InputActions.asset
Assets/ImmersiveFrameworkQA/Player/P3G4/P3G4_LocalPlayerHost.prefab
```

It installs or reuses the single `PlayerInputManager` in `QA_UIGlobal`, configures manual
join, and adds `LocalPlayerProvisioningAuthoring` with an explicit manager reference.

Expected setup log:

```text
[P3G4_RUNTIME_JOIN_FIXTURE_SETUP] status='Applied'
```

## Smoke

Enter Play Mode using normal Framework startup. Then run:

```text
Immersive Framework/QA/Player/P3G.4 Run Runtime Host Real Join Smoke
```

Expected:

```text
[P3G4_RUNTIME_HOST_REAL_JOIN_SMOKE] status='Passed' cases='18'
```

The smoke proves:

```text
runtime authoring binding
manual PlayerInputManager configuration
typed Invoke C# Events join notification configuration
explicit join-window opening
real PlayerInputManager.JoinPlayer execution
joined callback correlation
real PlayerInput scene instance
PlayerActorDeclaration evidence
playerIndex remains diagnostic
first configured Slot becomes Joined
reservation and commit evidence
stable Session context
manager and PlayerInput global evidence
non-destructive join-window closing
```

## Execution note

The smoke intentionally leaves one admitted local Player because leave and release are
not part of P3G.4. Re-enter Play Mode before running it again.
