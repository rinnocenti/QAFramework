# F39-ADR-Status-Mapping-Policy

Status: Accepted / Doc-policy
Last updated: 2026-07-01
Supersedes: none
Superseded by: none

## Context

F35 accepted the Extension Surface Model. F36 inventoried current Surface, Adapter, Bridge, Operation Service, Consumer, Validator/Evidence and QA Smoke Runner candidates. F37 accepted the Pause/InputMode apply boundary contract. F38 extracted the Pause/InputMode runtime boundary and proved the local pattern of `failedStage` plus original Pause/InputMode/PlayerInput evidence.

Several framework areas aggregate results from different domains:

- Loading surface/runtime and Loading observation.
- Transition orchestration and Transition Effect adapters.
- Pause surface runtime and Pause/InputMode apply.
- RuntimeContent and ContentAnchor materialization/release.
- Route/Activity lifecycle and participant execution logs.
- QA Canvas and smoke runners.

Without a shared policy, each area can accidentally hide the real cause through status laundering, overly generic enums, lost adapter results, lost failed stages, ambiguous no-op results, aggregate success after partial side effects, or smokes that assert only an aggregate `passed` flag.

## Decision

Accept a cross-cutting failure/status mapping policy, not a universal status type.

Each domain keeps its own enums and result types. Aggregate boundaries must preserve enough original evidence to understand which stage failed, which subsystem produced the cause, which adapter applied a side effect, and which blocking issues were present.

This ADR does not create:

- A universal status enum.
- A universal `Result<T>`.
- A common result base class.
- A shared stage taxonomy.
- A runtime implementation patch.

## Required concepts

| Concept | Policy meaning |
| --- | --- |
| Domain status | Status owned by the local domain, such as Loading, TransitionEffect, Pause, InputMode, RuntimeContent or ContentAnchor. It must not be replaced by a broad framework enum. |
| Aggregate status | Boundary-local summary status used only by the boundary that combines domain results. It must not erase original statuses. |
| Failed stage | Boundary-local stage name for the step that blocked the aggregate operation. It belongs to the boundary, not to a global stage taxonomy. |
| Original subsystem result | The domain result that explains the root cause or a named projection sufficient to reconstruct it. |
| Adapter result | Evidence returned by a concrete side-effect executor. It is local evidence, not policy. |
| Blocking issue | Evidence that must prevent success, readiness or required side-effect completion. |
| Non-blocking issue | Evidence that may allow completion with warnings or degraded diagnostics, but must remain visible. |
| Side effect applied | Explicit evidence that the required mutation happened, such as action-map selection, visible surface apply, placement, materialization or release. |
| Partial side effect | Evidence that one or more side effects happened but the full required operation did not complete. It must not become aggregate success unless rollback or policy explicitly makes the result safe. |
| Rollback result | Evidence for cleanup after partial side effects. It must be preserved when rollback exists. |
| Required capability missing | Missing required runtime, adapter, host, action map, anchor, root, materialization or surface. This is a visible failure/blocking issue. |
| Optional no-op | Explicitly configured absence that completes without side effects and explains why no operation was needed. |
| Skipped with evidence | A skipped step that states the policy reason and preserves source/reason diagnostics. |
| Unknown / not requested | Initialization or genuinely unexecuted state. It must not be interpreted as success. |

## Recommended aggregate result fields

Any boundary aggregate result should expose domain-specific equivalents of:

- `status`
- `failedStage`
- `blockingIssues`
- `issues`
- `source`
- `reason`
- `sideEffectsApplied`, or a domain-specific equivalent
- `originalResult`, or named original domain result references
- `adapterResult`, when an adapter exists
- `rollbackResult`, when rollback exists
- `message`, or short diagnostic text

These are conceptual requirements. This cut does not create a common interface, common base class or shared result container.

## Mapping rules

### Rule A - Preserve original cause

An aggregate result must keep the original result object or named fields sufficient to reconstruct the origin of the failure. A summary status without subsystem evidence is not acceptable.

### Rule B - Failed stage is boundary-local

`failedStage` belongs to the boundary that aggregates. Loading, Transition, Pause/InputMode, RuntimeContent and ContentAnchor must not share a fake global stage taxonomy.

### Rule C - Adapter evidence is not policy

Adapters return local side-effect evidence. Operation Services or Surfaces decide the aggregate result. Bridges expose diagnostics and must not launder failures into success.

### Rule D - Optional no-op must be explicit

A valid no-op must say that it was skipped/no-op and why. No-op must not mask missing required configuration.

### Rule E - Required absence fails

Missing required capability must become a visible failure or blocking issue. Required absence may not be silently downgraded to optional behavior.

### Rule F - Unknown is not success

`Unknown`, `NotRequested` or equivalent states cannot be treated as success. They are allowed only for initialization or truly unexecuted steps, and the aggregate must make that clear.

### Rule G - QA smoke must assert evidence

Smokes must assert observable cause evidence, such as `failedStage`, adapter result, original result, blocking issue count, no-op reason, rollback evidence or side-effect flags. A smoke that checks only `passed='True'` is not sufficient for policy readiness.

## Hot spot matrix

