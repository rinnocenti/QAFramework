# P3C — Player Profile Authoring QA

## Objective

Prove the P3C Player Slot and participation requirements authoring foundation without creating runtime Player state or permanent QA assets.

## Execution

```text
Immersive Framework
  > QA
    > Player
      > P3C Run Player Profile Authoring Smoke
```

## Cases

```text
profile-identity-normalized
ordered-game-application-configuration
valid-configuration-accepted
explicit-none-requirements-profile
validation-is-non-mutating
missing-profile-reference-rejected
repeated-profile-reference-rejected
duplicate-identity-rejected
empty-identity-rejected
complete-template-set-created
```

## Fixture policy

The smoke creates assets only under:

```text
Assets/ImmersiveFrameworkQA/__P3C_PlayerProfileAuthoring_Temp
```

The folder is deleted in `finally`, on PASS or failure.

The smoke does not create or modify:

```text
permanent QA scenes
FIRSTGAME content
framework package files
Project Settings
runtime Session state
```

## Validation boundary

The package authoring validator is internal Editor tooling. QA invokes its exact methods through Editor-only reflection so the package does not expose a public test API solely for QA.

Runtime reflection is not introduced.

## Expected PASS

```text
[P3C_PLAYER_PROFILE_AUTHORING_SMOKE] status='Passed' cases='10' completed='profile-identity-normalized,ordered-game-application-configuration,valid-configuration-accepted,explicit-none-requirements-profile,validation-is-non-mutating,missing-profile-reference-rejected,repeated-profile-reference-rejected,duplicate-identity-rejected,empty-identity-rejected,complete-template-set-created'.
```

## Failure behavior

The smoke logs:

```text
status='Failed'
exception='<type>'
message='<explicit failure>'
completed='<cases completed before failure>'
```

It then rethrows the exception so the failure remains visible in Unity.

## Suggested commit

```text
P3C.4 — add Player Profile authoring QA smoke
```
