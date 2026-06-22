# Completeness Tracker

Status consolidado do pacote `com.immersive.framework`.

Este arquivo substitui os antigos documentos de fechamento e aceite de fase. Os documentos técnicos específicos permanecem como evidência de cada corte; o status vivo fica aqui.

## Overview

| Phase | Status | Current gate | Consolidated docs |
|---|---|---|---|
| F0 | `CLOSED / PASS` | Baseline closed | `Core/BASELINE_SMOKE.md` |
| F1 | `CLOSED / PASS` | Identity, diagnostics and validation baseline closed | `Core/API_STATUS_CONVENTION.md`, `Core/FRAMEWORK_FACT_MINIMAL_MODEL.md`, `Core/VALIDATION_MODE_SEMANTICS.md`, `Core/TYPED_IDENTITY_PRIMITIVES.md`, `Core/CONTENT_IDENTITY_AND_HANDLE_REVIEW.md` |
| F2 | `CLOSED / PASS` | Session scope closed | `Session/SESSION_RUNTIME_STATE_BOUNDARY.md`, `Session/SESSION_CONTENT_SET_MINIMAL_MODEL.md` |
| F3 | `CLOSED / PASS` | Route baseline closed | `Route/ROUTE_RUNTIME_STATE_TYPED.md`, `Route/ROUTE_EXIT_RESULT_MINIMAL.md`, `Route/ROUTE_CONTENT_RUNTIME_EXECUTION_DECISION.md`, `Route/ROUTE_CONTENT_SET_SEMANTICS.md`, `Route/ROUTE_LOCAL_CALLBACK_SMOKE.md`, `Route/ROUTE_VALIDATOR_EXPANSION.md`, `Route/QA_PANEL_SIMPLIFICATION.md`, `Route/QA_AUTHORING_VALIDATION_HYGIENE.md` |
| F4 | `CLOSED / ACTIVITY BASELINE PASS` | Activity baseline closed | `Activity/ACTIVITY_RUNTIME_STATE_REFINED.md`, `Activity/ACTIVITY_CONTENT_SET_MINIMAL.md`, `Activity/ACTIVITY_CONTENT_LIFECYCLE_RESULT.md`, `Activity/ACTIVITY_READINESS_STATE_MINIMAL.md`, `Activity/ACTIVITY_LOCAL_VISIBILITY_ADAPTER.md`, `Activity/ACTIVITY_BASELINE_SMOKE.md` |
| F5 | `CLOSED / LOCAL CONTRIBUTION FOUNDATION PASS` | F5H local smoke passed; F5 closure audit completed | `Local/LOCAL_CONTENT_IDENTITY.md` |
| F6 | `OPEN / F6C APPLIED` | RouteSceneCompositionPlan and RouteSceneCompositionResult inert models added; pending Unity compile/smoke before F6D | `Planning/F6-Route-Scene-Composition-Audit.md`, `ADRs/F6-route-scene-composition-and-release/` |

## Consolidation rule

The following file families are status-only and should not expand further:

- phase closure files;
- ADR acceptance files;
- hygiene/checkpoint files whose only purpose is to restate status.

Use this file instead for phase state, next gate, and historical completion summary.

## Preserved technical evidence

Keep these docs as the durable record for implementation details:

- roadmap and traceability matrix;
- ADRs under `ADRs/`;
- architecture references under `Architecture/ADR/`;
- technical cut docs under `Core/`, `Session/`, `Route/`, `Activity/` and `Local/`.

## Current next step

| Next authorized step | Reason |
|---|---|
| `F6C — RouteSceneCompositionResult compile/smoke` | F6B passed smoke and F6C added inert result data. Validate Unity compilation before advancing to F6D additive scene primitive. |

## F5 closure audit

Status: `CLOSED / LOCAL CONTRIBUTION FOUNDATION PASS`.

Validated by QA:

```text
QA Smoke completed. name='Local Contribution Smoke'
QA Local Contribution Smoke step completed. step='loaded'
QA Local Contribution Smoke step completed. step='secondary'
QA Local Contribution Smoke step completed. step='primary'
QA Smoke completed. name='Standard Smoke'
QA Smoke completed. name='Route Callback Smoke'
validationIssues='0'
blockingIssues='0'
optionalSkips='0'
```

Implemented scope:

- `LocalContentIdentity`, `LocalContentId` and `LocalContentScopeKind`;
- explicit `Local Content Id` on `RouteContentBinding` and `ActivityLocalVisibilityAdapter`;
- loaded scene-authored local contribution discovery;
- `LocalContributionSet` with scope/source/identity/requiredness queries;
- `Required/Optional` metadata on discovered local handles;
- local contribution validators and expected requirement model for future consumers;
- dedicated `Local Contribution Smoke` in the QA Canvas.

Confirmed removals and exclusions:

- `FrameworkContentContributionMarker` removed;
- `IFrameworkContentContribution` removed;
- no `LocalContributionMarker` parallel component;
- no `GameObject.name`, scene name, scene path or hierarchy path as functional identity;
- no canonical materialization, Surface, Actors, Input, Camera, Reset, Snapshot, Save, Pooling, runtime references, release/unload policy or expected contribution asset in F5.


## F6 ADR gate

Status: `OPEN / F6C APPLIED`.

F6A completed the route scene composition and release ADR gate. F6B added inert runtime planning types for Route scene composition and passed baseline smoke. F6C added inert runtime result types for Route scene composition. No additive scene loading, unloading, release, Surface, RuntimeRoot or materialization execution was added.

Accepted ADRs:

- `ADRs/F6-route-scene-composition-and-release/F6-01-ADR-RELEASE-001-content-release-plan-by-scope.md`;
- `ADRs/F6-route-scene-composition-and-release/F6-02-ADR-SCENE-001-route-scene-composition-plan-and-result.md`.

Audit doc:

- `Planning/F6-Route-Scene-Composition-Audit.md`.

Current validation gate:

```text
F6C — RouteSceneCompositionResult compile/smoke
```

F6B is closed by smoke evidence. F6C is inert result data only. It does not load additive scenes, unload scenes, create Surface, create RuntimeRootRegistry, create prefab materialization or touch Actor/Input/Camera/Reset/Save/Pooling. F6D must not start until Unity compilation/smoke confirms this cut.
