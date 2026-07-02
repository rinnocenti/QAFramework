# F43-ADR-Loading-Runtime-Reference-Hardening

Status: Implemented locally / pending Unity validation
Last updated: 2026-07-01
Supersedes: none
Superseded by: none

## Context

F40 accepted Loading as the bounded Surface/Adapter Contract Pattern. That decision was sufficient for a pilot, but it also identified a weak point: multi-adapter aggregate evidence depended on adapter-prefixed issue text and aggregate counts.

F43 hardens the runtime reference without broadening the pattern into a global Surface layer. The goal is to make Loading a stronger concrete reference for future domain-specific cuts while keeping each domain responsible for its own result/status language.

F42 is now closed in the mutable tracker as owner-validated. Route/Activity trigger migration remains deferred to a future GameFlow-scoped cut.

## Decision

Add domain-specific Loading adapter evidence to aggregate Loading surface results.

Created/changed runtime contract:

- `LoadingSurfaceAdapterEvidence`
- aggregate `LoadingSurfaceResult.AdapterEvidence`
- aggregate counters for applied, skipped, failed, issue and blocking issue evidence
- `LoadingSurfaceResult.ToDiagnosticString()` fields for adapter evidence counts
- runtime aggregation in `LoadingSurfaceRuntime`
- request-level projection in `FrameworkLoadingDiagnostics` and `FrameworkRuntimeHost` logs
- Loading Result smoke coverage for adapter evidence

The evidence records:

- adapter name
- Loading-specific result status
- applied/skipped/failed projection
- issue count
- blocking issue count
- message

The collection is defensive/read-only from consumers. Existing `LoadingSurfaceResult` constructor and factories remain valid; new overloads accept adapter evidence for aggregate results.

## Preserved boundaries

This cut does not create:

- `IFrameworkAdapter`
- `FrameworkSurface`
- universal status enum
- universal `Result<T>`
- shared adapter base class
- global adapter registry
- Loading manager
- service locator
- broad Surface layer

This cut does not migrate:

- Transition
- Pause
- InputMode
- RuntimeContent
- ContentAnchor
- GameFlow
- RouteLifecycle
- ActivityFlow
- lifecycle kernel

## Runtime behavior

`LoadingSurfaceRuntime` now preserves named adapter evidence when it aggregates matching adapters. The runtime also records evidence for:

- explicit NoOp Loading surface behavior
- unsupported adapters when a configured surface has no matching adapter
- failed/rejected adapter execution
- succeeded or warning adapter execution

Required missing configuration remains blocking. Optional/no configured surface remains explicit skipped/no-op behavior.

## Diagnostics and QA

`LoadingSurfaceResult.ToDiagnosticString()` and request-level Loading diagnostics now include:

- `adapterEvidence`
- `adapterEvidenceApplied`
- `adapterEvidenceSkipped`
- `adapterEvidenceFailed`
- `adapterEvidenceBlockingIssues`

The Loading Result smoke now validates a synthetic aggregate with succeeded, skipped and failed adapter evidence. `FrameworkRuntimeHost` route/activity logs now project the same aggregate evidence from real Loading operations. The diagnostics expose:

- `loadingAdapterEvidenceCount`
- `loadingAdapterEvidenceApplied`
- `loadingAdapterEvidenceSkipped`
- `loadingAdapterEvidenceFailed`
- `loadingAdapterEvidenceIssues`
- `loadingAdapterEvidenceBlockingIssues`
- `loadingAdapterEvidenceNames`
- `loadingAdapterEvidenceStatuses`
- `loadingProgressSupported`
- `loadingProgressMode`
- `loadingNoOp`

No QA Canvas serialized asset or button was changed.

## Consequences

Loading is still only a bounded reference pattern. Future surfaces may copy the questions and evidence expectations, not the Loading class names or statuses.

The F40 weak point is resolved for Loading: aggregate diagnostics no longer need to parse adapter-prefixed issue text to know which adapter applied, skipped or failed.

Pause resident presentation remains the supported Pause visual path, but Pause visual/materialization is still frozen until a future cut defines a real consumer and result-returning evidence requirements.

## Validation

Static validation is required in this cut. Unity validation remains pending because runtime C# changed.

Manual validation after import/compile:

1. Unity compile/import.
2. Standard Smoke.
3. Loading Result Smoke.
4. Loading Readiness Smoke.
5. Loading Progress Smoke.
6. Loading Screen Adapter Smoke.
7. Runtime route/activity operation with Loading enabled, checking aggregate Loading diagnostics for adapter evidence fields.

## What not to change now

- Do not create a global Surface/Adapter abstraction.
- Do not migrate Transition/Pause/InputMode to Loading contracts.
- Do not change scenes, prefabs, serialized QA Canvas assets or ProjectSettings.
- Do not change GameFlow, RouteLifecycle, ActivityFlow or lifecycle kernel behavior in this cut.
- Do not treat QA-only invocation as product readiness for future surfaces.
