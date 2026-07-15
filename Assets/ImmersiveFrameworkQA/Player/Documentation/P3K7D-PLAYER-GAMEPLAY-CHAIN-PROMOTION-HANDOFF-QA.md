# P3K.7D Player Gameplay Chain Promotion and Handoff QA

## Runtime smoke

Use a fresh Play Mode session after normal Framework boot:

```text
Immersive Framework
  > QA
    > Player
      > P3K.7D Run Player Gameplay Chain Promotion Handoff Smoke
```

Expected:

```text
[P3K7D_PLAYER_GAMEPLAY_CHAIN_PROMOTION_HANDOFF_SMOKE]
status='Passed'
cases='52'
```

## Proof shape

The smoke uses the real QA provisioning fixture:

```text
real PlayerInputManager join
explicit default Actor selection
real P3J current Actor preparation
real P3K.2-P3K.5 current gameplay chain
real P3K.7C inactive target candidate
```

It then executes two handoffs.

### Reversible failure

The endpoint source rejects only the first candidate ActorId after P3J cutover.
The runtime must:

```text
release candidate partial chain
restore previous P3J Actor and preparation token
return candidate to staged inactive ownership
rebuild previous P3K chain with new functional tokens
```

### Commit

A second candidate uses valid explicit endpoints. The runtime must:

```text
release the restored current chain
promote the second candidate
create the candidate P3K chain
complete candidate ownership
release the previous physical Actor
reject rollback after commit
```

The smoke is one-shot because local Player leave is outside this cut. Re-enter
Play Mode before repeating it.
