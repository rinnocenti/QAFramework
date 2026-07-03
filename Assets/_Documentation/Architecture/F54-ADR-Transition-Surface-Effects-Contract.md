# F54-ADR-Transition-Surface-Effects-Contract

Status: Accepted / ADR-only  
Date: 2026-07-02  
Track: TRANSITION-1 / Transition Surface Effects Contract  
Depends on: F34, F35, F36, F39, F40, F41, F43, F53

## Context

F53 selected Transition Surface / Effects Hardening as the next architecture consolidation track because Transition is already visible in the real Route/Activity request path and directly affects first-game usability. GameFlow remains closed as an active axis until a real API consumer or orchestration bug appears. Loading is the current bounded Surface/Adapter reference, but F40 and F43 explicitly forbid copying Loading status values or creating a broad Surface layer.

Transition already has runtime contracts, effect adapters and Route/Activity diagnostics. It does not yet have an accepted contract that states the minimum surface, effect, adapter, request/result, evidence and failure mapping rules required before runtime hardening.

## Decision

Accept the Transition Surface / Effects Contract.

Transition is a bounded runtime surface for Route/Activity visual envelopes. Transition Effects are concrete visual operations requested by Transition. Transition Effect Adapters execute those effects Unity-side and return local evidence. Future hardening must strengthen Transition-specific evidence while preserving the current Route/Activity behavior and avoiding a universal adapter/result model.

F54 is contract-only. It does not change runtime C#, logs, scenes, prefabs, serialized assets, package metadata, asmdefs, csproj, QA Canvas, GameFlow, Loading, Pause or FlowTriggers.

## Current Transition Inventory

| Area | Current evidence | Contract reading |
| --- | --- | --- |
| Transition request | `TransitionRequest` carries operation id, scope, phase, source/reason and from/to Route/Activity references. | Adequate minimum request identity for Route, Activity and ActivityClear before/after visual phases. |
| Transition scope/kind/phase | `TransitionScope`, `TransitionKind`, `TransitionPhase` classify startup/route/activity/activity-clear and before/after operation phases. | Domain-owned vocabulary; do not replace with generic lifecycle stages. |
| Transition orchestrator | `ITransitionOrchestrator` executes sync/async `TransitionRequest` and returns `TransitionResult`. | Surface boundary for visual transition execution; must not own Route/Activity lifecycle. |
| No-op orchestrator | `NoOpTransitionOrchestrator` returns explicit `SucceededNoVisual` with `NoneConfigured`. | Valid optional/no configured behavior when policy allows no visual. |
| Unity effect orchestrator | `TransitionEffectOrchestrator` maps before/after transition requests to required fade effect requests and runs matching adapters. | Current concrete surface runtime; hardening should improve evidence, not behavior. |
| Transition result | `TransitionResult` preserves status, steps, issues, effect kind/status, adapter count, visual text and effect blocking issue count. | Good domain result base, but lacks retained named adapter evidence collection. |
| Request diagnostics | `FrameworkTransitionDiagnostics` keeps before/after results and projects transition/effect summary fields. | Route/Activity logs already expose minimum transition status and before/after separation. |
| Transition effect request | `TransitionEffectRequest` carries effect id, kind, requiredness, operation id, transition kind, phase, source and reason. | Effect-level request is separate from Route/Activity lifecycle and adapter execution. |
| Transition effect result | `TransitionEffectResult` preserves request, status, message, issues and `BlocksTransition`. | Adapter-local result is adequate but not retained as named aggregate evidence by Transition. |
| Effect authoring policy | `TransitionEffectAuthoringPolicy` reports required/optional missing adapters and duplicate effect ids. | Required absence already has blocking policy vocabulary. |
| Unity fade adapter | `UnityFadeCurtainEffectAdapter` applies a concrete fade/curtain/blackout surface through configured Unity references. | Adapter owns only local Unity side effects and visual settle timing. |
| Host | `GlobalUiSceneRuntime` collects `ITransitionEffectAdapter` from `UIGlobal` and exposes adapter count/references. | `UIGlobal` is a host for surface adapters, not a universal manager. |
| Consumer | `FrameworkRuntimeHost` creates the Transition orchestrator and `GameFlowRuntime` executes before/after transition requests for Route, Activity and ActivityClear. | Route/Activity request boundary is the consumer; lifecycle domains do not decide visual effects. |

