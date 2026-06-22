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
| F6 | `OPEN / F6G APPLIED / PENDING RELEASE SMOKE` | F6F passed; F6G executes ContentReleasePlan for owned additive Route scenes and adds Route Release Smoke | `Planning/F6-Route-Scene-Composition-Audit.md`, `ADRs/F6-route-scene-composition-and-release/` |

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
| `F6G — Scene release execution / release smoke` | Release execution added for owned additive Route scenes. Validate compile, baseline smokes, Route Scene Composition Smoke and Route Release Smoke. |

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

Status: `OPEN / F6G APPLIED / PENDING RELEASE SMOKE`.

F6A completed the route scene composition and release ADR gate. F6B added runtime planning types for Route scene composition and passed baseline smoke. F6C added runtime result types for Route scene composition and passed baseline smoke. F6D added an internal SceneLifecycle additive load primitive and passed baseline smoke. F6E connected RouteContentProfileAsset execution through RouteSceneCompositionPlan/Result and was validated with a profile/additional-scene smoke: routeSceneLoaded='2', routeSceneOwnedLoaded='2', routeSceneFailed='0', routeSceneBlockingIssues='0', routeContentHandles='2'. F6F adds ContentReleasePlan/ContentReleaseResult models and RouteContentSet release planning, and passed baseline/profile smokes. F6G executes the Route release plan for owned additive scenes through SceneLifecycle unload, records ContentReleaseResult in RouteLifecycleStartResult diagnostics and adds Route Release Smoke. Surface, RuntimeRoot and prefab materialization remain out of scope.

Accepted ADRs:

- `ADRs/F6-route-scene-composition-and-release/F6-01-ADR-RELEASE-001-content-release-plan-by-scope.md`;
- `ADRs/F6-route-scene-composition-and-release/F6-02-ADR-SCENE-001-route-scene-composition-plan-and-result.md`.

Audit doc:

- `Planning/F6-Route-Scene-Composition-Audit.md`.

Current validation gate:

```text
F6G — Scene release execution / release smoke
```

F6B, F6C, F6D, F6E and F6F are closed by smoke evidence. F6E wires additional scenes into Route lifecycle through scene composition execution. F6F introduces release planning/result vocabulary and builds a Route release plan from RouteContentSet: active Primary Scene receives no manual unload action; owned additive Route scenes receive UnloadScene intent; Registered and DiagnosticOnly content receive no release action. F6G executes only `UnloadScene` actions for owned additive Route scenes before the next Route composition. It does not destroy objects, return pool entries, create Surface, create RuntimeRootRegistry, create prefab materialization or touch Actor/Input/Camera/Reset/Save/Pooling.
