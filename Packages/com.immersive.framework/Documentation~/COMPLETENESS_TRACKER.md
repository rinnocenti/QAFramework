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
| F7 | `CLOSED / CONTENT ANCHOR DECLARATION BASELINE PASS` | F7I closure completed after F7H smoke pass | `Planning/F7-Content-Anchor-Declaration-Audit.md`, `ContentAnchor/CONTENT_ANCHOR_IDENTITY_PRIMITIVES.md`, `ContentAnchor/CONTENT_ANCHOR_DECLARATION_MODEL.md`, `ContentAnchor/ROUTE_CONTENT_ANCHOR_AUTHORING.md`, `ContentAnchor/CONTENT_ANCHOR_SET.md`, `ContentAnchor/ROUTE_CONTENT_ANCHOR_DISCOVERY.md`, `ContentAnchor/CONTENT_ANCHOR_DIAGNOSTICS_SMOKE.md`, `ContentAnchor/CONTENT_ANCHOR_AUTHORING_VALIDATION.md`, `ADRs/F7-content-anchor-declaration/` |
| F8 | `OPEN / RUNTIME ROOTS AND MATERIALIZATION` | F8J logical release applied; F8 closure smoke pending | `Planning/F8-Runtime-Roots-Materialization-Audit.md`, `RuntimeContent/RUNTIME_OWNERSHIP_PRIMITIVES.md`, `RuntimeContent/RUNTIME_CONTENT_HANDLE.md`, `RuntimeContent/RUNTIME_SCOPE_ROOT_REGISTRY.md`, `RuntimeContent/RUNTIME_CONTENT_RUNTIME.md`, `RuntimeContent/RUNTIME_ROOT_LIFECYCLE_INTEGRATION.md`, `RuntimeContent/RUNTIME_MATERIALIZATION_REQUEST_RESULT.md`, `RuntimeContent/RUNTIME_TRANSITION_GUARD_SCOPED_CANCELLATION.md`, `RuntimeContent/RUNTIME_RELEASE_POLICY_LOGICAL_EXECUTION.md`, `ADRs/F8-runtime-roots-and-materialization/` |
| F9 | `PLANNED / CONTENT ANCHOR BINDING` | F9+ roadmap realigned; wait for F8 closure | `Planning/F9Plus-Roadmap-Realignment.md`, `ADRs/F9-content-anchor-binding-and-runtime-placement/` |
| F10 | `PLANNED / TRANSITION + ACTIVITY CONTENT` | New phase from NewScripts gap analysis | `ADRs/F10-transition-loading-and-activity-content/` |
| F11 | `PLANNED / PARTICIPATION + CAPABILITY RUNTIME` | New phase before intermediate consumers | `ADRs/F11-participation-and-capability-runtime/` |
| F12 | `PLANNED / INPUT SAVE PAUSE` | Former F10 renumbered and expanded | `ADRs/F12-intermediate-consumers/` |
| F13 | `PLANNED / ADVANCED CONSUMERS` | Former F11 renumbered | `ADRs/F13-advanced-consumers/` |
| F14 | `PLANNED / GAMEPLAY CAPABILITIES` | Former F12 renumbered | `Planning/Immersive-Framework-Roadmap-Revisado.md` |
| F15/FX | `PLANNED / PRODUCTIZATION HARDENING` | New hardening backlog | `Planning/Foundation-Hardening-Backlog.md` |

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
| `F8J — Runtime release policy / logical release execution` | APPLIED / PENDING COMPILE-SMOKE. Defines logical release request/result/policy, release adapter boundary and handle/scope release helpers without physical cleanup in the core. |

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
Materialização física runtime
Runtime spawned content
Actor/Input/Camera/Reset/Save/Pooling
Addressables backend
```


## F7 opening audit

Status: `OPEN / CONTENT ANCHOR DECLARATION`.

F7A applied the Content Anchor ADR/detail audit. It is documentation-only and does not change runtime behavior.

F7B introduced passive identity primitives and passed smoke validation:

```text
ContentAnchorId
ContentAnchorScope
ContentAnchorKind
ContentAnchorRequiredness
```

F7B does not add authoring components, discovery, validators, registry/set, smoke buttons or consumers.

Accepted naming decision:

```text
Content Anchor
ContentAnchorId
ContentAnchorScope
ContentAnchorKind
ContentAnchorRoot
ContentAnchorSlot
ContentAnchorPoint
ContentAnchorSet
```

Rejected for the canonical concept:

```text
Hook
Content Hook
Hook Content
```

F7A defines Content Anchor as a passive authored placement/reference contract inside loaded Route/Activity/Local content. It is not a scene loader, materializer, spawn system, camera rig, pause behavior, UI behavior, input policy, save system, pooling system or service locator.

F7A does not authorize:

```text
RuntimeRootRegistry
Materialização física runtime
RuntimeContentAnchorBinding
ContentAnchorBindingRequest/Result
Camera/Pause/UI/Actor/Audio consumers
Session/global anchors
Addressables backend
```


F7C introduced passive declaration models:

```text
ContentAnchorDeclaration
ContentAnchorRoot
ContentAnchorSlot
ContentAnchorPoint
```

F7C did not add authoring components, discovery, validators, registry/set, smoke buttons, runtime binding or consumers.

F7D introduced `RouteContentAnchor` as the first passive Route-scoped authoring component. It does not add discovery, validators, registry/set, smoke buttons, runtime binding or consumers.

F7E introduced the passive `ContentAnchorSet` model. It records unique `ContentAnchorDeclaration` items and local duplicate/invalid issues. It does not discover scene objects, integrate with Route lifecycle, validate authoring globally, emit logs, bind runtime content or serve gameplay consumers. F7E closed/pass by smoke.

F7F discovers scene-authored `RouteContentAnchor` components from loaded Route scenes into a local diagnostic `ContentAnchorSet`. It emits Content Anchor counts in boot/route request diagnostics. It does not validate required anchors, expose a registry, bind runtime content or serve gameplay consumers. F7F closed/pass by smoke with one valid anchor and zero issues.

F7G adds `Run Content Anchor Diagnostics Smoke` and trims the visible QA Canvas buttons to the current validation path. It does not add validators, required-anchor blocking, Activity anchors, runtime binding/placement, RuntimeRootRegistry, materialização física runtime or gameplay consumers. F7G closed/pass by smoke.

F7H adds loaded Route Content Anchor authoring validation to Project Settings validation, the Route Content Anchor Inspector and the QA Canvas `Validate Loaded Authoring` path. It detects missing Route, missing Anchor Id, `Kind = Unknown`, invalid requiredness, scene/Route declaration mismatch, duplicate identity and duplicate owner/scope/Anchor Id. It does not block Route lifecycle, enforce required anchors, add Activity anchors, bind placement or serve consumers.

F7 is closed. F8 is the active phase; current next authorized cut is tracked in the F8 section below.


## F8 opening audit

F8A accepted the runtime roots/materialization boundary as documentation-only. F8B added passive runtime ownership primitives. F8C added passive runtime content handles and release-state transition diagnostics. F8D added logical runtime scope roots and an internal minimal registry. F8E added the internal RuntimeContentRuntime owner and explicit RuntimeScopeContext. F8F integrated logical runtime root/context creation and removal into Session, Route and Activity lifecycles. F8G added `RuntimeMaterializationRequest`, `RuntimeMaterializationResult`, `RuntimeMaterializationResource` and `RuntimeMaterializationStatus` as explicit contracts. F8H added scoped transition guardrails and `RuntimeScopeCancellationToken` so materialization requests can be rejected when their owner scope is cancelling, removed or stale. F8I added `IRuntimeMaterializationAdapter` as the boundary for physical adapters outside the RuntimeContent core. F8J added `RuntimeReleaseRequest`, `RuntimeReleaseResult`, `RuntimeReleasePolicy`, `RuntimeReleaseStatus`, `IRuntimeReleaseAdapter` and logical release helpers by handle/scope. Runtime behavior still does not instantiate, destroy, create hierarchy root GameObjects, unload scenes, return pools, release Addressables handles or bind anchors.

F8 has implemented:

- runtime ownership/scope/state primitives (`RuntimeContentScope`, `RuntimeContentState`, `RuntimeContentId`, `RuntimeContentOwner`, `RuntimeContentIdentity`);
- passive runtime content handle and transition diagnostics (`RuntimeContentHandle`, `RuntimeContentHandleTransitionStatus`, `RuntimeContentHandleTransitionResult`);
- logical runtime scope root and internal registry (`RuntimeScopeRoot`, `RuntimeRootRegistry`, `RuntimeRootRegistryOperationStatus`, `RuntimeRootRegistryOperationResult`);
- internal runtime content owner and explicit scope context (`RuntimeContentRuntime`, `RuntimeScopeContext`);
- lifecycle-driven logical root/context diagnostics (`RuntimeScopeLifecycleResult`);
- materialization request/result contracts (`RuntimeMaterializationRequest`, `RuntimeMaterializationResult`, `RuntimeMaterializationResource`, `RuntimeMaterializationStatus`);
- transition guard/scoped cancellation (`RuntimeScopeTransitionState`, `RuntimeScopeCancellationToken`, internal transition guard/result/status);
- materialization adapter boundary (`IRuntimeMaterializationAdapter`).

F8 is still allowed to define and implement:

- F8 closure smoke;
- F8 closure smoke.

F8 does not authorize:

- Content Anchor runtime binding;
- Transition/loading implementation;
- ActivityContentProfile execution;
- Activity Content Anchor;
- Actor/Pause/Camera/UI/Input/Save/Pooling consumers;
- pooled materialization;
- service locator roots;
- `GameObject.Find` root lookup;
- fallback root creation when a required root is absent.

F9+ was realigned after this point. The realignment is documented in `Planning/F9Plus-Roadmap-Realignment.md` and does not mark any F9+ runtime implementation as applied.

Next authorized cut:

```text
F8J — runtime release policy / logical release execution [APPLIED / PENDING COMPILE-SMOKE]
F8K — runtime request/guard/release-policy smoke and F8 closure
```
