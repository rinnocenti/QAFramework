# F3 — Route baseline technical closure

Status: CLOSED / PASS

## Purpose

F3 closes the Route baseline and `RouteContentRuntime` decision without advancing into scene composition, runtime materialization, Surface, release policy or consumers.

## Closed cuts

```text
F3A — CLOSED / ADRS ACCEPTED
F3B — CLOSED / COMPILE-SMOKE PASS
F3C — CLOSED / COMPILE-SMOKE PASS
F3D — CLOSED / COMPILE-SMOKE PASS
F3E — CLOSED / COMPILE-SMOKE PASS
F3F — CLOSED / CALLBACK-SMOKE PASS
F3F1 — CLOSED / COMPILE-SMOKE PASS
F3G — CLOSED / COMPILE-SMOKE PASS
F3G1 — CLOSED / COMPILE-SMOKE PASS
F3  — CLOSED / PASS
```

## Decisions accepted in F3A

| ADR | Outcome |
|---|---|
| `ADR-ROUTE-001 — RouteRuntimeState and RouteContentRuntime Status` | Route owns explicit typed runtime state. `RouteContentRuntime` is active in F3 only for local callbacks in the loaded Primary Scene. |
| `ADR-ROUTE-002 — RouteContentSet Semantics` | `RouteContentSet` is an immutable snapshot of known Route content. Ownership is explicit. Release remains deferred to F6. |

## Technical baseline delivered

| Roadmap item | Status | Result |
|---|---|---|
| `IF-FW-ROAD-3A — RouteRuntimeState tipado` | Closed in F3B | Active Route is represented by `RouteRuntimeState` with typed Route identity. |
| `IF-FW-ROAD-3B — RouteExitResult mínimo` | Closed in F3C | Route switches produce explicit `RouteExitResult`. |
| `IF-FW-ROAD-3C — RouteContentRuntime execution decision` | Closed in F3D | Route content exits before `Single` scene load; next Route content enters after load and before Startup Activity. |
| `IF-FW-ROAD-3D — RouteContentSet semantics` | Closed in F3E | `RouteContentSet` has explicit `RouteContentEntry` and `RouteContentOwnership`; Primary Scene baseline is required/owned. |
| `IF-FW-ROAD-3E — Route local callback smoke` | Closed in F3F | QA callback smoke validates real `IRouteContentLifecycleReceiver` dispatch. |
| `IF-FW-ROAD-3F — Route validator expansion` | Closed in F3G/F3G1 | QA validates loaded `RouteContentBinding` authoring with low-noise UI. |

## Final smoke evidence

F3 was closed after the revised F3G smoke produced:

```text
Boot succeeded
QA Smoke completed. name='Standard Smoke'
QA Smoke completed. name='Route Callback Smoke'
QA Route Callback Smoke step completed. step='alternate'
QA Route Callback Smoke step completed. step='canonical'
QA Authoring Validation completed. scope='Loaded Route Content' bindings='1' issues='0'
```

The callback smoke confirmed both authored Route Content probes:

```text
Canonical Route Probe -> QA Canonical Route -> StartupScene
Alternate Route Probe -> QA Alternate Route -> SecoundScene
```

## Explicit non-goals preserved

F3 did not implement:

```text
additive scene loading
RouteContentProfile execution
Scene composition plan/result
release policy
Surface
RuntimeMaterialization
Camera
Audio
Input
Save
Actor
Pooling
Projectile
consumer lifecycle
```

## Final Route baseline shape

```text
RouteAsset
  -> RouteRuntimeState
  -> RouteExitResult
  -> SceneLifecycleRuntime loads Primary Scene as Single
  -> RouteContentSet registers known Route content
  -> RouteContentRuntime dispatches local callbacks
  -> ActivityFlow starts or switches the startup Activity
```

## Next phase

F4 is the next authorized phase. It starts Activity baseline work and must not pull Activity profile loading, actors, input, camera, reset, snapshot, release, Surface or RuntimeMaterialization into the core early.

Next planned cut:

```text
F4A — IF-FW-ROAD-4A — ActivityRuntimeState refinado
```
