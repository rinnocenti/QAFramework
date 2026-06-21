# F1F Closure — Content identity / FrameworkContentHandle review

Status: CLOSED / COMPILE-SMOKE PASS  
Date: 2026-06-21  
Phase: F1 — API status, Identity and Diagnostics  
Cut: F1F

## Result

F1F is closed.

The compile-smoke after F1F validated the baseline path with the new content identity shape active in `FrameworkContentHandle` diagnostics.

## Smoke evidence

Observed in the submitted smoke log:

```text
Boot succeeded: 1
Route Smoke completed: 1
Activity Smoke completed: 1
Clear Activity Smoke completed: 1
Exception: 0
FATAL: 0
error CS: 0
failed / Failed: 0
Composed content identity entries: 3
```

The route content diagnostics now emit composed content identities such as:

```text
identity='Route:Scene:Route:Assets/Scenes/StartupScene.unity:primary-scene:StartupScene'
identity='Route:Scene:Route:Assets/Scenes/SecoundScene.unity:primary-scene:SecoundScene'
```

This confirms that the F1F `FrameworkContentIdentity` path is exercised by boot and route smoke.

## Closed scope

F1F closed the following items:

```text
Runtime/ContentFlow/FrameworkContentId.cs
Runtime/ContentFlow/FrameworkContentIdentity.cs
Runtime/ContentFlow/FrameworkContentHandle.cs
Documentation~/CONTENT_IDENTITY_AND_HANDLE_REVIEW.md
```

The old silent fallback using `Guid.NewGuid()` for route primary scene identity was removed. Required route-scene content identity is now deterministic or fails visibly.

## Explicit non-scope

F1F did not create or advance:

```text
SessionContentSet
ActivityContentSet
RuntimeContentHandle
SurfaceContentHandle
LocalContentIdentity
RuntimeContentIdentity
SurfaceIdentity
RuntimeMaterialization
Surface
Additive scene execution
Release policy
Consumer integration
```

These remain later roadmap work.
