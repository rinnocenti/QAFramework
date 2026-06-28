# IF-FW-F25A — Activity Content Profile Contract

## Status
Implemented / historical contract cut

## Context

F24F introduced Activity visual transition policy. At that historical point, F24F1 marked `FadeWithLoading` as reserved because Activity did not yet own scene/content declarations.

Before this cut, Activity could switch identity, readiness and local content callbacks, but it could not declare Activity-owned scenes as content.

## Decision

F25 opens the Activity Content Scene Composition track.

F25A introduced the initial Activity content profile contract:

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

Historical F25A state: `FadeWithLoading` did not yet drive Activity scene loading presentation until later F25 execution cuts.

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


## F25J closure note

This note is historical. Later F25 cuts implemented Activity scene composition, release, operation planning, startup unification, ledger tracking and visual-mode diagnostics. `FadeWithLoading` is no longer reserved after F25I1/F25I2; it is the Activity visual mode that uses TransitionSurface plus LoadingSurface when the operation requests loading presentation.
