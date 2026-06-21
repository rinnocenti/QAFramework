# F3G1 — QA authoring validation hygiene

Status: CLOSED / COMPILE-SMOKE PASS

## Purpose

Reduce authoring UI noise introduced during F3G before closing the validator cut.

## Changes

```text
- RouteContentBinding Inspector returns to a minimal Route field with tooltip.
- Verbose RouteContentBinding validation is removed from the Inspector surface.
- Project Settings no longer exposes Validate Authoring as the primary validation button.
- Framework QA Canvas adds Validate Loaded Route Content.
```

## QA validation button

The QA button validates currently loaded `RouteContentBinding` components for:

```text
- missing Route;
- Route whose Primary Scene does not match the loaded scene;
- missing IRouteContentLifecycleReceiver under the binding.
```

The button does not open scenes and does not mutate scenes.

## Non-goals

```text
Surface
RuntimeMaterialization
additive scene loading
release policy
consumers
auto-fix
scene mutation
```

## Smoke criteria

```text
QA Smoke completed. name='Standard Smoke'
QA Smoke completed. name='Route Callback Smoke'
QA Authoring Validation completed. scope='Loaded Route Content'
```
