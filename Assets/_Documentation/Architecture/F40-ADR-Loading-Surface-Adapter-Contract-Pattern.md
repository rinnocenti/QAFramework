# F40-ADR-Loading-Surface-Adapter-Contract-Pattern

Status: Accepted / Pilot contract
Last updated: 2026-07-01
Supersedes: none
Superseded by: none

## Context

F35 accepted the framework extension archetypes. F36 identified Loading as the strongest bounded Surface/Adapter pilot candidate. F39 accepted domain-first failure/status mapping and rejected a universal status enum, universal `Result<T>`, common result base class and global adapter/surface interface.

F40 uses Loading as the first concrete Surface Adapter Contract Pattern. The purpose is to make future Surface/Adapter work easier without creating a broad Surface layer or erasing domain semantics.

## Decision

Accept Loading as the bounded pilot for a Surface Adapter Contract Pattern.

Surface/Adapter extensibility is facilitated by domain contract patterns, not by universal framework abstractions. Each domain keeps its own requests, results, statuses, failure semantics and diagnostics. Common helpers may be extracted only when they are mechanical and neutral, such as enum validation, defensive copying, issue counting or diagnostics formatting.

Loading is accepted as `Pilot accepted / no runtime patch required`.

This ADR does not create:

- A universal status enum.
- A universal `Result<T>`.
- A common result base class or global result interface.
- `IFrameworkAdapter`.
- `FrameworkSurface`.
- A broad Surface layer.
- A runtime implementation patch.

## Canonical Surface Adapter Contract Pattern

### Surface contract

A Surface defines:

- Capability intent.
- Readiness/availability semantics.
- Required versus optional behavior.
- Request shape.
- Aggregate result shape.
- Failure and no-op semantics.
- Consumer expectations.
- QA evidence expectations.

### Adapter contract

An Adapter defines:

- One local side effect.
- Explicit adapter evidence.
- Supported and unsupported behavior.
- Local failure details.
- No lifecycle ownership.
- No policy over unrelated domains.
- No silent fallback for required configuration.

### Runtime or operation boundary

The runtime boundary defines:

- Adapter use from explicit references.
- Aggregation rules.
- Required missing failure.
- Optional no-op.
- Issue aggregation.
- Diagnostics.
- Preservation of original and adapter evidence.

### Consumer

A Consumer defines:

- Why the capability is needed.
- How absence or failure is handled.
- Source and reason diagnostics.
- Whether the Surface is required or optional.

### QA evidence

QA should prove:

- Success path.
- Required missing capability.
- Optional/no-op, when supported.
- Adapter unsupported/failure, when supported.
- Side effect applied.
- Original or adapter evidence visible.
- No forbidden side effects.

QA-only consumers do not prove product readiness.

## Loading audit against the pattern

| Pattern area | Loading evidence | Assessment |
| --- | --- | --- |
| Capability intent | `LoadingSurfaceRequest`, `LoadingSurfaceAction`, `LoadingProgress` and runtime docs separate Loading presentation from Transition effects and scene lifecycle. | Meets pattern. |
| Readiness/availability | `LoadingSurfaceRuntime` exposes adapter count, visible surface and progress support. `FrameworkLoadingDiagnostics` logs visual state, adapter count and progress mode. | Meets pattern for current pilot. |
| Required vs optional behavior | `LoadingSurfacePolicy` names `NoneConfigured`, `Optional` and `Required`; runtime diagnostics distinguish no-op and required missing surface. | Meets pattern conceptually. |
| Request shape | `LoadingSurfaceRequest` carries action, visibility, progress, title/detail, source and reason. | Meets pattern. |
| Adapter result shape | `LoadingSurfaceResult` carries request, domain status, adapter name, issues, blocking issue count and diagnostics. | Meets pattern. |
| Adapter local side effect | `UnityLoadingSurfaceAdapter` mutates only configured UI references, root active state, CanvasGroup and progress widgets. | Meets pattern. |
| No unrelated lifecycle ownership | Loading contracts and adapter comments explicitly reject SceneLifecycle, RouteLifecycle, ActivityFlow, registry and tween/lifecycle ownership. | Meets pattern. |
| Optional no-op | `NoOpLoadingSurfaceAdapter` returns explicit `Skipped` results with a no-surface reason. | Meets pattern. |
| Required missing failure | `FrameworkLoadingDiagnostics.FailedRequiredUnitySurfaceMissing` and `LoadingSurfaceRuntime` failure paths expose blocking issue evidence. | Meets pattern. |
| Unknown handling | Loading result constructors reject `Unknown` status for valid results; diagnostics may return `Unknown` only for default/unexecuted result projection. | Meets pattern. |
| Runtime aggregation | `LoadingSurfaceRuntime` aggregates matching adapter results into a domain result and preserves adapter names in issue text. | Good enough for pilot; direct per-adapter result list is not required now. |
| Internal consumer | `FrameworkRuntimeHost` invokes `LoadingSurfaceRuntime` around route/activity operations. `GameFlowRuntime` and `RouteLifecycleRuntime` carry loading progress reporters through real runtime paths. | Real internal consumer, not QA-only. |
| Diagnostics | Runtime logs include `loadingBefore`, `loadingAfter`, `loadingBlockingIssues`, `loadingAdapterCount`, `loadingProgressSupported`, `loadingProgressMode`, progress value/percent/phase/message. | Meets pattern for current pilot. |
| QA evidence | Existing QA covers Loading progress aggregation, Loading observation, Loading result/issues and legacy loading screen adapter. | Sufficient for doc-only pilot; a future runtime patch touching Loading surface should add or update direct surface adapter smoke evidence. |

