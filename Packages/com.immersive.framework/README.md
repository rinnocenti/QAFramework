# Immersive Framework

`com.immersive.framework` is the Unity package for Immersive Framework runtime, authoring, diagnostics and QA surfaces.

## Documentation

Start here:

- [Package documentation](Documentation~/README.md)

The package documentation is for setup, authoring, runtime surfaces, QA smokes and troubleshooting. Project-side architecture tracking, closeouts, audits and consolidation roadmaps are not package usage documentation.

## Current public surface

- Game Application authoring through `GameApplicationAsset`.
- Route and Activity baseline through `RouteAsset`, `ActivityAsset`, route lifecycle and activity flow runtime surfaces.
- Optional app/session scoped `UIGlobal` scene for shared visual surfaces.
- Loading surface through `UnityLoadingSurfaceAdapter`.
- Transition surface through transition orchestration and `UnityFadeCurtainEffectAdapter`.
- Pause surface through resident `UIGlobal` presentation with `UnityPauseResidentSurfaceAdapter`.
- Pause input through `PauseInputActionRuntimeBridgeTrigger` and `PauseInputModeUnityPlayerInputRuntimeBridge`.
- RuntimeContent and ContentAnchor logical runtime, Unity materialization adapters, materialization bridges and composite release helpers.
- QA Canvas and smoke runners for package validation.

## Package boundaries

- Framework contracts and framework-specific Unity adapters live in this package.
- Technical primitives are consumed from `com.immersive.foundation`, `com.immersive.logging` and `com.immersive.pooling` instead of being reimplemented here.
- Runtime, Editor, package metadata and Unity assets are not documented as historical cuts. Use the package docs for the current supported state.
