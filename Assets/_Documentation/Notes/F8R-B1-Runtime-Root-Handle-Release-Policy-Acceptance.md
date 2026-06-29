# F8R-B1 — Runtime Root / Handle / Release Policy Acceptance

Status: Accepted

Scope: docs-only / ADR acceptance closeout.

## Summary

F8R-B1 accepts `F8R-B — Runtime Root / Handle / Release Policy` as the official logical ownership baseline before any physical materialization work.

Accepted decision:

- RuntimeContent core keeps root, handle and release as logical framework language.
- `RuntimeScopeRoot` does not become a `GameObject` or `Transform`.
- `RuntimeContentHandle` does not represent a Unity object, pooled instance, Addressables handle or Content Anchor placement.
- `RuntimeReleasePolicy` remains logical.
- Physical root creation, placement, `Instantiate`, `Destroy`, pool return, scene unload and Addressables release remain outside RuntimeContent core.
- Consumers cannot assume a physical object from `RuntimeContentHandle`.

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
- No `PlayerInputManager.JoinPlayer`.
- No F34.
- No gameplay, camera, audio, save/progression, pooling/runtime-spawned or actor materialization selection.

## Next Candidates

| Candidate | Status after F8R-B1 |
|---|---|
| F8R-C — Runtime Materialization Adapter Boundary Plan | Candidate only; not selected automatically. |
| F8R-D — Physical Release Adapter Plan | Candidate only; not selected automatically. |
| F9R-A — ContentAnchor Runtime Binding Re-entry | Candidate only; not selected automatically. |

None of these candidates is an implementation selected by F8R-B1. A later user decision is required before any of them opens.

## References

- `Assets/_Documentation/Audits/F8R-A-RuntimeContent-ContentAnchor-Materialization-Audit.md`
- `Assets/_Documentation/Plans/F8R-B-PLAN-Runtime-Root-Handle-Release-Policy.md`
- `Packages/com.immersive.framework/Documentation~/ADRs/F8R-B-ADR-Runtime-Root-Handle-Release-Policy.md`
- `Assets/_Documentation/Plans/POST-F33-PLAN-Matrix-Reconciliation.md`
