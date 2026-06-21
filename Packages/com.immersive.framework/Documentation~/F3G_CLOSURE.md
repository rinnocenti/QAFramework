# F3G — Route validator expansion closure

Status: CLOSED / COMPILE-SMOKE PASS

## Closed cuts

```text
F3G — CLOSED / COMPILE-SMOKE PASS
F3G1 — CLOSED / COMPILE-SMOKE PASS
```

## Evidence

F3G/F3G1 closed after the revised QA/authoring validation flow passed with the F3 Route baseline scenes configured correctly.

Required smoke signals:

```text
Boot succeeded
QA Smoke completed. name='Standard Smoke'
QA Smoke completed. name='Route Callback Smoke'
QA Route Callback Smoke step completed. step='alternate'
QA Route Callback Smoke step completed. step='canonical'
QA Authoring Validation completed. scope='Loaded Route Content' bindings='1' issues='0'
```

## Result

The active Route validator surface is the Framework QA Canvas button:

```text
Validate Loaded Route Content
```

The validator checks loaded `RouteContentBinding` roots for:

```text
- missing Route reference;
- Route whose Primary Scene does not match the loaded scene;
- missing IRouteContentLifecycleReceiver under the binding root.
```

The `RouteContentBinding` Inspector remains intentionally small. The Route field tooltip explains the authoring rule:

```text
Route asset that owns this scene. Use the Route whose Primary Scene is this scene.
```

## Non-goals preserved

F3G did not implement:

```text
Surface
RuntimeMaterialization
additive scene loading
RouteContentProfile execution
release policy
consumers
scene mutation
auto-fix
```

## Follow-up

F3G closes the last technical item in F3. The next package step is the F3 technical closure.
