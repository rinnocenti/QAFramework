# FXX-AUDIT - Common Internal Mechanics Repetition Inventory

Status: Draft / audit-only / documentation
Scope: `Packages/com.immersive.framework/Runtime` only
Last updated: 2026-06-30

## Executive summary

`Runtime/Common` is currently very small: in this snapshot it only contains `FrameworkStringExtensions` (`NormalizeText*` and `ToDiagnosticText*`). The rest of `Runtime` still repeats several internal mechanics that are mechanical, not semantic.

Three families are ready to become internal Common helpers later, provided they stay additive and internal:

1. Enum/status validation.
2. Defensive copy / empty collection handling.
3. Issue counting / blocking issue counting.

Result/status shells are also repeated, but the shape is only similar, not identical. Those should not be extracted to Common yet without a dedicated ADR because they start to carry domain vocabulary and policy.

Route, Activity, Pause, RuntimeContent, ContentAnchor and InputMode semantics are not Common concerns. Those mechanics should stay in their owning modules even when they reuse the same low-level shape.

## Inventory table

| Pattern name | Evidence (files / classes / methods) | Approx repeated shape | Concrete call sites | Repetition kind | Safe for Common | Recommended action |
|---|---|---:|---:|---|---|---|
| Enum / status validation | `ContentAnchorDeclaration(..)`, `PreferenceReadResult(..)`, `UnityInputTargetDescriptor(..)`, `TransitionEffectSnapshot(..)`, `PlayerActorSetIssue(..)` | `Enum.IsDefined` + reject `Unknown` / `None` + `IsValid` excludes sentinel values | ~142 files with `Enum.IsDefined`; ~164 files with explicit `Unknown` / `None` rejection or guards | Identical at the mechanic level; domain-specific enum targets | Yes | Extract now candidate |
| Defensive copy / empty collection | `GateSnapshot(..)`, `ActivityContentExecutionPhasePlan(..)`, `GlobalUiSceneRuntime(..)`, `LoadingProgressAggregationResult(..)`, `TransitionResult(..)` | `null` or empty => `Array.Empty<T>()`; otherwise copy to array; expose `IReadOnlyList<T>` wrappers | ~95 files with `Array.Empty` / `ToArray` / `CopyEntries` / `CopyIssues` shapes | Identical or very similar | Yes | Extract now candidate |
| Issue counting / blocking issue counting | `ActivityContentExecutionAggregateResult(..)`, `ActivityContentExecutionLifecycleResult(..)`, `InputModeUnityPlayerInputRequestApplicationResult(..)`, `PauseResult(..)`, `TransitionEffectPolicyEvaluation(..)`, `GateEvaluationResult(..)` | `IssueCount`, `BlockingIssueCount`, `HasBlockingIssues`, `HasNonBlockingIssues`, often via severity predicate | ~101 files with issue-counting surfaces | Similar; counting is shared, severity vocabulary stays domain-specific | Mostly yes, if helper stays internal and generic | Extract now candidate |
| Result / status shell patterns | `TransitionStep(..)`, `ActivityFlowStartResult(..)`, `FrameworkGameFlowStartResult(..)`, `PreferenceWriteResult(..)`, `ContentAnchorBindingResult(..)`, `ObjectResetResult(..)`, `PauseResult(..)`, `TransitionEffectResult(..)` | `Status + Source + Reason + Message`, `Succeeded` / `Failed`, `ToDiagnosticString`, factory methods like `Succeeded` / `Failed` / `Skipped` | ~43 files with explicit factory methods; ~220 files with `ToDiagnosticString` | Similar, not identical | Not yet | Needs ADR before extraction |
| Domain mappers that must stay out of Common | `ActivitySceneCompositionPlan.CountRequiredness`, `ActivitySceneReleaseResult.CountByStatus`, `ActivitySceneCompositionRuntime.CountReleasableRecords`, `RouteLifecycleRuntime`, `ActivityFlowRuntime`, `GameFlowRuntime`, `PauseInputModeUnityPlayerInputRuntimeBridge`, `RuntimeContent` / `ContentAnchor` bridges | Helpers that encode Route / Activity / Pause / RuntimeContent / ContentAnchor / InputMode / lifecycle policy | Many, but each one is tied to a local domain rule | Domain-specific | No | Keep domain-specific |

## Candidate helper groups

### 1) Enum / status validation helper

Why this is a candidate:

- The shape is repeated and low-risk.
- The helper would only centralize validation mechanics, not domain meaning.
- It fits the current `Runtime/Common` role better than any domain module-local copy.

Concrete uses seen in this snapshot:

- `Packages/com.immersive.framework/Runtime/ContentAnchor/ContentAnchorDeclaration.cs`
- `Packages/com.immersive.framework/Runtime/Preferences/PreferenceReadResult.cs`
- `Packages/com.immersive.framework/Runtime/UnityInput/UnityInputTargetDescriptor.cs`
- `Packages/com.immersive.framework/Runtime/TransitionEffects/TransitionEffectSnapshot.cs`

### 2) Defensive copy / empty collection helper

Why this is a candidate:

- The copy-and-empty pattern is repeated in many result and snapshot types.
- The helper would stay purely mechanical: no lifecycle, no routing, no authoring semantics.
- It reduces repeated `null` handling and `Array.Empty<T>()` branches without changing behavior.

Concrete uses seen in this snapshot:

- `Packages/com.immersive.framework/Runtime/Gate/GateSnapshot.cs`
- `Packages/com.immersive.framework/Runtime/ActivityFlow/ActivityContentExecutionPhasePlan.cs`
- `Packages/com.immersive.framework/Runtime/GlobalUi/GlobalUiSceneRuntime.cs`
- `Packages/com.immersive.framework/Runtime/Loading/LoadingProgressAggregationResult.cs`

