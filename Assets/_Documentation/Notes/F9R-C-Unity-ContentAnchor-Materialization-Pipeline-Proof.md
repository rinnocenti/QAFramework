# F9R-C — Unity ContentAnchor Materialization Pipeline Proof

Status: Implemented

## Summary

F9R-C adds the first explicit Unity composition proof that combines existing runtime pieces into one authored flow:

```text
RuntimeContent materialization request
  -> Unity prefab materialization adapter
  -> logical RuntimeContent handle registration
  -> logical ContentAnchor binding
  -> Unity ContentAnchor physical placement adapter
```

This cut is runtime implementation plus QA smoke. It is not a docs-only acceptance step.

## Runtime files

```text
Packages/com.immersive.framework/Runtime/ContentAnchor/UnityContentAnchorMaterializationPipeline.cs
Packages/com.immersive.framework/Runtime/ContentAnchor/UnityContentAnchorMaterializationPipelineResult.cs
Packages/com.immersive.framework/Runtime/ContentAnchor/UnityContentAnchorMaterializationPipelineStatus.cs
```

## QA

```text
FrameworkQaCanvas -> Run Content Anchor Materialization Pipeline Smoke
```

The smoke validates:

- missing anchor Transform blocks before materialization side effects;
- valid request materializes a prefab through the Unity materialization adapter;
- logical RuntimeContent handle registration succeeds;
- logical ContentAnchor binding succeeds;
- physical placement parents the instance under the explicit anchor Transform;
- duplicate materialization for the same runtime identity is blocked;
- logical unbind, physical release and logical release complete;
- another materialization after release remains blocked by the adapter-side registry.

## Non-goals

- No automatic lifecycle wiring.
- No Route/Activity auto materialization.
- No Addressables.
- No pooling.
- No actor spawn.
- No `PlayerInputManager.JoinPlayer`.
- No camera/audio/save/gameplay consumer.
- No identity from Unity names or paths.

## Accepted boundary

The pipeline may orchestrate explicit adapters, but ownership remains split:

- RuntimeContent core owns logical identity/request/handle/release language.
- Unity materialization adapter owns prefab instantiation proof.
- ContentAnchor binding remains logical.
- Unity placement adapter owns `Transform.SetParent` proof.
- Unity release adapter owns physical `Object.Destroy` request proof.
