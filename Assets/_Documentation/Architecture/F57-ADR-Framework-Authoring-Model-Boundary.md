# F57-ADR-Framework-Authoring-Model-Boundary

Status: Accepted / model boundary
Date: 2026-07-03
Track: MODEL-1 / Framework Authoring Model Boundary
Depends on: F34, F56

## Context

F56 documented the first practical Transition-backed flow using the current `GameApplicationAsset`, `UIGlobal`, `RouteAsset`, `ActivityAsset`, Transition and Loading paths.

Before opening a FIRSTGAME implementation gate, the framework needs a minimal package-usable authoring model. A consumer project must understand which assets and scene-authored components describe the game for the framework, which runtime objects execute those descriptions, and which validation questions must be answered before Git package readiness can be claimed.

F57 is ADR-only and contract-first. It changes no runtime, editor tooling, scenes, prefabs, serialized assets, package metadata, asmdefs or ProjectSettings.

## Decision

F57 accepts a minimal Immersive Framework 1.0 Model/Authorship boundary.

Model means authored data and configuration that describe the game to the framework:

- application startup and shared UI policy;
- route navigation and route scene/content declarations;
- activity state/content declarations;
- shared surface configuration through existing `UIGlobal` adapters;
- existing content profile and Content Anchor declarations needed for current route/activity/content flows.

Model does not mean gameplay domain model, save model, actor model, inventory, entity system, database, universal surface layer or a generic model API.

FIRSTGAME is deferred until the minimal Model boundary and package readiness validation are clearer. The next gate is:

`F58 - MODEL-2 - Minimal Authoring Validation / Project Readiness`

## Model Definition

The authoring model is descriptive. Runtime execution remains owned by existing runtime boundaries.

| Model area | Describes | Does not own |
| --- | --- | --- |
| Game Application Model | App startup route, validation mode and `UIGlobal` policy. | Route execution, scene loading internals, global singleton/bootstrap expansion. |
| Route Model | Top-level route, primary scene, optional startup activity and optional route content profile. | Activity runtime, global visual surfaces, gameplay object spawning. |
| Activity Model | Activity identity, optional content profile and visual transition intent. | Route lifecycle, actor/input/camera/save systems. |
| Surface Model | Existing shared Loading, Transition and Pause presentation configuration through `UIGlobal` and adapters. | Generic Surface layer, manager/coordinator, automatic adapter discovery outside the accepted host. |
| Content/Anchor Model | Existing route/activity content profiles and explicit Content Anchor declarations. | Broad materialization consumer expansion, gameplay inventory/entity model. |

The boundary rule is:

- runtime executes;
- model describes;
- Unity adapter applies local Unity-side effects;
- bridge connects Inspector/authorship to an explicit runtime boundary.

## Current Authoring Assets

Current 1.0 authoring candidates are the assets and components that already exist in `com.immersive.framework`.

| Asset or component | Current role | F57 classification |
| --- | --- | --- |
| `GameApplicationAsset` | Root app asset with `StartupRoute`, `ValidationMode`, `GlobalUiScenePolicyValue`, `GlobalUiScenePath` and `GlobalUiSceneName`. | Game Application Model. |
| `RouteAsset` | Route asset with `PrimaryScenePath`, `RouteContentProfile` and optional `StartupActivity`. | Route Model. |
| `ActivityAsset` | Activity asset with optional `ActivityContentProfile` and `ActivityVisualTransitionMode`. | Activity Model. |
| `RouteContentProfileAsset` | Route-owned additional scene declarations. | Content Model, route-scoped. |
| `ActivityContentProfileAsset` | Activity-owned scene declarations used by Activity planning/composition/release. | Content Model, activity-scoped. |
| `RouteContentAnchor` | Scene-authored passive route Content Anchor declaration. | Content/Anchor Model, route-scoped. |
| `ActivityContentAnchor` | Scene-authored passive activity Content Anchor declaration. | Content/Anchor Model, activity-scoped. |
| `UIGlobal` scene authored through `GameApplicationAsset` | App/session scoped host for shared presentation adapters. | Surface Model host, not a generic surface layer. |
| `UnityFadeCurtainEffectAdapter` / `ITransitionEffectAdapter` | Existing Transition effect adapter path. | Surface Model configuration for Transition. |
| `UnityLoadingSurfaceAdapter` / `ILoadingSurfaceAdapter` | Existing Loading adapter path. | Surface Model configuration for Loading. |
| `UnityPauseResidentSurfaceAdapter` / `IPauseSurfaceAdapter` | Current resident Pause presentation adapter path. | Surface Model configuration for resident Pause. |
| `PauseVisualSurfaceAuthoring` | Experimental/frozen passive Pause visual contract over RuntimeContent/ContentAnchor. | Not part of minimum 1.0 Model; reference-only unless a future consumer gate promotes it. |

