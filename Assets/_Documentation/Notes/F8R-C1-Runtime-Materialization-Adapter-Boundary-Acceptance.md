# F8R-C1 — Runtime Materialization Adapter Boundary Acceptance

Status: Accepted

Scope: docs-only / ADR acceptance closeout.

## Summary

F8R-C1 accepts `F8R-C — Runtime Materialization Adapter Boundary` as the official boundary before any physical RuntimeContent materialization implementation.

Accepted decision:

- Pure RuntimeContent core keeps request, result, identity, owner, scope, handle state, guard state and diagnostics.
- A future Unity materialization adapter is the first authorized boundary for physical materialization.
- RuntimeContent core does not reference `UnityEngine.GameObject`, `UnityEngine.Transform`, `Object.Instantiate`, `Object.Destroy`, Addressables handles or Pooling-owned instance references.
- `RuntimeContentHandle` remains logical and cannot be treated as a `GameObject`, `Transform`, prefab instance, pooled instance, Addressables handle or placement token.
- Physical object evidence may exist only inside the future adapter boundary and must not leak into pure core contracts.
- No consumer may depend on physical materialization until the accepted adapter boundary is implemented in a later explicit cut.

This closeout accepts the ADR only. It does not create runtime implementation and does not select a new implementation phase.

## Non-goals

- No runtime changes.
- No scene or prefab changes.
- No asmdef changes.
- No code creation.
- No materializer.
- No `Instantiate`.
- No `Destroy`.
- No pooling.
- No Addressables.
- No ContentAnchor physical placement.
- No actor spawn.
- No F34.
- No gameplay, camera, audio, save/progression, pooling/runtime-spawned or actor materialization selection.

## Next Candidates

| Candidate | Status after F8R-C1 |
|---|---|
| F8R-D — Physical Release Adapter Plan | Candidate only; not selected automatically. |
| F9R-A — ContentAnchor Runtime Binding Re-entry | Candidate only; not selected automatically. |
| Future materializer implementation | Candidate only after explicit acceptance; not selected automatically. |

None of these candidates is an implementation selected by F8R-C1. A later user decision is required before any of them opens.

## References

- `Assets/_Documentation/Audits/F8R-A-RuntimeContent-ContentAnchor-Materialization-Audit.md`
- `Assets/_Documentation/Plans/F8R-B-PLAN-Runtime-Root-Handle-Release-Policy.md`
- `Assets/_Documentation/Notes/F8R-B1-Runtime-Root-Handle-Release-Policy-Acceptance.md`
- `Assets/_Documentation/Plans/F8R-C-PLAN-Runtime-Materialization-Adapter-Boundary.md`
- `Packages/com.immersive.framework/Documentation~/ADRs/F8R-C-ADR-Runtime-Materialization-Adapter-Boundary.md`
- `Assets/_Documentation/Plans/POST-F33-PLAN-Matrix-Reconciliation.md`
