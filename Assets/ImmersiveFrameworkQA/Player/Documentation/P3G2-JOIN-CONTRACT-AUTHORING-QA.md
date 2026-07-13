
# P3G.2 — Join Contract and Authoring QA

## Purpose

Prove the passive manual join contracts and `LocalPlayerProvisioningAuthoring` validation before any `PlayerInputManager.JoinPlayer` side effect exists.

## Run

Do not enter Play Mode.

```text
Immersive Framework
  QA
    Player
      P3G.2 Run Join Contract Authoring Smoke
```

## Expected

```text
[P3G2_JOIN_CONTRACT_AUTHORING_SMOKE] status='Passed' cases='14'
```

The smoke creates only temporary in-memory objects and destroys them in `finally`.
