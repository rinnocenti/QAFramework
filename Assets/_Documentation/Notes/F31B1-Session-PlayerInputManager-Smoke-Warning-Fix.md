# F31B1 — Session PlayerInputManager Smoke Warning Fix

## Status

Closed / compile-warning cleanup.

## Context

After F31B, Unity reported compiler warning CS1718 in `SessionPlayerInputManagerBoundaryQaSmokeRunner.cs` because the smoke contract step compared `UnityInputPlayerInputManagerScope.Session` to itself.

## Decision

Replace the self-comparison with an explicit scope validity check:

```text
expectedScope = Session
passed = expectedScope != Unknown
```

This keeps the smoke semantics intact while removing the redundant comparison.

## Runtime behavior

No runtime behavior changes.

F31B remains evidence-only:

```text
PlayerInputManager = Unity official Session-level input infrastructure
Framework = evidence, validation and lifecycle integration
No custom input manager
No player join
No prefab spawn
No action-map switching
```

## Next direction

The next input work may continue from the now-clean canonical references:

```text
SessionPlayerInputManagerDeclaration
PlayerActorDeclaration : IActor + PlayerInput evidence
UnityInputTargetDeclaration
InputModeRequest / PauseInputModeRequestMapper
```

The next implementation should apply InputMode through Unity official components/adapters, not through a framework-owned input manager.
