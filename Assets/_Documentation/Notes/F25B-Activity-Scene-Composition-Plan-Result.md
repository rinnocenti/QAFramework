# IF-FW-F25B — Activity Scene Composition Plan/Result

## Status
Implemented / smoke pending

## Context

F25A added `ActivityContentProfileAsset` and scene declarations, but Activity still had no composition evidence in runtime diagnostics.

F24F/F24F1 already separated Activity visual transition policy from Activity loading. `FadeWithLoading` remains reserved until Activity scene/content loading exists.

## Decision

F25B introduces side-effect-free Activity scene composition planning and result evidence.

Activity scene composition is now represented as:

```text
ActivityAsset
  -> ActivityContentProfileAsset
      -> ActivityContentSceneEntry[]
          -> ActivitySceneCompositionPlan
          -> ActivitySceneCompositionResult
```

The result records whether declarations are execution-ready for later F25 cuts.

## Runtime behavior

F25B does not load scenes and does not release content.

Activity requests now record diagnostics such as:

```text
activitySceneComposition='Planned'
activitySceneCompositionProfile='<profile>'
activitySceneCompositionScenes='1'
activitySceneCompositionRequired='1'
activitySceneCompositionOptional='0'
activitySceneCompositionExecutionReady='1'
activitySceneCompositionBlockingIssues='0'
```

If the Activity has no profile, the result is `NotRequested`.

## Non-goals

- No additive Activity scene loading.
- No LoadingSurface for Activity yet.
- No Activity content release.
- No Addressables.
- No new lifecycle pipeline.
- No gameplay adapter work.

## Future work

- `IF-FW-F25C` — Activity Scene Composition Execution.
- `IF-FW-F25D` — Activity Content Release.
