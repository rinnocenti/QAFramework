# F58-ADR-Minimal-Authoring-Validation-Project-Readiness

Status: Implemented locally / pending Unity validation
Date: 2026-07-03
Track: MODEL-2 / Minimal Authoring Validation / Project Readiness
Depends on: F34, F57

## Context

F57 accepted the minimum Immersive Framework 1.0 Model/Authorship boundary: `GameApplicationAsset`, `RouteAsset`, `ActivityAsset`, `UIGlobal`, existing Loading/Transition/Pause adapters, and existing content profile / Content Anchor declarations where they are already used.

That contract needs an Editor-only readiness check so a consumer project can know whether its authored setup is package-ready without running gameplay, creating fallback, creating assets or changing runtime behavior.

## Decision

F58 implements an Editor-only Model Readiness validator:

`FrameworkAuthoringModelReadinessValidator`

The validator consolidates the existing `FrameworkAuthoringValidator` path and adds F57-specific readiness checks. It reports evidence only. It does not create assets, modify settings, change Build Settings, auto-assign scenes, add adapters, or change runtime execution.

The canonical entry point is Project Settings > Immersive Framework > Model Readiness > `Run Model Readiness Check`.

## Implemented Validation Shape

The readiness check returns the existing `FrameworkAuthoringValidationReport` shape with:

- total issue count;
- blocking issue count through `ErrorCount`;
- warning count;
- info count;
- optional skip count through `OptionalSkipCount`;
- explicit per-issue messages and Unity object context where available.

`FrameworkAuthoringValidationReport` keeps the existing Error/Warning/Info severity model. F58 adds optional skip counting without creating a new public API or changing runtime contracts.

## Validation Coverage

Implemented coverage:

- missing framework settings;
- missing active `GameApplicationAsset`;
- missing `StartupRoute`;
- invalid serialized `ValidationMode`;
- invalid serialized `GlobalUiScenePolicy`;
- inconsistent `UIGlobal` no-op policy with an assigned scene;
- required `UIGlobal` scene missing, invalid or outside Build Settings;
- required `UIGlobal` missing Transition adapter;
- required `UIGlobal` missing Loading adapter;
- resident Pause adapter count, reported as optional skip when absent because no serialized Pause-expected policy exists yet;
- startup Route missing primary scene;
- startup Route primary scene outside Build Settings;
- startup Activity validation when assigned;
- startup Activity absence as optional skip because no route-level requires-startup-activity policy exists yet;
- invalid serialized `ActivityVisualTransitionMode`;
- Activity Content Profile scenes where assigned;
- Route Content Profile scenes where assigned;
- open-scene Content Anchor and materialization bridge validation through the existing authoring validator path when requested.

F58 intentionally does not invent a readiness profile asset. Build Settings remain the only project readiness scene source for this cut.

## Explicit Non-Fallback Rule

The validator is non-destructive and idempotent. It only observes assets, scenes and open-scene authoring components.

Required missing configuration produces explicit errors. Optional absence produces an optional skip/info diagnostic when the current model has no field that can make it required.

F58 does not silently replace missing scenes, routes, activities or adapters, and it does not infer identity from object names beyond existing diagnostic labels.

## Package Git Readiness Impact

F58 makes package readiness auditable from a consumer project before FIRSTGAME.

A project is not considered Git-package-ready until:

- Unity imports/compiles the package;
- the Model Readiness check runs;
- blocking issues are resolved or consciously documented;
- package installation/release details are handled by the next gate.

Git URL pinning, package dependency pinning, release tags and package installation policy remain for:

`F59 - PACKAGE-1 - Git Package Readiness`

## Rejected Scope

F58 does not:

- alter runtime C#;
- alter runtime behavior;
- create a generic Model layer;
- create public runtime API;
- create service locator/singleton behavior;
- create scenes, prefabs, serialized assets or ProjectSettings changes;
- create or modify `package.json`, asmdefs or csproj;
- create a new materialization consumer;
- promote Pause visual materialization;
- add QA Canvas buttons.

## Validation Plan

Static validation:

1. `git diff --check`
2. Confirm changes are Editor/docs/ADR only.
3. Confirm no scenes, prefabs, serialized assets, ProjectSettings, runtime C#, package metadata, asmdefs or csproj changed.
4. Confirm the validator does not call `CreateAsset`, `SaveAssets`, `SetDirty`, `Undo.RecordObject`, `EditorBuildSettings.scenes =` or runtime execution paths.

Unity validation required before closing:

1. Unity import/compile.
2. Run Project Settings > Immersive Framework > Model Readiness > `Run Model Readiness Check`.
3. Confirm the current QA project has zero blocking readiness issues or document each blocker.
4. If runtime smoke paths are touched by later fixes, run Standard Smoke, Activity Baseline Smoke and Route Scene Composition Smoke.

## Next Gate

Recommended next gate:

`F59 - PACKAGE-1 - Git Package Readiness`

FIRSTGAME remains deferred until package readiness is accepted.
