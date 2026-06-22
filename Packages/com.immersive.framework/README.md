# Immersive Framework

Development package for game lifecycle architecture in Unity 6.5.

Package name:

```text
com.immersive.framework
```

## Current status

Use `Documentation~/COMPLETENESS_TRACKER.md` as the authoritative status file.

Current consolidated position:

```text
F0 — CLOSED / PASS
F1 — CLOSED / PASS
F2 — CLOSED / PASS
F3 — CLOSED / PASS
F4 — CLOSED / ACTIVITY BASELINE PASS
F5 — CLOSED / LOCAL CONTRIBUTION FOUNDATION PASS
F6 — OPEN / ADR ACCEPTED
```

F6 has completed the ADR/audit gate only. Runtime implementation has not started.

Next authorized implementation step:

```text
F6B — RouteSceneCompositionPlan
```

F6B must be inert planning data. It must not execute additive scene loading, release scenes, create Surface, create RuntimeRootRegistry, create prefab materialization, or touch Actor/Input/Camera/Reset/Save/Pooling.

## Documentation entry points

```text
Documentation~/COMPLETENESS_TRACKER.md
Documentation~/Planning/Immersive-Framework-Roadmap-Revisado.md
Documentation~/Planning/Capability-Traceability-Matrix.md
Documentation~/Planning/F6-Route-Scene-Composition-Audit.md
Documentation~/ADRs/
```

## Accepted F6 ADRs

| ADR | Decision |
|---|---|
| `F6-01 — ADR-RELEASE-001` | Release is planned/executed through `ContentReleasePlan`/`ContentReleaseResult`, guided by explicit ownership. |
| `F6-02 — ADR-SCENE-001` | Route scene composition is planned/executed through `RouteSceneCompositionPlan`/`RouteSceneCompositionResult`; additive loading comes only after plan/result. |

## Current hard boundary

The framework currently has lifecycle/content/contribution foundations. It is not yet a Surface, RuntimeSpawned, Actor, Camera, Input, Save, Reset or Pooling framework.

Do not skip from F6 ADRs directly to F7/F8/F9/F10+ consumers.
