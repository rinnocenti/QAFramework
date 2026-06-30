# FXX-CLOSEOUT - COMMON-B Enum / Status Validation Helper

Status: Draft / closeout / docs-only summary
Cut: ARCH-COMMON-B
Date: 2026-06-30

## Helper created

- `Packages/com.immersive.framework/Runtime/Common/FrameworkEnumValidation.cs`
- Namespace: `Immersive.Framework.Common`
- Type: `internal static class FrameworkEnumValidation`

## Call sites migrated

1. `Packages/com.immersive.framework/Runtime/ContentAnchor/ContentAnchorDeclaration.cs`
2. `Packages/com.immersive.framework/Runtime/Preferences/PreferenceReadResult.cs`
3. `Packages/com.immersive.framework/Runtime/UnityInput/UnityInputTargetDescriptor.cs`
4. `Packages/com.immersive.framework/Runtime/TransitionEffects/TransitionEffectSnapshot.cs`

## Behavior preserved

- Validation remains fail-fast in constructors.
- `Unknown` / `None` sentinel rejection remains explicit.
- The same exception family is preserved at the call sites: `ArgumentOutOfRangeException` for invalid enum/status values and `ArgumentException` for non-enum invalid data already guarded by the owning type.
- No public API was added or changed.
- No public enum values were changed or replaced.
- No fallback behavior was introduced.

## Smokes affected

No Unity smoke was run in this cut.

Manual validation should recheck the nearest affected smokes later:

- Content Anchor diagnostics / smoke corresponding to `ContentAnchorDeclaration`
- Preferences smoke corresponding to `PreferenceReadResult`
- `UnityInputTargetOwnershipQaSmokeRunner`
- `UnityInputOfficialComponentEvidenceQaSmokeRunner`
- `TransitionEffectQaSmokeRunner`
- `TransitionEffectPolicyQaSmokeRunner`

## Validation performed

- Reviewed the four pilot call sites before editing.
- Kept the helper internal and mechanical.
- Kept the cut limited to one new helper file, the four pilot call sites, and this closeout document.

## Validation not performed

- Unity compile.
- Unity import.
- Unity smoke execution.
- Playmode or batchmode.

## Manual validation needed

- Confirm the four migrated constructors still throw the same exception type on invalid inputs.
- Confirm the same `paramName` is preserved where the existing constructor used `ArgumentOutOfRangeException`.
- Confirm `IsValid` still rejects sentinel values and now also rejects undefined enum values without altering caller-facing diagnostics.
- Confirm no other enum validation call sites were expanded in this cut.

## Next cuts suggested

1. `COMMON-C` - Defensive copy and empty collection helper.
2. `COMMON-D` - Issue counting helper.
3. `COMMON-E` - Result/status container ADR only after a second concrete use is confirmed.

## Files altered

- `Packages/com.immersive.framework/Runtime/Common/FrameworkEnumValidation.cs`
- `Packages/com.immersive.framework/Runtime/ContentAnchor/ContentAnchorDeclaration.cs`
- `Packages/com.immersive.framework/Runtime/Preferences/PreferenceReadResult.cs`
- `Packages/com.immersive.framework/Runtime/UnityInput/UnityInputTargetDescriptor.cs`
- `Packages/com.immersive.framework/Runtime/TransitionEffects/TransitionEffectSnapshot.cs`
- `Assets/_Documentation/Closeouts/FXX-CLOSEOUT-COMMON-B-Enum-Status-Validation-Helper.md`