### 3) Issue counting / blocking issue counting helper

Why this is a candidate:

- The counting shape repeats across result shells and aggregate diagnostics.
- The helper can remain internal and generic if it only aggregates counts and does not invent status semantics.
- It is a good fit for `Runtime/Common` because it is a shared mechanic, not a domain concept.

Concrete uses seen in this snapshot:

- `Packages/com.immersive.framework/Runtime/ActivityFlow/ActivityContentExecutionAggregateResult.cs`
- `Packages/com.immersive.framework/Runtime/ActivityFlow/ActivityContentExecutionLifecycleResult.cs`
- `Packages/com.immersive.framework/Runtime/InputMode/InputModeUnityPlayerInputRequestApplicationResult.cs`
- `Packages/com.immersive.framework/Runtime/Pause/PauseResult.cs`
- `Packages/com.immersive.framework/Runtime/TransitionEffects/TransitionEffectPolicyEvaluation.cs`
- `Packages/com.immersive.framework/Runtime/Gate/GateEvaluationResult.cs`

## Rejected Common candidates

These must not be promoted to `Runtime/Common` in this cut:

| Area | Examples | Why not Common |
|---|---|---|
| Route / Activity lifecycle | `ActivitySceneCompositionPlan.CountRequiredness`, `ActivitySceneReleaseResult.CountByStatus`, `ActivitySceneCompositionRuntime.CountReleasableRecords`, `RouteLifecycleRuntime`, `ActivityFlowRuntime` | These encode Route / Activity lifecycle policy and sequencing, not shared mechanics. |
| Pause / InputMode apply boundary | `PauseInputModeUnityPlayerInputRuntimeBridge`, `InputModeUnityPlayerInputRequestApplicationResult`, `PauseResult` | The helper shape is shared, but the meaning is specific to Pause and InputMode application. |
| RuntimeContent / ContentAnchor materialization | `RuntimeContentRuntime`, `RuntimeScopeTransitionGuardResult`, `ContentAnchorBindingResult`, `UnityContentAnchorMaterializationBridgeSetResult` | These know runtime-content and content-anchor semantics and must stay in their owning module. |
| Result shells with domain vocabulary | `TransitionStep`, `ActivityFlowStartResult`, `FrameworkGameFlowStartResult`, `ObjectResetResult`, `PreferenceWriteResult` | They look reusable, but the status vocabulary is domain-specific and the factory methods are policy-bearing. |
| Actor / save / adapter style helpers | `PlayerActorSetIssue`, `ProgressionSaveManifest`, adapter and bridge result shells | Out of scope for this inventory and not selected as Common candidates. |

## Recommended first implementation candidates

Order below matches the safest cut sequence for future implementation:

1. Enum / status validation helper.
2. Defensive copy / empty collection helper.
3. Issue counting / blocking issue counting helper.

Notes:

- Each candidate already has more than two concrete uses in this snapshot.
- Each candidate stays internal and mechanical.
- None of these candidates should replace public domain enums, result types or lifecycle policy.

## Risks

- `Common` can become a junk drawer if it accepts domain-specific semantics.
- Result shells can look generic while still carrying domain policy.
- Over-normalizing diagnostics can hide the original failure cause.
- Copy helpers can become too abstract if they try to cover mismatched shapes before a second concrete use exists.
- Validation helpers must not soften fail-fast behavior for bad sentinels or invalid serialized data.

## Suggested follow-up cuts

1. `COMMON-B` - Enum / status validation helpers.
2. `COMMON-C` - Defensive copy and empty collection helpers.
3. `COMMON-D` - Issue counting helpers.
4. `COMMON-E` - Result/status container ADR only after a second concrete cross-domain use is confirmed.

## Affected smokes

No Unity smoke was run in this docs-only cut.

If the future Common helpers are implemented, the affected QA smokes should be rechecked at minimum:

- `LoadingResultQaSmokeRunner`
- `LoadingProgressQaSmokeRunner`
- `TransitionQaSmokeRunner`
- `TransitionGateBlockerQaSmokeRunner`
- `TransitionEffectQaSmokeRunner`
- `TransitionEffectPolicyQaSmokeRunner`
- `InputModeUnityApplicationPreviewQaSmokeRunner`
- `InputModeUnityApplicationPlanQaSmokeRunner`
- `InputModeUnityPlayerInputApplicationQaSmokeRunner`
- `InputModeUnityPlayerInputRequestApplicationQaSmokeRunner`
- `PauseRuntimeRequestQaSmokeRunner`
- `PauseInputModeUnityPlayerInputRuntimeBridgeQaSmokeRunner`
- `PauseInputModeUnityPlayerInputApplicationQaSmokeRunner`
- `GateAdmissionQaSmokeRunner`
- `ObjectEntryQaSmokeRunner`
- `CycleResetQaSmokeRunner`
- `UnityInputTargetOwnershipQaSmokeRunner`
- `UnityInputOfficialComponentEvidenceQaSmokeRunner`

## Manual validation checklist

- Confirm the inventory matches the current `Runtime` snapshot only.
- Confirm each selected candidate has at least two concrete call sites.
- Confirm no Route / Activity / Pause / RuntimeContent / ContentAnchor / InputMode semantics were proposed as Common ownership.
- Confirm no `.cs`, `.asmdef`, `package.json`, scene, prefab or asset file was changed in this cut.

## Files altered in this cut

- `Assets/_Documentation/Audits/FXX-AUDIT-Common-Internal-Mechanics-Repetition-Inventory.md`
