# F1 Closure — API status, Identity and Diagnostics

Status: CLOSED / PASS  
Date: 2026-06-21  
Phase: F1 — API status, Identity and Diagnostics

## Result

F1 is closed.

F1 established the minimum governance layer needed before opening F2:

```text
API status vocabulary and source markers.
Structured diagnostics fact model.
ValidationMode minimum semantics.
Typed identity primitives.
Content identity applied to FrameworkContentHandle.
ADR naming aligned to roadmap order.
```

F1 does not claim that the whole framework is fully typed or production-stable. It closes only the F1 baseline needed to stop creating new ambiguous public/runtime surfaces.

## Cut checklist

| Cut | Status | Result |
|---|---|---|
| F1A | CLOSED / ACCEPTED | ADRs for identity, diagnostics and content identity accepted. |
| F1B | CLOSED / COMPILE-SMOKE PASS | API status convention and minimal markers applied. |
| F1C | CLOSED / COMPILE-SMOKE PASS | `FrameworkFact` minimal model created. |
| F1D | CLOSED / COMPILE-SMOKE PASS | `ValidationMode` minimum semantics defined and wired into authoring validation. |
| F1E | CLOSED / COMPILE-SMOKE PASS | Typed identity primitives created. |
| F1E1 | CLOSED / DOCUMENTATION ONLY | ADR file naming aligned to roadmap/cut order. |
| F1F | CLOSED / COMPILE-SMOKE PASS | Content identity and `FrameworkContentHandle` reviewed. |

## Accepted F1 ADRs

| Ordem no Plano | ADR | Status | Implementation coverage |
|---|---|---|---|
| F1A-01 | ADR-ID-001 — Typed Identity Policy | Accepted | F1E created the minimum primitives. |
| F1A-02 | ADR-DIAG-001 — FrameworkFact vs Human Log | Accepted | F1C created the minimum fact model. |
| F1A-03 | ADR-CONTENT-001 — Content Identity Domain | Accepted | F1F applied composed content identity to `FrameworkContentHandle`. |

## Smoke evidence

The F1 closing smoke validated:

```text
Boot succeeded: 1
Route Smoke completed: 1
Activity Smoke completed: 1
Clear Activity Smoke completed: 1
Exception: 0
FATAL: 0
error CS: 0
failed / Failed: 0
```

It also exercised the composed content identity diagnostics for route-scene handles:

```text
identity='Route:Scene:Route:Assets/Scenes/StartupScene.unity:primary-scene:StartupScene'
identity='Route:Scene:Route:Assets/Scenes/SecoundScene.unity:primary-scene:SecoundScene'
```

## What F1 allows next

F1 allows the roadmap to open F2.

The next phase must begin with ADR/review and stay within F2 scope:

```text
F2 — Session scope
```

The next likely cut is:

```text
F2A — Session scope ADR review and acceptance
```

## What F1 does not authorize

F1 does not authorize jumping directly to advanced consumers or late roadmap features.

Do not start yet:

```text
Route additive composition outside F3/F6.
Activity readiness expansion outside F4.
Surface declaration outside F7.
Runtime materialization outside F8/F9.
Input, Pause, Save, Actor, Audio, Camera, Pooling or Projectile consumers outside F10/F11.
```

## Remaining constraints

F2+ cuts must keep the F1 rules active:

```text
No silent fallback for required functional identity.
No path/name-only identity when owner/scope/kind/content id are required.
No public/semi-public surface without API status.
No log-text parsing as functional validation.
No consumer package dictating lifecycle core shape.
```