## Contract Definitions

Transition Surface:
A runtime capability responsible for presenting a visual transition envelope before and after a Route/Activity operation when configured. It owns visual transition execution and diagnostics only; it does not own Route lifecycle, Activity lifecycle, Loading progress, Pause state, scene loading or content materialization.

Transition Effect:
A concrete visual operation requested by Transition, such as Fade, Curtain, Blackout, Cut or Crossfade. An effect describes visual intent and requiredness; it does not decide Route/Activity policy.

Transition Effect Adapter:
A Unity-side executor for one concrete effect family. It mutates only configured visual objects, returns `TransitionEffectResult` evidence, supports explicit sync/async execution when needed and must not discover global services, own lifecycle, choose Route/Activity policy or hide failures behind fallback.

Transition Consumer:
The current consumer is `FrameworkRuntimeHost` / Route/Activity operation boundary through `GameFlowRuntime` request execution. Scene-authored triggers call host request methods; they do not become Transition consumers.

Transition Host:
`UIGlobal` is the current explicit host for app/session scoped Transition Effect Adapters. A future host may exist only if it is explicit and scoped; it must not become a universal UI manager or service locator.

## Transition vs Loading

| Concern | Transition | Loading |
| --- | --- | --- |
| User purpose | Covers the visual change before/after a Route/Activity operation. | Communicates loading/progress/readiness during an operation. |
| Runtime intent | Visual envelope and visual settle. | Loading presentation, progress and status. |
| Required data | Scope, phase, visual/effect intent, source/reason and operation context. | Loading action, visibility, title/detail, progress, source/reason and progress support. |
| Adapter side effect | Fade/curtain/blackout/cut/crossfade visual effect. | Loading panel/progress UI visibility and progress display. |
| Progress ownership | Does not own progress. | Owns Loading progress presentation. |
| Relationship | May run with or without Loading. | May run with or without Transition. |

Loading remains the reference for contract questions and evidence expectations. Transition must keep its own domain statuses, request/result types and effect vocabulary.

## Ownership Map

| Owner | Responsibility | Must not own |
| --- | --- | --- |
| `FrameworkRuntimeHost` | Creates the current Transition orchestrator from `UIGlobal` adapters and logs Route/Activity Transition diagnostics. | Public GameFlow API, adapter execution details, scene-authored visual policy. |
| `GameFlowRuntime` | Executes Route/Activity before/after transition requests as part of internal request execution. | Public API, `UIGlobal` loading, adapter discovery, Loading/Pause behavior. |
| Transition runtime/service | Executes before/after Transition requests and aggregates Transition/effect evidence. | Route/Activity lifecycle, scene loading, Loading progress, Pause state. |
| Transition Effect Adapter | Applies one local Unity visual effect and returns local effect evidence. | Orchestration, lifecycle policy, service lookup, fallback policy. |
| `UIGlobal` / `GlobalUiSceneRuntime` | Hosts explicit shared visual adapters and reports missing configured adapters. | Universal UI management, Route/Activity content ownership, public service location. |
| Route/Activity lifecycle | Provides the operation context and receives diagnostics through request results/logs. | Effect selection, adapter execution, Loading progress. |
| Loading runtime | Presents Loading/progress evidence when requested. | Transition visual effect behavior. |
| Pause runtime | Owns logical Pause and resident Pause surface application. | Transition surface/effect contract. |

## Request / Result / Evidence Contract

Transition request must carry, at minimum:

- scope: `Startup`, `Route`, `Activity` or `ActivityClear` when available;
- phase: before/after, represented today by operation opened/closed phases;
- operation identity;
- source/reason diagnostics;
- route/activity from/to context when applicable;
- visual/effect mode or effect kind when already available;
- required/optional/no-op policy when available.

Transition result must preserve, at minimum:

- Transition-local status;
- phase evidence through observed steps or before/after result projection;
- effect kind/name or visual text;
- adapter count;
- issue count;
- blocking issue count;
- no-op/skipped reason;
- original adapter result/evidence when available;
- source/reason and diagnostic text.

Future Transition evidence should allow:

