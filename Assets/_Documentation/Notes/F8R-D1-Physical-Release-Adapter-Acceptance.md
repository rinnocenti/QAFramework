# F8R-D1 — Physical Release Adapter Acceptance

Status: Accepted

Scope: docs-only / ADR acceptance closeout.

## Summary

F8R-D1 accepts `F8R-D — Physical Release Adapter` as the official boundary before any physical RuntimeContent cleanup implementation.

Accepted decision:

- RuntimeContent core keeps release as logical request/result/policy language.
- Physical release belongs to future adapters.
- `Object.Destroy`, pool return, Addressables release and scene unload stay outside RuntimeContent core.
- A future Unity release adapter may release only physical objects owned by an accepted materialization adapter.
- Future Pooling, Addressables and scene-owned cleanup require their own accepted adapter/boundary decisions.
- No consumer may depend on physical cleanup until this accepted adapter boundary is implemented in a later explicit cut.

This closeout accepts the ADR only. It does not create runtime implementation and does not select a new implementation phase.

## Non-goals

- No runtime changes.
- No scene or prefab changes.
- No asmdef changes.
- No code creation.
- No release adapter.
- No materializer.
- No `Instantiate`.
- No `Destroy`.
- No pooling.
- No Addressables.
- No scene unload.
- No ContentAnchor physical placement.
- No actor spawn.
- No F34.
- No gameplay, camera, audio, save/progression, pooling/runtime-spawned or actor materialization selection.

## Next Candidates

| Candidate | Status after F8R-D1 |
|---|---|
| F9R-A — ContentAnchor Runtime Binding Re-entry | Candidate only; not selected automatically. |
| Future materializer/release adapter implementation | Candidate only after explicit acceptance; not selected automatically. |

None of these candidates is an implementation selected by F8R-D1. A later user decision is required before any of them opens.

## References

- `Assets/_Documentation/Audits/F8R-A-RuntimeContent-ContentAnchor-Materialization-Audit.md`
- `Assets/_Documentation/Plans/F8R-B-PLAN-Runtime-Root-Handle-Release-Policy.md`
- `Assets/_Documentation/Notes/F8R-B1-Runtime-Root-Handle-Release-Policy-Acceptance.md`
- `Packages/com.immersive.framework/Documentation~/ADRs/F8R-B-ADR-Runtime-Root-Handle-Release-Policy.md`
- `Assets/_Documentation/Plans/F8R-C-PLAN-Runtime-Materialization-Adapter-Boundary.md`
- `Assets/_Documentation/Notes/F8R-C1-Runtime-Materialization-Adapter-Boundary-Acceptance.md`
- `Packages/com.immersive.framework/Documentation~/ADRs/F8R-C-ADR-Runtime-Materialization-Adapter-Boundary.md`
- `Assets/_Documentation/Plans/F8R-D-PLAN-Physical-Release-Adapter.md`
- `Packages/com.immersive.framework/Documentation~/ADRs/F8R-D-ADR-Physical-Release-Adapter.md`
- `Assets/_Documentation/Plans/POST-F33-PLAN-Matrix-Reconciliation.md`
