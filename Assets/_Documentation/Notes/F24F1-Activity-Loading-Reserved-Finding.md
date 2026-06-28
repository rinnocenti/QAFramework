# IF-FW-F24F1 — Activity Loading Reserved Finding

## Status

Superseded / historical pre-F25 finding.

F25I1/F25I2 replace this finding. `FadeWithLoading` is no longer reserved; it is the Activity visual mode that uses TransitionSurface and LoadingSurface when the Activity operation requests loading presentation. `Seamless`, `Fade` and `FadeWithLoading` may all execute Activity scene load/release.

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

At this historical point, `FadeWithLoading` was unavailable for Activity loading until Activity Content Scene Composition existed.

This no longer describes current runtime behavior after F25I1/F25I2.

## Current behavior

| Mode | Current behavior |
|---|---|
| `Seamless` | Skips Activity transition by policy. |
| `Fade` | Uses the Session `TransitionSurface`; Activity loading remains skipped. |
| `FadeWithLoading` | Historical F24F1 behavior only. Current F25 behavior uses TransitionSurface and LoadingSurface when the Activity operation requests loading presentation. |

The request-level loading fields remain explicit:

```text
loading='SkippedNoSceneLoad'
loadingVisual='None'
loadingBefore='Skipped'
loadingAfter='Skipped'
```

This was correct only before Activity had real scene/content loading.

## Guard

Current authoring validation no longer warns only because an Activity uses `FadeWithLoading`.

Current Inspector text presents `FadeWithLoading` as an active visual mode.

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
