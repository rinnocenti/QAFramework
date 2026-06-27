# IF-FW-F25D1 — Activity Release Policy Semantics

## Status
Accepted / Runtime correction

## Context
F25D introduced Activity-owned additive scene release. The initial wording used `ReleaseOnActivityExit` / `KeepUntilRouteExit`, which made the Activity policy look like it controlled Route lifetime.

That was wrong.

Route lifetime is not an Activity policy. Content that survives Route changes is Session content, not Route or Activity content.

## Decision
Activity content release policy controls only what happens when the active Activity changes or is cleared:

- `ReleaseOnActivityChange`: unload the Activity-owned scene when the Activity is replaced or cleared.
- `KeepOnActivityChange`: keep the Activity-owned scene loaded when the Activity is replaced or cleared.

When the Route changes, all Activity-owned scenes from the previous Route are force-released regardless of Activity policy.

## Route content rule
Route content does not expose a release policy. Route content is valid only for its owning Route and is always released during Route change.

If content must outlive Route changes, it belongs to Session content, not Route content.

## Runtime rule
Activity change / clear:

```text
ReleaseOnActivityChange -> unload
KeepOnActivityChange    -> keep loaded
```

Route change:

```text
ReleaseOnActivityChange -> force unload
KeepOnActivityChange    -> force unload
```

## Diagnostics
Route requests may report forced Activity-scene release with:

```text
routeActivitySceneRelease
routeActivitySceneReleaseScenes
routeActivitySceneReleaseReleased
routeActivitySceneReleaseSkipped
routeActivitySceneReleaseFailed
routeActivitySceneReleaseSideEffects
routeActivitySceneReleaseBlockingIssues
```

Activity requests still report Activity-change release with:

```text
activitySceneRelease
activitySceneReleaseReleased
activitySceneReleaseSkipped
```

## Non-goals
- No Session persistent content is introduced in this cut.
- No Route release policy is introduced.
- No Loading progress is introduced.
- No Addressables integration is introduced.
