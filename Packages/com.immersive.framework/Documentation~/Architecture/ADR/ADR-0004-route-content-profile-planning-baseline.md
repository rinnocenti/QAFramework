# ADR-0004 — Route Content Profile Planning Baseline

## Status

Accepted.

## Context

The framework is moving content materialization away from ad-hoc `GameObject.SetActive` bindings and toward explicit content scopes, handles, sets, and contributions.

Route must be solved before Activity because a Route owns the runtime context in which Activities later contribute content. The current Route model still executes only one Primary Scene, but the framework needs an authoring/planning surface for future Route scene composition without changing runtime behavior immediately.

## Decision

Introduce a Route Content Profile authoring asset and a minimal runtime Route Content Materialization Plan.

The Route asset may now reference an optional `RouteContentProfileAsset`.

The profile may declare additional Route scenes through `RouteContentSceneEntry` records. Each entry carries:

- content id;
- scene path/name;
- requiredness.

In this cut, additional scenes are planning data only. They are not loaded, unloaded, activated, or validated as mandatory runtime dependencies.

The Route Lifecycle still materializes only the Primary Scene through the existing Scene Lifecycle path. The resulting `RouteContentSet` still contains only the active Primary Scene handle. When a Route Content Profile is assigned, diagnostics include the planned additional scene count and details.

## Consequences

- Route authoring now has a place to describe future Route-owned content beyond the Primary Scene.
- Route runtime now has an explicit plan object without changing scene loading behavior.
- Additive Route scene composition is intentionally deferred to the next cut.
- Activity content remains unchanged.
- `ActivityContentBinding` remains a local visibility adapter, not canonical content materialization.
- No camera, audio, actor, pause, presentation, Addressables, or materialização física runtime is introduced.

## Non-goals

- Do not load additional Route scenes yet.
- Do not introduce fallback loading.
- Do not make planned required entries block Route startup yet.
- Do not create Activity content profiles in this cut.
- Do not revive CameraFlow.

## Freeze note

This ADR is part of the frozen materialization baseline. The profile and plan exist, but execution remains intentionally deferred.

The route-additive execution cut generated after this ADR is not part of the accepted baseline. The next implementation step should not deepen Route execution yet; it should first normalize ContentFlow across Session, Route, Activity and Local scopes.
