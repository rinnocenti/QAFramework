# Route Scene Composition Smoke

Status: `F6 CLOSED / PASS`

Use this smoke to validate that a `RouteContentProfileAsset` declared on a Route is consumed by Route scene composition and that valid additional scenes are loaded additively.

## Authoring setup

1. Select the Route that will own the additional scene. Prefer the canonical QA route while validating F6.
2. In the Route Inspector, assign or create a `Content Profile`.
3. Select the `Route Content Profile` asset.
4. In `Additional Route Scenes`, click `Add Scene`.
5. Fill `Content Id` with a stable explicit id, for example `qa-additive-route-content`.
6. Assign a valid Unity scene in `Scene`.
7. Ensure the additional scene is available to Unity scene loading through the active Build Profile or Shared Scene List.
8. Set `Requiredness`:
   - `Required` when the Route must fail if this additional scene cannot load.
   - `Optional` when the Route may continue and report a non-blocking issue.
9. Keep the additional scene `Owned` when the framework should unload it on Route exit.

Do not put another framework bootstrap object in the additional scene.
Do not configure the additional scene as a separate boot route.

## QA Canvas setup

On `FrameworkQaCanvas`:

```text
Route Scene Composition Route = route with the Route Content Profile
Alternate Route = a different valid route used only to force a route transition when the composition route is already active
Expected Route Scene Loaded Count = 2
Expected Route Scene Owned Loaded Count = 2
```

For one Primary Scene plus one additional scene, both expected counts should be `2`.

## Run order

```text
1. Enter Play Mode.
2. Run Standard Smoke once to confirm baseline still passes.
3. Run Route Scene Composition Smoke.
```

## PASS evidence

Expected completion log:

```text
QA Route Scene Composition Smoke step completed. step='composition' route='<route>' routeSceneComposition='Succeeded' routeSceneEntries='2' routeSceneLoaded='2' routeSceneOwnedLoaded='2' routeSceneFailed='0' routeSceneIssues='0' routeSceneBlockingIssues='0' routeContentHandles='2' routeContentOwned='2' routeContentDiagnosticOnly='0'
```

F6 closure evidence used this shape with:

```text
routeSceneLoaded='2'
routeSceneOwnedLoaded='2'
routeSceneFailed='0'
routeSceneBlockingIssues='0'
routeContentHandles='2'
```

For more than one additional scene, increase the expected counts accordingly.

## Current boundary

This smoke validates additive loading and Route-owned handle registration. Use `ROUTE_RELEASE_SMOKE.md` to validate unload/release of owned additive scenes.
