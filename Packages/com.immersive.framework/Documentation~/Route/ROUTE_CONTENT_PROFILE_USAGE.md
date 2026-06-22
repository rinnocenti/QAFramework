# Route Content Profile Usage

Status: `F6 CLOSED / ROUTE SCENE COMPOSITION + RELEASE BASELINE PASS`

## What this profile does now

`RouteContentProfileAsset` is no longer planning-only in F6.

When a Route starts, the framework builds a `RouteSceneCompositionPlan`, executes it, produces a `RouteSceneCompositionResult`, and registers loaded scene handles in `RouteContentSet`.

Current supported execution:

```text
Primary Scene declared on RouteAsset -> LoadSceneMode.Single -> active scene
Additional Route Scene declared on RouteContentProfileAsset -> LoadSceneMode.Additive
Owned additional Route Scene on Route exit -> ContentReleasePlan -> UnloadScene
```

## What it does not do

`RouteContentProfileAsset` still does not materialize runtime prefabs, spawn actors, bind Content Anchors, configure input/camera/save, return objects to pools, or load Activity-owned content.

Those concerns remain deferred to later phases.

## Authoring setup

1. Select the `RouteAsset` that owns the additional scene.
2. In the Route Inspector, assign or create a `Content Profile`.
3. Open the `RouteContentProfileAsset`.
4. Add an entry under `Additional Route Scenes`.
5. Set an explicit stable `Content Id`, for example:

```text
qa-additive-route-content
```

6. Assign the Unity scene asset.
7. Ensure the scene is available to Unity scene loading through the active Build Profile or Shared Scene List.
8. Choose `Requiredness`:
   - `Required`: load failure blocks the Route composition.
   - `Optional`: load failure records an issue but does not block the Route composition.
9. Keep ownership as `Owned` when the framework is responsible for unloading the additional scene on Route exit.

## Additional scene guardrails

Do not put another `ImmersiveFrameworkBootstrap` in the additional scene.

Do not make the additional scene its own boot Route.

Do not rely on GameObject name, scene display name or hierarchy path as functional identity. Use explicit content ids for additional scenes.

## Expected boot diagnostics

With one Primary Scene and one owned additional scene:

```text
routeSceneComposition='Succeeded'
routeSceneLoaded='2'
routeSceneFailed='0'
routeSceneBlockingIssues='0'
routeContentHandles='2'
```

On the first boot, release counts should be zero because there is no previous Route to release:

```text
routeRelease='Succeeded'
routeReleaseReleased='0'
routeReleaseSkipped='0'
routeReleaseFailed='0'
routeReleaseBlockingIssues='0'
```

## Expected Route switch diagnostics

When switching away from the Route with one owned additional scene:

```text
routeRelease='Succeeded'
routeReleaseReleased='1'
routeReleaseSkipped='1'
routeReleaseFailed='0'
routeReleaseBlockingIssues='0'
```

Interpretation:

```text
Released 1 -> the owned additive scene was unloaded.
Skipped 1  -> the active Primary Scene was not manually unloaded; it remains controlled by LoadSceneMode.Single.
```

## QA validation

Use:

```text
Run Route Scene Composition Smoke
Run Route Release Smoke
```

For the canonical F6 QA setup with one Primary Scene and one additional scene, configure the QA Canvas as:

```text
Route Scene Composition Route = QA Canonical Route
Alternate Route = QA Alternate Route
Expected Route Scene Loaded Count = 2
Expected Route Scene Owned Loaded Count = 2
Expected Route Release Released Count = 1
```

F6 is considered valid when the composition smoke loads two owned scene handles and the release smoke unloads one owned additive scene while skipping the Primary Scene.
