# IF-FW-F25H2 — Activity Scene Ledger Route-Scoped Queries

## Status
Implemented / route-scope hardening

## Context

`F25H` introduced `ActivitySceneLedger` and `F25H1` made the effective lookup key include:

```text
RouteInstanceId + Activity + ContentIdentity
```

After that fix, the ledger still exposed two internal route-less collection methods:

```text
CollectLoadedForActivity(activity)
CollectLoaded()
```

They were not used by the current runtime, but they made future regressions easy: a caller could accidentally query Activity-owned content across route instances and recreate stale `AlreadyLoaded` or release planning behavior.

## Decision

Remove route-less loaded-entry query methods from `ActivitySceneLedger`.

The only supported loaded-entry queries are route-scoped:

```text
CollectLoadedForActivityRouteInstance(activity, routeInstanceId)
CollectLoadedForRouteInstance(routeInstanceId)
```

## Runtime behavior

No runtime behavior changes are intended.

This cut only removes unused internal API surface that contradicted the route-instance ownership rule.

## Rule

Activity-owned scene ledger reads must always include `RouteInstanceId` unless the operation is intentionally reading a route-scoped collection for the current route instance.

Route-less Activity scene ledger reads are invalid for canonical runtime flow.
