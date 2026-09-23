# QA-M07-INTERNAL-FIX1 — Assembly boundaries

This fix is applied on top of:

```text
QA-M07-INTERNAL-player-reconcile-authority-and-idempotence
```

## Errors corrected

```text
CS0234 UnityEngine.InputSystem missing
CS0234 ImmersiveFrameworkQA.Player.Editor missing
CS0246 PlayerInputManager missing
```

## Cause

`GameFlow/InternalEditor` does not compile against the Input System assembly and
does not reference the separate Player Editor assembly. Existing canonical QA
code already crosses these internal Editor-only boundaries through reflection.

## Changes

### `QaM07InternalReconcileSetup.cs`

- removes the compile-time `ImmersiveFrameworkQA.Player.Editor` import;
- resolves `QaP3J5RuntimeHostPreparationSetup` by exact loaded type name;
- invokes its parameterless internal `Apply` method through Editor-only reflection;
- unwraps `TargetInvocationException`.

### `QaM07InternalReconcileRegression.cs`

- removes `UnityEngine.InputSystem`;
- removes the compile-time `PlayerInputManager` type;
- resolves the `PlayerInputManager` property from the scoped
  `LocalPlayerProvisioningAuthoring`;
- treats the result as a Unity `Component` only when scoped hierarchy traversal
  is required;
- reads `playerCount` and `joiningEnabled` through typed reflection checks.

## Architecture preserved

```text
no asmdef edit
no new Input System dependency
no global object lookup
no runtime package reflection
no new QA assembly
no scene or prefab changes
```

## Apply

Replace the two complete `.cs` files from this ZIP. Existing `.meta` files remain
unchanged.

Then allow Unity to recompile.
