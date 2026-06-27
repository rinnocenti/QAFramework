# IF-FW-F25C — Activity Scene Composition Execution

## Status
Implemented / Pending Unity smoke

## Context

F25A introduced `ActivityContentProfileAsset` and F25B introduced side-effect-free `ActivitySceneCompositionPlan/Result` diagnostics.

F25C is the first runtime execution cut for Activity-owned scenes.

## Decision

Activity scene composition now loads execution-ready Activity content scenes additively through `SceneLifecycleRuntime`.

The operation remains Activity-owned framework lifecycle/content core:

```text
ActivityAsset
  -> ActivityContentProfileAsset
      -> ActivityContentSceneEntry[]
          -> ActivitySceneCompositionPlan
          -> ActivitySceneCompositionRuntime
          -> ActivitySceneCompositionResult
```

## Loading boundary

When an Activity request has execution-ready scene content and a canonical `LoadingSurface` is available from `UIGlobal`, the Activity request executes inside the loading window:

```text
activity transition before, if Activity policy asks for transition
loading show
activity scene composition execution
activity local discovery / content callbacks / execution participants
loading hide
activity transition after, if Activity policy asks for transition
```

This mirrors the Route rule:

```text
transition fade-in
loading show
route scene/content composition
loading hide
transition fade-out
```

## Progress

Progress remains deferred.

Current diagnostics should still report:

```text
loadingProgressSupported='False'
loadingProgress='Indeterminate'
```

A future loading progress cut must aggregate Route scene composition and Activity scene composition without making `LoadingSurface` the owner of loading.

## Release boundary

Activity scene release is not implemented in this cut.

`ActivityContentReleasePolicy` remains authoring intent for a later cut:

```text
F25D — Activity Content Release
```

Therefore F25C may load Activity scenes additively, but unload/release policy is still deferred.

## Current expected diagnostics

For an Activity with an execution-ready content profile:

```text
activitySceneComposition='Succeeded'
activitySceneCompositionScenes='1'
activitySceneCompositionLoaded='1'
activitySceneCompositionSideEffects='True'
loading='SucceededWithUnitySurface'
loadingVisual='UnitySurface'
loadingProgressSupported='False'
loadingProgress='Indeterminate'
```

For an Activity without content scenes:

```text
activitySceneComposition='NotRequested'
loading='SkippedNoSceneLoad'
```

## Non-goals

F25C does not add:

- Activity content release/unload;
- Addressables;
- loading progress aggregation;
- actor/player materialization;
- camera/audio/input adapters;
- pause overlay behavior;
- runtime spawned/pooling.
