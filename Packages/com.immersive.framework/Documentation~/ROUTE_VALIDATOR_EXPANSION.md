# F3G — Route validator expansion

Status: APPLIED / PENDING COMPILE-SMOKE
Hygiene: F3G1 applied before smoke to reduce UI noise.

## Roadmap item

```text
IF-FW-ROAD-3F — Route validator expansion
```

## Purpose

F3G expands validation for the active F3 Route baseline. F3G1 moves the validation entry point to the QA panel and keeps the Inspector minimal.

## Added validation

`FrameworkAuthoringValidator` now checks loaded scene `RouteContentBinding` components for:

```text
- missing Route reference;
- binding authored in a scene that does not match the assigned Route Primary Scene;
- nested RouteContentBinding roots;
- child RouteContentBinding roots;
- missing IRouteContentLifecycleReceiver under the binding root.
```

## Binding authoring feedback

`RouteContentBinding` keeps only a minimal Route field tooltip:

```text
Route asset that owns this scene. Use the Route whose Primary Scene is this scene.
```

The Inspector no longer shows verbose validation details by default.

## Validation entry point

The active QA entry point is now:

```text
Framework QA Canvas > Route Content Callback Smoke > Validate Loaded Route Content
```

It checks loaded scenes only. It does not open or mutate scenes, does not create GameObjects, and does not auto-fix references.

The editor validator remains available internally for editor tooling, but Project Settings is not the main authoring-validation surface for this cut.

## Non-goals

F3G does not implement:

```text
Surface
RuntimeMaterialization
additive scene loading
RouteContentProfile execution
release policy
consumers
runtime lifecycle changes
scene mutation
```

## Smoke criteria

Run:

```text
Run Standard Smoke
Run Route Callback Smoke
Framework QA Canvas > Validate Loaded Route Content
```

Expected:

```text
QA Smoke completed. name='Standard Smoke'
QA Smoke completed. name='Route Callback Smoke'
QA Authoring Validation completed. scope='Loaded Route Content'
```

For correctly configured QA scenes, validation must not report:

```text
has no Route assigned
points to Route ... but it is authored in scene
has no IRouteContentLifecycleReceiver
```