## Direct answers

1. Does Loading already comply with the pattern?
   Yes for a bounded contract pilot. It has domain-specific surface contracts, adapter contracts, result/status types, no-op semantics, runtime aggregation and real internal consumers.

2. What evidence exists?
   `LoadingSurfaceContracts.cs`, `LoadingSurfaceRuntime.cs`, `UnityLoadingSurfaceAdapter.cs`, `LoadingObservationAdapter.cs`, `FrameworkRuntimeHost` loading diagnostics, GameFlow/RouteLifecycle progress reporter paths and current Loading QA smokes.

3. What evidence is weak?
   Aggregated multi-adapter evidence is currently represented through adapter-prefixed issue text, not a retained per-adapter result list. Direct QA evidence for the current `UnityLoadingSurfaceAdapter` path is weaker than the result/progress/observation evidence.

4. Is a runtime patch required now?
   No. The weakness is not critical for accepting the contract pattern. A future runtime cut that changes Loading aggregation or public diagnostics should add direct smoke coverage and may preserve named per-adapter result evidence if needed.

5. Is adapter result visible enough?
   Yes for the pilot: individual adapter results include adapter name, status, request, issues and blocking issue count; aggregate diagnostics expose adapter count and blocking issues. Future multi-adapter work should preserve named adapter evidence more directly.

6. Is required/optional/no-op explicit?
   Yes. Required missing configuration fails visibly; no configured surface is explicit no-op/skipped behavior.

7. Is `Unknown`/`NotRequested` handled correctly?
   Yes for valid results. `Unknown` is rejected by domain result constructors and only appears as default/unexecuted projection in diagnostics.

8. Is the consumer internal real or QA-only?
   Real internal. `FrameworkRuntimeHost` uses Loading around route/activity operations, with progress reporter integration through GameFlow and RouteLifecycle.

## How to create the next Surface Adapter

1. Create or reuse a domain Surface contract. Define capability intent, request shape, readiness/availability and required/optional semantics.
2. Create a domain Adapter contract. It must execute one local side effect and return domain adapter evidence.
3. Define the local side effect precisely. Do not let the adapter own lifecycle, unrelated policy, discovery, rollback or service lookup.
4. Define domain result/status types. Do not reuse Loading statuses unless the domain is actually Loading.
5. Define aggregate result semantics. Preserve failed stage when the boundary has stages, original subsystem evidence, adapter result evidence, blocking issues, source/reason and message.
6. Define failure/no-op behavior. Required missing capability fails; optional absence must return explicit skipped/no-op evidence.
7. Define a real consumer. QA-only invocation is evidence, not product readiness.
8. Define smoke/evidence expectations. Smokes must assert cause and side-effect evidence, not only aggregate pass/fail.
9. Extract common helpers only after concrete repetition proves the helper is mechanical and domain-neutral.

## Accepted scope

- Loading as the first bounded Surface/Adapter contract pilot.
- Contract pattern language for future Surface/Adapter work.
- Static audit of Loading against F35/F36/F39.
- Public documentation wording for authoring, runtime surfaces and troubleshooting.

## Rejected scope

- Runtime patch in this cut.
- New adapter or new Surface.
- Universal framework adapter/surface/result abstractions.
- Migration of Transition, Pause, InputMode, ContentAnchor or lifecycle into this pattern.
- Broad Surface layer readiness.
- QA-only consumer as product readiness.
- Closing `PAUSEVIS-1`, `FLOWTRIGGER`, `GAMEFLOW` or `LIFECYCLE-KERNEL-REMAINING`.

## Current implementation coverage

Loading is the accepted reference for the pattern, not a template to copy wholesale. Future domains should copy the contract questions and evidence expectations, not Loading class names or status values.

Adapter readiness remains partial because only one bounded contract pattern is accepted. Surface readiness remains partial because no broad Surface layer is approved.

## Pending decisions

- Whether `PAUSEVIS-1` should decide result-returning evidence for Pause visual adapters before any Pause surface promotion.
- Whether `FLOWTRIGGER` should run before lifecycle kernel work because repeated trigger/request/state shapes are now more isolated.
- Whether a future Loading runtime patch should preserve a named per-adapter result list in aggregate diagnostics.
