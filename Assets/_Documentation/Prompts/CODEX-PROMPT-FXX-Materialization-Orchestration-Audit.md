# CODEX PROMPT — FXX Materialization Orchestration Audit

You are working in a Unity 6.5 project with `com.immersive.framework` under `Packages/com.immersive.framework`.

Task type: audit-only. Do not modify code, assets, asmdefs, scenes, prefabs or package metadata.

## Goal

Audit the RuntimeContent / ContentAnchor prefab materialization path and produce a concise implementation plan for making the path reusable without changing observable behavior.

## Files/areas to inspect first

```text
Packages/com.immersive.framework/Runtime/ContentAnchor/UnityContentAnchorMaterializationBridge.cs
Packages/com.immersive.framework/Runtime/ContentAnchor/UnityContentAnchorMaterializationPipeline.cs
Packages/com.immersive.framework/Runtime/ContentAnchor/UnityContentAnchorPlacementAdapter.cs
Packages/com.immersive.framework/Runtime/ContentAnchor/*Materialization*Result*.cs
Packages/com.immersive.framework/Runtime/ContentAnchor/*Materialization*Status*.cs
Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeContentRuntime.cs
Packages/com.immersive.framework/Runtime/RuntimeContent/UnityPrefabRuntimeMaterializationAdapter.cs
Packages/com.immersive.framework/Runtime/RuntimeContent/UnityObjectRuntimeReleaseAdapter.cs
Packages/com.immersive.framework/Runtime/RuntimeContent/UnityRuntimeMaterializedObjectRegistry.cs
Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeMaterialization*.cs
Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeRelease*.cs
```

## Questions to answer

1. What is the exact sequence from bridge call to physical placement?
2. Which classes own runtime state, physical Unity side effects, logical binding, placement and rollback?
3. Which result/status enums are mapped into wrapper statuses?
4. Where does rollback happen, and which failure points trigger it?
5. Which part should become a reusable non-MonoBehaviour service?
6. What is the smallest safe first implementation cut?
7. What existing smoke/QA output must remain unchanged?

## Expected output

Create or update only documentation under `Assets/_Documentation` if asked by the user. For this audit pass, output markdown text only:

```text
FXX-AUDIT-RuntimeContent-ContentAnchor-Materialization-Orchestration.md
FXX-ADR-CONSOLIDATION-002-RuntimeContent-ContentAnchor-Materialization-Orchestration.md
FXX-PLAN-RuntimeContent-ContentAnchor-Materialization-Orchestration.md
```

## Guardrails

```text
No implementation.
No new runtime lifecycle.
No fallback behavior.
No reflection.
No service locator.
No pooling/actor/pause/loading integration.
No public Inspector field changes.
No serialized field rename.
No asmdef change.
No scene or prefab edit.
```

## Preferred direction to evaluate

Prefer a conservative service extraction:

```text
ContentAnchorMaterializationService.MaterializeBindPlace(...)
```

The bridge should become a thin wrapper that reads Inspector fields and delegates to the service. The existing pipeline can remain internally during migration if that preserves behavior.

Do not split `RuntimeContentRuntime` first. Plan that as a later decision after the service boundary is proven.
