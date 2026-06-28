# IF-FW-F25I2 — Loading Skip Diagnostics Refinement

## Status

Implemented as a diagnostics-only refinement after F25I1.

## Problem

After F25I1, `Seamless` and `Fade` may execute Activity scene load/release side-effects without opening the canonical `LoadingSurface`.

The runtime behavior was correct, but Activity Request logs still reported:

```text
loading='SkippedNoSceneLoad'
```

That was misleading when the same result also reported:

```text
activitySceneComposition='Succeeded'
activitySceneCompositionLoaded='1'
activitySceneCompositionSideEffects='True'
```

## Decision

When an Activity request/clear does not open `LoadingSurface`, loading diagnostics now distinguish two cases:

```text
SkippedNoSceneLoad
```

No Activity scene load/release side-effect happened.

```text
SkippedByActivityPolicy
```

An Activity scene load/release side-effect happened, but the authored visual mode chose not to use `LoadingSurface`.

## Runtime behavior

No execution behavior changes.

- `Seamless` can still execute Activity scene load/release without TransitionSurface or LoadingSurface.
- `Fade` can still execute Activity scene load/release with TransitionSurface and without LoadingSurface.
- `FadeWithLoading` still opens LoadingSurface when the operation requires it.

## Expected smoke

For `Seamless + Activity scene composition`:

```text
activityTransitionMode='Seamless'
activityLoadingMode='ActivitySceneComposition'
activitySceneComposition='Succeeded'
activitySceneCompositionLoaded='1'
activitySceneCompositionSideEffects='True'
loading='SkippedByActivityPolicy'
loadingVisual='None'
```

For Activity requests with no Activity scene side-effect:

```text
loading='SkippedNoSceneLoad'
```
