# F24A9 — Transition QA Activity Switch Panels

## Status

Applied / Pending Unity Validation

## Goal

Add isolated QA fixtures to validate Activity transition diagnostics without reusing the baseline framework QA scenes.

## Scope

This cut adds QA-only panels and installer tooling under `Assets/ImmersiveFrameworkQA/UnityBuildSurface`.

It creates alternate activity assets for the Transition QA routes and installs scene-local `ActivityRequestTrigger` components plus an IMGUI QA panel.

## Expected fixtures

- `Assets/ImmersiveFrameworkQA/UnityBuildSurface/Activities/QA_TransitionActivityA_Alt.asset`
- `Assets/ImmersiveFrameworkQA/UnityBuildSurface/Activities/QA_TransitionActivityB_Alt.asset`
- Activity request panel in `TransitionRouteA.unity`
- Activity request panel in `TransitionRouteB.unity`

## Menu

Use:

```text
Immersive Framework > QA > Unity Build Surface > Install Transition QA Activity Switch Panels
```

## Validation

1. Set `QA Transition Game Application` active.
2. Enter Play Mode.
3. In `TransitionRouteA`, request `Activity A Alt`, then request `Activity A`, then clear activity.
4. Switch to `TransitionRouteB`.
5. Request `Activity B Alt`, then request `Activity B`, then clear activity.
6. Confirm Activity Request logs include:

```text
transition='SucceededNoVisual'
transitionScope='Activity'
transitionBefore='SucceededNoVisual'
transitionAfter='SucceededNoVisual'
transitionBlockingIssues='0'
```

For clear activity, `transitionScope='ActivityClear'` is expected if the framework uses a dedicated scope for clear.

## Non-goals

- No transition visual.
- No loading screen.
- No pause overlay.
- No framework core changes.
- No lifecycle changes.
