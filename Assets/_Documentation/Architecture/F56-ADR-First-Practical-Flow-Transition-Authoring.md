# F56-ADR-First-Practical-Flow-Transition-Authoring

Status: Accepted / first practical flow transition authoring guide  
Date: 2026-07-03  
Track: TRANSITION-3 / First Practical Flow Transition Authoring  
Depends on: F34, F54, F55

## Context

F54 accepted the Transition Surface / Effects Contract. F55 hardened Transition runtime evidence so Route/Activity logs can show named Transition Effect adapter evidence through `transitionEffectAdapterEvidence*` fields.

The next practical gap is authoring usability: a project needs a compact path for configuring a first playable flow from startup/menu to gameplay route, activity switch and activity clear/restore using the runtime surfaces that already exist.

## Decision

F56 accepts a documentation-only authoring guide for the first practical Transition-backed flow.

The guide treats Transition as ready for documented practical use after F55 evidence hardening. It does not claim final game visuals, does not create sample assets and does not change runtime behavior.

## Authoring Reading

The supported first-flow mental model is:

- `GameApplicationAsset` starts the app and optionally loads the canonical `UIGlobal` scene.
- `UIGlobal` hosts shared visual surfaces such as Transition and Loading adapters.
- `RouteAsset` selects the top-level gameplay route and its primary scene.
- `ActivityAsset` selects gameplay state/content inside the active route.
- Transition covers the visual change before and after Route, Activity and ActivityClear operations.
- Loading communicates loading/progress when scene or content work needs it.

## Runtime Scope

F56 changes no runtime C# and introduces no public API. It only documents the current authoring path and expected diagnostics.

F56 does not create:

- visual game-ready prefabs;
- new scenes or sample assets;
- new Transition adapter;
- broad Surface layer;
- public GameFlow API;
- Pause visual changes;
- save/progression behavior.

## Validation

Static validation:

1. `git diff --check`
2. Confirm no runtime C# changed for F56.
3. Confirm no scenes, prefabs, serialized assets, ProjectSettings, package metadata, asmdefs or csproj changed for F56.

Unity/smoke validation is not required for F56 because it is documentation-only. Projects using the guide should validate with Unity import/compile, Standard Smoke, Activity Baseline Smoke, Route Scene Composition Smoke, Route Release Smoke and Transition smoke group as needed.

## Next Gate

Recommended next gate:

`F57 - FIRSTGAME-1 - Minimal Playable Framework Flow`

Alternative only if the team decides to create explicit example assets/scenes first:

`F57 - TRANSITION-4 - Authoring Sample Assets`
