# IF-FW-F24E2 — Route/Activity Visual Operation Policy

## Status
Accepted / Documentation Only

## Context

F24E and F24E1 moved global visual surfaces to `UIGlobal`.

The validated shape is:

```text
Session / FrameworkRuntimeHost
  -> UIGlobal persistent scene
      -> Transition Surface Adapter
      -> Loading Surface Adapter
```

## Decision

- `UIGlobal` is app/session-scoped.
- `TransitionSurface` and `LoadingSurface` are Session UI capabilities.
- Route switch requires transition.
- Route loading uses LoadingSurface during scene/content composition.
- Activity transition is optional and policy-based.
- Activity loading only occurs when Activity has real scene/content loading.

## Boundary

- `FrameworkRuntimeHost` owns the session-scoped visual capabilities.
- Route and Activity request visual operations through those capabilities.
- Transition does not belong to Route ownership.
- Loading does not belong to Activity ownership.

## Canonical Route Sequence

```text
transition fade-in
loading show
route scene/content composition
loading hide
transition fade-out
```

## Canonical Activity Policy

```text
None / Seamless
Fade
FadeWithLoading
```

## Non-goals

- No runtime implementation.
- No new Activity policy fields yet.
- No Addressables.
- No Pause.
- No Camera/Input/Audio/Player.
- No new lifecycle.
- No fallback to prefab surfaces.

## Future Work

- `IF-FW-F24F`: Activity Transition Policy.
- Future Loading: progress support only when a real progress source exists.
