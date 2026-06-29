# F9R-B — Unity ContentAnchor Physical Placement Adapter Proof

Status: Implemented

## Summary

F9R-B adds the first explicit Unity adapter proof for physical ContentAnchor placement.

The cut keeps logical ContentAnchor binding separate from Unity hierarchy placement:

```text
RuntimeContent materialization -> logical ContentAnchor binding -> explicit Unity placement adapter
```

## Implemented

- `UnityContentAnchorPlacementAdapter`
- `UnityContentAnchorPlacementResult`
- `UnityContentAnchorPlacementStatus`
- `Run Content Anchor Physical Placement Smoke`

The adapter requires:

- a successful logical `ContentAnchorBindingResult`;
- adapter-side `UnityRuntimeMaterializedObjectEvidence` from F8R-E;
- an explicit anchor `Transform`.

## Non-goals

- No prefab materialization ownership in ContentAnchor.
- No physical release ownership in ContentAnchor.
- No Addressables.
- No pooling.
- No actor spawn.
- No camera/audio/save/gameplay consumer.
- No identity from Unity object name or hierarchy path.

## Expected smoke

Run:

```text
Run Content Anchor Physical Placement Smoke
```

Expected result:

```text
QA Smoke completed. name='Content Anchor Physical Placement Smoke'.
```

The smoke validates missing anchor transform blocking, logical binding, physical parenting, already-placed behavior, logical unbind, physical release, logical release and guardrails.
