# FXX-CLOSEOUT - COMMON-E Common Internal Mechanics Track Closeout

Status: Closed / track closeout / docs-only summary
Track: Track 1 - Common internal mechanics
Date: 2026-06-30

## 1. Track status

Track 1 is closed at the documentation level.

The three bounded Common mechanics identified in `COMMON-A` were consolidated as internal, additive helpers in `Runtime/Common`:

1. Enum / status validation.
2. Defensive copy / empty collection handling.
3. Issue counting / blocking issue counting.

This closeout records the decision boundary only. It does not authorize broader Common expansion.

## 2. Helpers created

### FrameworkEnumValidation

- `Packages/com.immersive.framework/Runtime/Common/FrameworkEnumValidation.cs`
- Type: `internal static class FrameworkEnumValidation`
- Namespace: `Immersive.Framework.Common`

Pilot call sites:

1. `Packages/com.immersive.framework/Runtime/ContentAnchor/ContentAnchorDeclaration.cs`
2. `Packages/com.immersive.framework/Runtime/Preferences/PreferenceReadResult.cs`
3. `Packages/com.immersive.framework/Runtime/UnityInput/UnityInputTargetDescriptor.cs`
4. `Packages/com.immersive.framework/Runtime/TransitionEffects/TransitionEffectSnapshot.cs`

### FrameworkCollectionCopy

- `Packages/com.immersive.framework/Runtime/Common/FrameworkCollectionCopy.cs`
- Type: `internal static class FrameworkCollectionCopy`
- Namespace: `Immersive.Framework.Common`

Pilot call sites:

1. `Packages/com.immersive.framework/Runtime/Gate/GateSnapshot.cs`
2. `Packages/com.immersive.framework/Runtime/ActivityFlow/ActivityContentExecutionPhasePlan.cs`
3. `Packages/com.immersive.framework/Runtime/GlobalUi/GlobalUiSceneRuntime.cs`
4. `Packages/com.immersive.framework/Runtime/Loading/LoadingProgressAggregationResult.cs`

### FrameworkIssueCounting

- `Packages/com.immersive.framework/Runtime/Common/FrameworkIssueCounting.cs`
- Type: `internal static class FrameworkIssueCounting`
- Namespace: `Immersive.Framework.Common`

Pilot call sites:

1. `Packages/com.immersive.framework/Runtime/ActivityFlow/ActivityContentExecutionAggregateResult.cs`
2. `Packages/com.immersive.framework/Runtime/InputMode/InputModeUnityPlayerInputRequestApplicationResult.cs`
3. `Packages/com.immersive.framework/Runtime/Pause/PauseResult.cs`
4. `Packages/com.immersive.framework/Runtime/Gate/GateEvaluationResult.cs`

## 3. Mechanical contract confirmed

The Common helpers are intentionally:

- `internal`
- mechanical only
- additive
- non-public
- free of domain semantics

Confirmed exclusions:

- No public API was added.
- No public enum was replaced.
- No public result type was replaced.
- No lifecycle policy moved into Common.
- No Route / Activity / Pause / RuntimeContent / ContentAnchor / InputMode semantics moved into Common.

## 4. FrameworkIssueCounting.Sum note

`FrameworkIssueCounting.Sum` is accepted only as an internal aggregation primitive to preserve exact count parity in the `InputMode` aggregate pilot.

It does not authorize domain-specific severity logic in Common.

Common may sum counts mechanically.
Common must not decide what is blocking, failed, warning, fatal, required or optional.

## 5. What was not consolidated

Explicitly out of scope and not consolidated in Track 1:

- `OperationResult<TStatus>`
- result/status shells
- public status enums
- public result types
- lifecycle policy
- Route semantics
- Activity semantics
- Pause semantics
- RuntimeContent semantics
- ContentAnchor semantics
- InputMode semantics

## 6. Decision on OperationResult<TStatus>

`OperationResult<TStatus>` is not implemented now.

It requires:

- a separate ADR.
- at least two concrete call sites that are truly identical in shape.
- explicit agreement that the helper remains mechanical and does not become a domain policy container.

Until that happens, `OperationResult<TStatus>` remains blocked.

## 7. Validation pending

No Unity compile, import or smoke was run in this cut.

Pending validation should cover the helpers and the affected module smokes from COMMON-B, COMMON-C and COMMON-D:

- Content Anchor diagnostics / smoke corresponding to `ContentAnchorDeclaration`, if present
- Preferences smoke corresponding to `PreferenceReadResult`, if present
- `UnityInputTargetOwnershipQaSmokeRunner`
- `UnityInputOfficialComponentEvidenceQaSmokeRunner`
- `TransitionEffectQaSmokeRunner`
- `TransitionEffectPolicyQaSmokeRunner`
- `GateAdmissionQaSmokeRunner`
- Activity Content Execution smoke corresponding to `ActivityContentExecutionPhasePlan` and `ActivityContentExecutionAggregateResult`, if present
- `GlobalUi` smoke corresponding to `GlobalUiSceneRuntime`, if present
- `LoadingProgressQaSmokeRunner`
- `LoadingObservationQaSmokeRunner`
- `InputModeUnityPlayerInputRequestApplicationQaSmokeRunner`
- `PauseRuntimeRequestQaSmokeRunner`

## 8. Risks remaining

- Common can drift into a semantic dump if future cuts ignore the mechanical boundary.
- `Sum` could be misread as a general-purpose abstraction if future callers are not reviewed carefully.
- Result/status shell consolidation still needs a separate ADR and stronger evidence than the current Common track.
- Smokes have not been re-run yet, so parity is documented but not empirically revalidated in Unity.

## 9. Recommended next track

Recommended next track: Participant consolidation.

Start with `CONS-A` and `CONS-B` only if the existing ADR and plan remain accepted and current.

This recommendation does not auto-authorize implementation.

## 10. Files altered in this cut

- `Assets/_Documentation/Closeouts/FXX-CLOSEOUT-COMMON-E-Common-Internal-Mechanics-Track-Closeout.md`
