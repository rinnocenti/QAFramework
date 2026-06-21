# F3F1 — QA Panel Simplification

Status: APPLIED / PENDING COMPILE-SMOKE

## Context

After F3F, the QA panel exposed too many controls at the same level:

- core Route smoke;
- core Activity smoke;
- Clear Activity smoke;
- Route callback smoke;
- manual Route requests;
- manual Activity requests;
- no-activity/no-content/negative edge smokes;
- reset diagnostics.

This made the normal validation path harder to read and made the specialized Route Content callback smoke look like a regular baseline smoke.

## Decision

The QA panel now has a smaller default surface:

1. Runtime status.
2. Core QA scenario summary.
3. Core smokes.
4. Route Content callback smoke.
5. Collapsed advanced/manual controls.

The advanced section keeps manual and edge controls available, but they are no longer part of the default QA surface.

## Core controls

The default controls are:

- `Run Standard Smoke`;
- `Route`;
- `Activity`;
- `Clear Activity`;
- `Reset QA Scenario`;
- `Run Route Callback Smoke`.

`Run Standard Smoke` executes the canonical route/activity validation path in one button:

1. request alternate Route;
2. request canonical Route;
3. request secondary Activity;
4. request primary Activity;
5. clear Activity;
6. request primary Activity again.

## Route callback smoke

The Route callback smoke remains specialized. It still requires real scene setup:

```text
RouteContentBinding
└── RouteContentLifecycleSmokeProbe
```

Both QA primary scenes must contain a valid binding/probe pair:

- canonical Route scene;
- alternate Route scene.

The validation log now reports the callback route for Enter/Exit failures so a missing probe points to the correct scene.

## Non-goals

This cut does not add:

- runtime lifecycle;
- additive scene loading;
- Surface;
- RuntimeMaterialization;
- consumers;
- release policy;
- auto-created scene objects;
- editor scene mutation.

## Validation

Expected validation:

```text
Boot succeeded
QA Smoke completed. name='Standard Smoke'
QA Smoke completed. name='Route Callback Smoke'
```

For projects that still run separate buttons, these remain valid:

```text
QA Smoke completed. name='Route Smoke'
QA Smoke completed. name='Activity Smoke'
QA Smoke completed. name='Clear Activity Smoke'
```

For callback smoke, failures should identify the callback route:

```text
phase='Exit' callbackRoute='...'
phase='Enter' callbackRoute='...'
```
