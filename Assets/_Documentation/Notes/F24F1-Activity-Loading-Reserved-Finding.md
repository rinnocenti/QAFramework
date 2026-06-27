# IF-FW-F24F1 — Activity Loading Reserved Finding

## Status

Accepted / Documentation + Guard.

## Context

F24F introduced Activity visual transition policy through `ActivityVisualTransitionMode`:

```text
Seamless
Fade
FadeWithLoading
```

The cut exposed a missing framework capability: Activity does not yet own scene/content composition.

Current Activity supports:

- Activity identity;
- Activity state;
- readiness;
- local content callbacks/bindings;
- visual transition policy.

Current Activity does not yet support:

- `ActivityContentProfile`;
- `ActivitySceneEntry`;
- Activity scene composition plan/result;
- Activity-owned additive scene loading;
- Activity content release.

## Decision

`FadeWithLoading` is reserved until Activity Content Scene Composition exists.

It must not imply real Activity loading in the current runtime.

## Current behavior

| Mode | Current behavior |
|---|---|
| `Seamless` | Skips Activity transition by policy. |
| `Fade` | Uses the Session `TransitionSurface`; Activity loading remains skipped. |
| `FadeWithLoading` | Reserved. Uses the same fade behavior for now, logs `activityLoadingMode='ReservedNoActivityContentLoading'`, and keeps Activity loading skipped. |

The request-level loading fields remain explicit:

```text
loading='SkippedNoSceneLoad'
loadingVisual='None'
loadingBefore='Skipped'
loadingAfter='Skipped'
```

This is correct until Activity has real scene/content loading.

## Guard

Authoring validation warns when an Activity uses `FadeWithLoading` because no `ActivityContentProfile` execution exists yet.

The Inspector also marks `FadeWithLoading` as reserved.

## Non-goals

- No Activity scenes.
- No `ActivityContentProfile`.
- No additive Activity scene loading.
- No Activity content release.
- No LoadingSurface use for Activity without real content loading.
- No Route transition/loading change.
- No new scene loader.
- No Addressables.

## Future work

Open a dedicated Activity content composition track:

- `IF-FW-F25A` — Activity Content Profile Contract;
- `IF-FW-F25B` — Activity Scene Composition Plan/Result;
- `IF-FW-F25C` — Activity Scene Composition Execution;
- `IF-FW-F25D` — Activity Content Release.
