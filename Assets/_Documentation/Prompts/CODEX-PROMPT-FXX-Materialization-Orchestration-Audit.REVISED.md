# CODEX PROMPT — FXX Materialization Orchestration Audit — Revised Scope

Use this only if the user asks for a focused audit of `RuntimeContent` / `ContentAnchor` materialization.

For the broader architecture review, use:

```text
CODEX-PROMPT-FXX-General-Architecture-Pattern-Audit.md
```

## Scope correction

This prompt is **not** the general review. It focuses only on the materialization path and should not be used to answer whether other framework phases have similar problems.

## Task type

Audit-only. Do not modify code, assets, asmdefs, scenes, prefabs or package metadata.

## Goal

Audit the RuntimeContent / ContentAnchor prefab materialization path and produce a concise implementation plan for making the path reusable without changing observable behavior.

## Files/areas to inspect first

```text
Packages/com.immersive.framework/Runtime/ContentAnchor/UnityContentAnchorMaterializationBridge.cs
Packages/com.immersive.framework/Runtime/ContentAnchor/UnityContentAnchorMaterializationPipeline.cs
Packages/com.immersive.framework/Runtime/ContentAnchor/UnityContentAnchorPlacementAdapter.cs
Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeContentRuntime.cs
Packages/com.immersive.framework/Runtime/RuntimeContent/UnityPrefabRuntimeMaterializationAdapter.cs
Packages/com.immersive.framework/Runtime/RuntimeContent/UnityObjectRuntimeReleaseAdapter.cs
Packages/com.immersive.framework/Runtime/RuntimeContent/UnityRuntimeMaterializedObjectRegistry.cs
Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeMaterialization*.cs
Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeRelease*.cs
```

## Questions to answer

```text
1. What is the exact sequence from bridge call to physical placement?
2. Which classes own runtime state, physical Unity side effects, logical binding, placement and rollback?
3. Which result/status enums are mapped into wrapper statuses?
4. Where does rollback happen, and which failure points trigger it?
5. Which part should become a reusable non-MonoBehaviour service?
6. What is the smallest safe first implementation cut?
7. What existing smoke/QA output must remain unchanged?
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
