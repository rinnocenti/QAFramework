# FXX-ADR-ARCH-0002 - Route/Activity Lifecycle Operation Kernel

Status: Proposed / docs-only / no implementation

## 1. Context

The Architecture Consolidation program has already closed the first two tracks:

- Track 1 - Common internal mechanics
- Track 2 - Participant consolidation

`LIFECYCLE-A` audited the internal sequence overlap between `RouteLifecycle` and `ActivityFlow` and found:

- both flows share the same general orchestration shape;
- `Route` and `Activity` do not share the same semantics;
- a broad kernel would be high risk;
- the first safe seam is the mechanical tail:
  - scope enter/exit
  - previous owner binding cleanup
  - merge of `RuntimeScopeLifecycleResult`

The current decision is therefore not to merge the public concepts, but to study a narrow internal kernel that can be mapped by domain delegates.

## 2. Decision

Authorize only the study of a narrow internal lifecycle kernel.

The kernel:

- must remain internal;
- must not merge `Route` and `Activity`;
- must not absorb scene composition, content dispatch, anchor discovery, progress budgeting or result shells in its first implementation;
- must accept domain semantics through mappers or delegates owned by `Route` or `Activity`.

The first future implementation candidate is limited to:

- scope enter/exit
- remove previous scope root
- cleanup of previous owner bindings
- merge of `RuntimeScopeLifecycleResult`

This ADR does not authorize a broad lifecycle abstraction.

## 3. Permitted scope

The future work allowed by this ADR is limited to:

- an internal, non-public operation model;
- non-`MonoBehaviour` code;
- no Unity serialization;
- no new public API;
- no change to public result/status shells;
- no change to public enums;
- domain semantics supplied by Route or Activity mappers/delegates.

The kernel may own only mechanical lifecycle sequencing.

## 4. Prohibited scope

The following are explicitly out of scope:

- broad lifecycle kernel creation;
- moving Route semantics to Common;
- moving Activity semantics to Common;
- changes to `GameFlowRuntime`;
- changes to `SceneLifecycleRuntime`;
- changes to `RouteSceneCompositionRuntime`;
- changes to `ActivitySceneCompositionRuntime`;
- changes to `Loading` or `Transition`;
- changes to `RuntimeContent` or `ContentAnchor`;
- serialized field changes;
- a base `MonoBehaviour` abstraction;
- service locator;
- singleton;
- reflection workaround;
- fallback rail.

## 5. Candidate seams

### Accepted for future planning

- scope enter/exit + remove previous scope root + merge
- previous owner binding cleanup guard

### Needs a later ADR

- progress budgeting helper

### Rejected from the first implementation

- scene composition
- content runtime dispatch/apply
- anchor discovery
- result/status shells

## 6. Diagnostics contract

Any future implementation that uses this ADR must preserve:

- `RuntimeScopeLifecycleResult` fields;
- diagnostic status text;
- root counts;
- cleanup counts;
- existing message shape where practical;
- existing smoke text.

Refactoring must not rewrite smoke output just to make a change appear equivalent.

## 7. Smoke parity contract

The following smokes are affected by any future implementation under this ADR:

- Standard Smoke
- Route Scene Composition Smoke
- Route Release Smoke
- Activity Baseline Smoke
- Activity Content Anchor Diagnostics Smoke
- Composite Lifecycle Release Smoke
- `LoadingProgressQaSmokeRunner`, if progress handling is touched later
- `TransitionQaSmokeRunner`, if transition diagnostics are touched later

The implementation must preserve observable output unless a separately approved bug fix is documented.

## 8. Future plan

Recommended follow-up cuts:

- `LIFECYCLE-C` - Internal Scope Tail Operation Model Shell
- `LIFECYCLE-D` - Route Scope Tail Pilot
- `LIFECYCLE-E` - Activity Scope Tail Pilot
- `LIFECYCLE-F` - Closeout / Decision

## 9. Consequences

### Positive

- reduces duplication without merging domains;
- creates a small and testable seam;
- preserves diagnostics and keeps ownership local.

### Cost

- adds another internal layer;
- may temporarily increase code volume;
- requires strict smoke parity discipline.

## 10. Rationale

`Route` and `Activity` are similar enough to justify a mechanical seam, but not similar enough to justify a shared domain kernel.

The correct architectural move is to isolate only the tail mechanics that already repeat on both sides, then re-evaluate after a narrow pilot.

## 11. Files changed

- `Assets/_Documentation/ADRs/FXX-ADR-ARCH-0002-Route-Activity-Lifecycle-Operation-Kernel.md`

