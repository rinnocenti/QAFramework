# P3B — PlayerComposer Minimal Materialization QA

## Objective

Protect the simplified `PlayerComposer` product contract with a deterministic editor smoke in QAFramework.

## Type

Technical QA.

## Files

### Created

```text
Assets/ImmersiveFrameworkQA/Player/Editor/QaP3BPlayerComposerMinimalMaterializationSmoke.cs
Assets/ImmersiveFrameworkQA/Player/Documentation/P3B-PLAYER-COMPOSER-MINIMAL-QA.md
```

## Execution

Unity menu:

```text
Immersive Framework
  QA
    Player
      P3B Run PlayerComposer Minimal Materialization Smoke
```

## Cases

1. Nominal Player with camera binding enabled.
2. Second Apply/Rebuild is idempotent.
3. Camera binding disabled creates no anchors.
4. Invalid authored default action map fails explicitly.
5. Required camera targets with automatic creation disabled fail explicitly.
6. `UseFollowTarget` reuses `CameraTarget` without a separate LookAt object.
7. Legacy Player materialization is removed when legacy types are present.

## Canonical assertions

- `PlayerActorDeclaration` exists on the Player root.
- `PlayerSlotDeclaration` exists on the Player root.
- Both declarations match `PlayerComposer`.
- Gate references the typed `PlayerInput`.
- Gate references the typed `PlayerSlotDeclaration`.
- Gate map equals the authored default map.
- `PlayerInput.defaultActionMap` receives the authored default.
- No legacy binding components remain.
- Empty `_Framework/_Bindings` is removed.
- Camera anchors are children of the logical Player.
- No permanent scene or asset is created.

## PASS log

```text
[P3B_PLAYER_COMPOSER_MINIMAL_SMOKE] status='Passed'
```

## Suggested commit

```text
P3B — add PlayerComposer minimal materialization QA
```