| Area | Current mapping shape | Risk | Required policy | Implementation needed now? | Future gate |
| --- | --- | --- | --- | --- | --- |
| Loading surface/runtime | `LoadingSurfaceResult` has local status, adapter name, issues and blocking issue count; runtime aggregates adapter outcomes into a surface result. | Adapter-specific result objects are flattened into messages/issues when multiple adapters execute. | Preserve adapter evidence or named adapter diagnostics when promoting Loading as a pilot. Required surface absence must fail; optional/no-op must stay explicit. | No. Use as `SURFACE-PILOT-1` contract requirement. | `SURFACE-PILOT-1` |
| Loading observation adapter | Observation maps lifecycle/transition observations into Loading progress/status. | Synthetic progress/status can launder the original lifecycle/transition cause. | Observation diagnostics must name the source observation and avoid presenting synthetic progress as domain success. | No. Review during pilot. | `SURFACE-PILOT-1` |
| Transition effect orchestration | `TransitionResult` aggregates effect policy/effect adapter status, issues, effect status and blocking counts. | Required effect failure can be collapsed into transition-level message text. | Keep TransitionEffect status and policy issues visible when reporting aggregate Transition failure. | No. Secondary pilot candidate only. | Later pilot or Transition-focused patch |
| Unity fade curtain effect adapter | `TransitionEffectResult` preserves request, requiredness, status, issues and `BlocksTransition`. | Visual timing failure or unsupported phase can be hidden if only aggregate Transition status is asserted. | QA and callers must inspect effect status/issues when the fade adapter is involved. | No. | Later pilot or Transition diagnostics |
| Pause surface runtime | `PauseSurfaceApplicationResult` reports adapter counts, applied count, failed count and issues; adapter contract itself is void. | Adapter-local evidence is weak because `IPauseSurfaceAdapter.Apply` does not return a result. | Treat Pause surface as partial until PAUSEVIS decides whether result-returning adapter evidence is required. | No. Do not patch in STATUS-1. | `PAUSEVIS-1` |
| Pause/InputMode apply service/result | `PauseInputModeApplyResult` preserves aggregate status, `failedStage`, Pause result, preflight plan, application result and PlayerInput adapter result. | Good reference pattern, but still domain-specific and must not become a universal result model. | Use as evidence pattern only: failed stage plus original domain results plus adapter result. | No. F38 is owner-validated. | Reference for `SURFACE-PILOT-1` |
| Pause InputAction bridge trigger | Trigger delegates to the runtime bridge and records trigger/input evidence. | Trigger can be mistaken for a generic flow trigger helper. | Keep trigger as Consumer/Bridge evidence only; do not close FLOWTRIGGER from this path. | No. | `FLOWTRIGGER` |
| RuntimeContent materialization/release | RuntimeContent results preserve request, status, handle, source, reason and message. Release plans preserve skipped counts. | Broad consumers can treat materialization success as Surface readiness. | Preserve RuntimeContent status as original subsystem evidence inside broader materialization aggregates. | No. MAT core remains closed; consumers remain gated. | `SURFACE-PILOT-1` or materialization consumer gate |
| ContentAnchor materialization service | `ContentAnchorMaterializationResult` preserves `failedStage`, materialization result, applied materialization result, binding result, placement result and rollback result. | Strong evidence pattern can tempt broad generic abstraction. | Keep stage taxonomy ContentAnchor-local; preserve rollback result on failures after side effects. | No. Already fits policy. | Future materialization consumers |
| ContentAnchor release/binding cleanup | Release/binding cleanup paths expose domain statuses and skipped/cleanup evidence. | Cleanup skip or partial cleanup can be interpreted as full success. | Skipped cleanup must be explicit; partial release must keep physical/logical result evidence. | No. | Future materialization consumers |
| Route/Activity lifecycle request logs | Route/Activity and ActivityContentExecution aggregate results preserve phase/status/result counts and blocking/non-blocking issues. | Lifecycle status can become a broad operation-success proxy without original participant or scene result details. | Keep per-result details in diagnostics and do not close lifecycle kernel from aggregate logs alone. | No. | `LIFECYCLE-KERNEL-REMAINING` |
| QA Canvas/smoke runners | Smokes log many detailed fields, including blocking issue counts, skipped counts, rollback and side-effect flags in several paths. | QA-only consumers can be treated as product readiness; smokes can drift toward aggregate pass checks only. | Smokes must assert evidence, not only pass/fail; QA-only evidence does not prove product consumer readiness. | No. | All runtime gates |

## Accepted scope

- Cross-boundary policy language for future adapters, surfaces, bridges and operation services.
- Conceptual fields for aggregate results.
- Domain-first status ownership.
- Boundary-local failed stages.
- Explicit optional/no-op and required-missing behavior.
- QA evidence requirements for future smokes.

## Rejected scope

- Universal status enum.
- Universal `Result<T>`.
- Common result base class or common interface.
- Runtime/editor/asmdef/package changes.
- Smoke runner changes.
- Broad Surface layer readiness.
- Closing `SURFACE-PILOT-1`, `PAUSEVIS-1`, `FLOWTRIGGER`, `GAMEFLOW` or `LIFECYCLE-KERNEL-REMAINING`.

## Current implementation coverage

`PauseInputModeApplyResult` and `ContentAnchorMaterializationResult` are the strongest current examples of the accepted pattern because they preserve boundary-local failed stage plus original subsystem results.

Loading and Transition have usable domain results and adapter evidence, but pilot work must verify that aggregate diagnostics keep adapter/original evidence visible enough for consumers and QA.

Pause surface runtime remains partial because adapter execution is currently void and evidence is aggregated through counts/issues. This is not a STATUS-1 blocker, but it is relevant for `PAUSEVIS-1`.

Route/Activity lifecycle remains partial. Aggregate logs and QA evidence must not be used to close the lifecycle operation kernel.

No critical mapping gap blocks the next readiness gate. The next recommended gate is `SURFACE-PILOT-1`, with this ADR as an input constraint.

## Pending decisions

- Which single Surface path is selected for `SURFACE-PILOT-1`.
- Whether the selected pilot needs a small implementation patch to preserve named adapter/original evidence.
- Whether Pause visual readiness requires result-returning surface adapter evidence in `PAUSEVIS-1`.
- Whether any future repeated mapping mechanic is concrete enough for a small common helper without creating a generic result container.
