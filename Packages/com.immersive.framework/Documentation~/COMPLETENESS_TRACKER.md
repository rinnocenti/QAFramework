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
| F6 | `CLOSED / ROUTE SCENE COMPOSITION + RELEASE BASELINE PASS` | F6G release smoke passed; F6 closed | `Planning/F6-Route-Scene-Composition-Audit.md`, `Route/ROUTE_CONTENT_PROFILE_USAGE.md`, `Route/ROUTE_SCENE_COMPOSITION_SMOKE.md`, `Route/ROUTE_RELEASE_SMOKE.md`, `ADRs/F6-route-scene-composition-and-release/` |

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
| `F7A — Content Anchor ADR/detail audit` | F6 closed the Route scene composition/release baseline. Next work may define Content Anchor declaration only; do not start RuntimeRoot/materialization or gameplay consumers yet. |

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
- no canonical materialization, Content Anchor, Actors, Input, Camera, Reset, Snapshot, Save, Pooling, runtime references, release/unload policy or expected contribution asset in F5.

## F6 closure audit

Status: `CLOSED / ROUTE SCENE COMPOSITION + RELEASE BASELINE PASS`.

Implemented cuts:

```text
F6A — ADR completion and audit                  [CLOSED / DOCS]
F6B — RouteSceneCompositionPlan                 [CLOSED / PASS]
F6C — RouteSceneCompositionResult               [CLOSED / PASS]
F6D — SceneLifecycle additive primitive         [CLOSED / PASS]
F6E — RouteContentProfileAsset execution        [CLOSED / PROFILE SMOKE PASS]
F6F — ContentReleasePlan / ContentReleaseResult [CLOSED / PASS]
F6G — Scene release execution                   [CLOSED / RELEASE SMOKE PASS]
```

F6 closed the first real Route scene composition and release loop:

- `RouteSceneCompositionPlan` and `RouteSceneCompositionResult` exist and are consumed by Route lifecycle.
- `RouteContentProfileAsset` additional scenes are loaded additively when valid.
- Required additional scene load failure blocks Route composition.
- Optional additional scene load failure remains non-blocking.
- Loaded scene handles are registered in `RouteContentSet`.
- `ContentReleasePlan` and `ContentReleaseResult` exist.
- Owned additive Route scenes are unloaded on Route exit.
- Active Primary Scene is skipped by manual release and remains controlled by `LoadSceneMode.Single`.

F6G PASS evidence:

```text
routeRelease='Succeeded'
routeReleasePlanned='2'
routeReleaseReleased='1'
routeReleaseSkipped='1'
routeReleaseFailed='0'
routeReleaseIssues='0'
routeReleaseBlockingIssues='0'
```

Restore composition evidence:

```text
routeSceneComposition='Succeeded'
routeSceneEntries='2'
routeSceneLoaded='2'
routeSceneOwnedLoaded='2'
routeSceneFailed='0'
routeSceneBlockingIssues='0'
routeContentHandles='2'
```

Smokes covered:

```text
Standard Smoke PASS
Activity Baseline Smoke PASS
Route Scene Composition Smoke PASS
Route Release Smoke PASS
Route Callback Smoke PASS
Local Contribution Smoke PASS
Loaded Local Contributions authoring validation PASS
```

Accepted ADRs:

- `ADRs/F6-route-scene-composition-and-release/F6-01-ADR-RELEASE-001-content-release-plan-by-scope.md`;
- `ADRs/F6-route-scene-composition-and-release/F6-02-ADR-SCENE-001-route-scene-composition-plan-and-result.md`.

F6 does not authorize:

```text
Activity canonical materialization
Activity release execution
Content Anchor
RuntimeRootRegistry
Prefab materialization
Runtime spawned content
Actor/Input/Camera/Reset/Save/Pooling
Addressables backend
```
