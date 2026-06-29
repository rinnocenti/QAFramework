# F9R-D — Unity ContentAnchor Materialization Scope Release Proof

Status: Implemented

## Summary

F9R-D adds the first explicit scope-release proof for Unity objects created through the RuntimeContent / ContentAnchor materialization pipeline.

The cut composes existing accepted boundaries:

```text
Unity ContentAnchor Materialization Pipeline
  -> adapter-created physical GameObject evidence
  -> logical RuntimeContent handle
  -> logical ContentAnchor binding
  -> explicit scope release pipeline
  -> Unity physical release adapter
  -> logical RuntimeContent release
  -> logical ContentAnchor binding cleanup
```

## Runtime files

```text
Packages/com.immersive.framework/Runtime/ContentAnchor/UnityContentAnchorMaterializationScopeReleasePipeline.cs
Packages/com.immersive.framework/Runtime/ContentAnchor/UnityContentAnchorMaterializationScopeReleasePipelineResult.cs
Packages/com.immersive.framework/Runtime/ContentAnchor/UnityContentAnchorMaterializationScopeReleasePipelineStatus.cs
```

## QA

```text
FrameworkQaCanvas -> Run Content Anchor Materialization Scope Release Smoke
```

The smoke validates:

- missing runtime host blocks before cleanup side effects;
- two ContentAnchor materialization pipeline entries can be released by one runtime owner context;
- physical release is requested for each adapter-created object;
- logical RuntimeContent release unregisters handles;
- logical ContentAnchor bindings are cleaned up by runtime owner;
- second scope release is idempotent and reports no remaining content.

## Non-goals

- No automatic Route/Activity lifecycle wiring.
- No automatic materialization from Route/Activity definitions.
- No Addressables.
- No pooling.
- No actor spawn.
- No `PlayerInputManager.JoinPlayer`.
- No camera, audio, save or gameplay consumer.
- No Unity object names or paths as functional identity.