- aggregate adapter evidence count;
- applied/skipped/failed adapter counts;
- named adapter evidence;
- effect result status per adapter;
- blocking issue count per adapter;
- diagnostic string per adapter;
- before/after evidence without parsing issue text to reconstruct adapter status.

## Required / Optional / NoOp Rules

- Required Transition visual capability absence must fail visibly when the operation requires visual transition.
- Optional or unconfigured Transition capability may no-op only with explicit diagnostics.
- No-op is a declared result, not a fallback for missing required configuration.
- Missing, disabled, unsupported or invalid adapters must appear as evidence.
- Required adapter failure, rejection or missing adapter blocks Transition.
- Optional absence must become explicit skipped/no-op evidence.
- Before and after phases must remain separately diagnosable.
- `Unknown` or unexecuted state must not be interpreted as success.

## Failure Mapping Rules

F54 applies F39 to Transition:

- keep Transition and TransitionEffect statuses domain-owned;
- do not create a universal status/result type;
- preserve original adapter/effect cause when aggregating;
- keep blocking issues explicit;
- keep required absence blocking when policy requires visual capability;
- keep optional absence explicitly skipped/no-op;
- avoid reconstructing adapter status from issue text;
- aggregate success must mean required visual side effects were applied or explicitly skipped by declared optional/no-op policy.

## Route / Activity Log Projection Rules

Existing Route/Activity logs must continue projecting:

- `transition`
- `transitionScope`
- `transitionBefore`
- `transitionAfter`
- `transitionBlockingIssues`
- `transitionVisual`
- `transitionEffect`
- `transitionEffectBefore`
- `transitionEffectAfter`
- `transitionEffectBlockingIssues`
- `transitionEffectAdapterCount`

Future F55 may add Transition-specific aggregate evidence projection if needed, such as:

- `transitionEffectAdapterEvidenceCount`
- `transitionEffectAdapterEvidenceApplied`
- `transitionEffectAdapterEvidenceSkipped`
- `transitionEffectAdapterEvidenceFailed`
- `transitionEffectAdapterEvidenceBlockingIssues`
- `transitionEffectAdapterEvidenceNames`
- `transitionEffectAdapterEvidenceStatuses`

F54 does not implement these fields and does not alter current logs.

## Rejected Scope

F54 rejects:

- broad Surface layer;
- universal adapter result;
- universal status enum;
- public Transition API;
- GameFlow API reopening;
- Route/Activity lifecycle behavior changes;
- Loading runtime changes;
- Pause visual promotion;
- FlowTrigger changes;
- scene, prefab, serialized asset, ProjectSettings, package, asmdef, csproj or QA Canvas changes;
- new smoke buttons or smoke runners.

## Runtime Hardening Plan

Next gate: `F55 - TRANSITION-2 - Transition Runtime Evidence Hardening`.

Probable F55 scope:

- add Transition-specific aggregate/named adapter evidence if missing;
- preserve before/after adapter evidence separately;
- project aggregate Transition evidence in Route/Activity logs when useful;
- avoid issue-text parsing for adapter status;
- keep Route/Activity behavior unchanged;
- keep Loading, Pause and GameFlow untouched unless a direct compile-time dependency requires documentation-only alignment;
- validate with existing Standard, Route/Activity and Transition smoke groups.

## Validation Plan

F54 is ADR-only. No Unity compile, import, PlayMode, smoke or batchmode validation is required for this cut.

Static validation:

1. `git diff --check`
2. Confirm no active `FXX` placeholder file was created.
3. Confirm `Assets/_Documentation/Architecture` remains flat.
4. Confirm no runtime C# files changed.
5. Confirm no scenes, prefabs, serialized assets, ProjectSettings, package metadata, asmdefs or csproj changed.
6. Confirm GameFlow, Loading, Transition, TransitionEffects, Pause and FlowTriggers runtime files are untouched.

Manual validation for future F55:

1. Unity compile/import.
2. Standard Smoke.
3. Activity Baseline Smoke.
4. Route Scene Composition Smoke.
5. Route Release Smoke.
6. Composite Lifecycle Release Smoke, when applicable.
7. Transition Smoke.
8. Transition Effect Smoke.
9. Transition Effect Unity Fade Curtain Smoke.
10. Transition Gate Blocker Smoke.

## Next Gate

`F55 - TRANSITION-2 - Transition Runtime Evidence Hardening`