## Ownership Map

| Concern | Owner |
| --- | --- |
| Model/Authorship language | `com.immersive.framework` documentation and authoring assets. |
| Game Application startup contract | `GameApplicationAsset` and `FrameworkRuntimeHost`. |
| `UIGlobal` loading and adapter collection | `GlobalUiSceneRuntime` as internal runtime surface host. |
| Route scene lifecycle | `RouteAsset`, route lifecycle runtime and scene lifecycle runtime. |
| Activity flow/content lifecycle | `ActivityAsset` and activity flow runtime. |
| Transition execution | Transition runtime/effect adapters. |
| Loading execution | Loading surface runtime/adapters. |
| Resident Pause presentation | Pause runtime/surface adapters. |
| Content profiles | Route/Activity authoring profiles and existing route/activity execution paths. |
| Content Anchor declarations | `RouteContentAnchor`, `ActivityContentAnchor` and ContentAnchor runtime validation/evidence paths. |
| Project readiness validation | Editor validation/readiness tooling, to be consolidated by F58. |

## Minimum 1.0 Authoring Model

The minimum practical model for a package consumer is:

1. A `GameApplicationAsset` assigned in framework settings, with a `StartupRoute`, `ValidationMode` and explicit `GlobalUiScenePolicy`.
2. A `UIGlobal` scene when shared visuals are required, referenced by project-relative scene path and included in Build Settings.
3. At least one Transition adapter in `UIGlobal` when route/activity transitions are expected.
4. A Loading adapter in `UIGlobal` when loading/progress presentation is expected.
5. A resident Pause adapter in `UIGlobal` when shared Pause presentation is expected.
6. A `RouteAsset` for each top-level route, with a primary scene path and optional startup activity.
7. Optional `RouteContentProfileAsset` only when route-owned additive content scenes are required.
8. Optional `ActivityAsset` for activity flow, with optional `ActivityContentProfileAsset` and `ActivityVisualTransitionMode`.
9. Optional route/activity Content Anchors only when existing ContentAnchor declarations are needed by content/materialization paths.

This model is sufficient for package documentation and readiness validation. It does not require sample assets or a new public API.

## What Is Not Model Yet

The following are rejected from the minimum 1.0 Model:

- gameplay actor/player/enemy/item model;
- save, preferences or progression model;
- inventory/entity/database model;
- generic `ModelAsset`, universal model registry or service locator;
- generic Surface manager/coordinator/processor;
- public GameFlow request API;
- automatic runtime materialization consumer expansion;
- Pause visual/materialization promotion beyond the current experimental/frozen contract;
- sample scenes, prefabs or serialized example assets.

## Validation Needed For F58

F58 should implement or consolidate the minimum readiness validation without changing runtime behavior by fallback.

Required validation questions:

- `GameApplicationAsset` missing `StartupRoute`;
- `RouteAsset` missing primary scene;
- route primary scene missing from Build Settings or the active project readiness profile;
- route startup activity missing when a route is declared to require an initial activity;
- required `UIGlobal` scene missing or not included in Build Settings;
- Transition expected but no Transition adapter is present in `UIGlobal`;
- Loading expected but no Loading adapter is present in `UIGlobal`;
- Pause expected but no resident Pause adapter is present in `UIGlobal`;
- route/activity content profile scene entries missing, invalid or outside Build Settings/profile;
- Content/Anchor config invalid only where fields and validators already exist.

F58 should preserve the existing no silent fallback rule. Missing required configuration must become explicit validation evidence.

## Package Git Readiness Relationship

The Model boundary is a prerequisite for practical Git package use because consumers need a small set of authored assets and scenes they can create without copying project-specific Base 2.0 assets or `ProjectSettings`.

F57 does not pin Git URLs, change `package.json`, create package release tags or alter dependency versions. Package Git readiness remains blocked until F58 validates the minimum authoring setup and future install/release work pins package dependencies reproducibly.

## Rejected Scope

F57 does not:

- alter runtime C#;
- alter editor C#;
- create scenes, prefabs or sample assets;
- change serialized assets or ProjectSettings;
- change package metadata, asmdefs or csproj;
- create a lifecycle runtime;
- introduce service locator/singleton/bootstrap behavior;
- create a generic model layer;
- move ownership into technical packages.

## Next Gate

Next gate:

`F58 - MODEL-2 - Minimal Authoring Validation / Project Readiness`

FIRSTGAME remains deferred until the minimal Model validation/readiness gate is accepted.
