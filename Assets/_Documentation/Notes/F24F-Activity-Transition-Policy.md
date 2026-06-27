# IF-FW-F24F — Activity Transition Policy

## Status

Implemented / smoke pending.

## Context

F24E/F24E1 made `UIGlobal` the app/session-scoped source for global visual surfaces. F24E2 then accepted the visual operation policy:

- Route switch requires a transition.
- Route loading uses LoadingSurface during scene/content composition.
- Activity transition is policy-based and optional.
- Activity loading only occurs when Activity has real scene/content loading.

## Decision

`ActivityAsset` now owns a minimal visual transition policy:

```text
Seamless
Fade
FadeWithLoading
```

The policy only decides whether Activity Flow requests the Session `TransitionSurface`. It does not move ownership of `TransitionSurface` or `LoadingSurface` out of `UIGlobal`.

## Runtime behavior

| Activity transition mode | Current behavior |
|---|---|
| `Seamless` | Activity request/clear skips transition by policy. |
| `Fade` | Activity request/clear runs the Session `TransitionSurface`. |
| `FadeWithLoading` | Currently behaves as `Fade`; Loading remains skipped until Activity content/scene loading exists. |

Route transition behavior is unchanged and remains mandatory.

Activity loading behavior is unchanged and remains `SkippedNoSceneLoad` until a real Activity loading source exists.

## Activity clear rule

`ActivityClear` uses the policy of the Activity being cleared.

## QA configuration

For Unity Build Surface QA:

- primary transition activities use `Seamless`;
- alternate transition activities use `Fade`.

This allows smoke to validate both policy branches without creating new routes/scenes.

## Non-goals

- No Activity content scene loading.
- No LoadingSurface usage for Activity without a real Activity loading source.
- No Route transition change.
- No new scene loader.
- No Addressables.
- No Pause/Input/Camera/Audio/Player work.
