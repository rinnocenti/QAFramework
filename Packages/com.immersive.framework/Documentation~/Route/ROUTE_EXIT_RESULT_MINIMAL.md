# F3C — RouteExitResult minimal

Status: APPLIED / PENDING COMPILE-SMOKE

## Roadmap item

```text
IF-FW-ROAD-3B — RouteExitResult mínimo
```

## Purpose

F3C makes Route exit explicit as a result object during Route switching.

Before this cut, a route switch only exposed the previous Route as a field in `RouteLifecycleStartResult`. After this cut, the previous Route exit is represented by `RouteExitResult`.

## Runtime files

```text
Runtime/RouteLifecycle/RouteExitResult.cs
Runtime/RouteLifecycle/RouteLifecycleRuntime.cs
Runtime/RouteLifecycle/RouteLifecycleStartResult.cs
```

## Semantics

`RouteExitResult` records:

```text
- whether a previous Route was exited;
- the exited Route;
- the previous RouteRuntimeState;
- the next Route;
- source;
- reason;
- diagnostic message.
```

The result is intentionally minimal. It is diagnostics/state data, not release execution.

## What it does not do

F3C does not implement:

```text
RouteContentRuntime active callbacks
Route content release
RouteContentOwnership
additive scene loading
Surface
RuntimeMaterialization
consumers
release policy
```

## Expected smoke signal

The standard smoke must still pass:

```text
Boot succeeded
QA Smoke completed. name='Route Smoke'
QA Smoke completed. name='Activity Smoke'
QA Smoke completed. name='Clear Activity Smoke'
```

The F3C-specific diagnostic should appear during Route switches:

```text
routeExit='Exited'
```

Startup does not have a previous Route, so it does not need to emit an exited Route diagnostic.

## Next roadmap item

```text
F3D — IF-FW-ROAD-3C — RouteContentRuntime execution decision
```
