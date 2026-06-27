# IF-FW-F25A — Activity Content Profile Contract

## Status
Implemented / Unity validation pending

## Context

F24F introduced Activity visual transition policy and F24F1 marked `FadeWithLoading` as reserved because Activity did not yet own scene/content declarations.

Before this cut, Activity could switch identity, readiness and local content callbacks, but it could not declare Activity-owned scenes as content.

## Decision

F25 opens the Activity Content Scene Composition track.

F25A creates the declaration-only contract:

- `ActivityContentProfileAsset`
- `ActivityContentSceneEntry`
- `ActivityContentSceneLoadMode`
- `ActivityContentReleasePolicy`
- `ActivityAsset.ActivityContentProfile`

Activity content profiles declare Activity-owned scenes, explicit content ids, requiredness, intended load mode and release policy.

## Non-goals

F25A does not implement:

- Activity scene loading;
- Activity scene composition plan/result;
- Activity content release;
- LoadingSurface usage for Activity;
- Addressables;
- new lifecycle ownership;
- gameplay/actor/input/camera/audio adapters.

## Runtime behavior

Runtime behavior is unchanged.

Activity loading remains:

```text
loading='SkippedNoSceneLoad'
```

`FadeWithLoading` remains reserved until later F25 execution cuts.

## Authoring validation

The validator checks Activity content profile declarations for:

- explicit content id;
- duplicate content ids within a profile;
- valid project-relative scene path;
- scene asset existence;
- build settings warning;
- required entry without scene;
- unsupported load mode.

## Future work

- `IF-FW-F25B` — Activity Scene Composition Plan/Result
- `IF-FW-F25C` — Activity Scene Composition Execution
- `IF-FW-F25D` — Activity Content Release
- future loading progress only when there is a real progress source
