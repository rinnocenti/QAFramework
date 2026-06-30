# FXX-ADR-ARCH-0001 - Architecture Consolidation Governance

Status: Proposed / docs-only / governance
Type: Architecture Consolidation Track
Last updated: 2026-06-30

This ADR creates governance for the Architecture Consolidation Track. It does not authorize implementation by itself.

## 1. Context

The framework now has multiple evidence sources showing the same class of architectural pressure:

```text
FXX-AUDIT-General-Architecture-Pattern-Review
FXX-ADR-CONSOLIDATION-001 - Participant & Flow Pattern Consolidation
FXX-PLAN-Participant-And-Flow-Pattern-Consolidation
FXX-ADR-CONSOLIDATION-002 - RuntimeContent / ContentAnchor Materialization Orchestration
FXX-PLAN-RuntimeContent-ContentAnchor-Materialization-Orchestration
```

The issue is not lack of architecture. The framework has strong contracts, typed identities, result objects, diagnostics and explicit lifecycle boundaries. The issue is that several mechanics have been reimplemented manually in multiple modules or concentrated in bridge/coordinator classes.

## 2. Decision

Create an explicit Architecture Consolidation Track governed by these rules:

```text
1. Future implementation must be internal, additive and smoke-parity driven.
2. No public API changes are allowed without a dedicated ADR and migration plan.
3. No serialized field rename is allowed in this track.
4. No runtime behavior change is allowed unless it is an explicitly documented bug fix.
5. No implementation cut may select gameplay/F34, camera, audio, save, actor, pooling or adapter-module scope.
6. No service locator, singleton, reflection workaround or hidden compatibility rail may be introduced.
7. Common may host shared mechanics, but not domain semantics.
8. Bridge components must trend toward authoring/delegation, not orchestration.
9. Every track must name affected smokes and preserve their observable output unless a bug fix is explicitly approved.
```

## 3. Governance tracks

The Architecture Consolidation Track is organized into:

```text
1. Common internal mechanics
2. Participant consolidation
3. Route/Activity lifecycle operation kernel
4. RuntimeContent/ContentAnchor materialization service
5. Pause/InputMode apply boundary
6. GameFlow lifecycle request envelope
7. Status mapping policy
8. Flow trigger helper
9. Pause visual consumer readiness
```

The roadmap file owns ordering and per-track cut planning:

```text
Assets/_Documentation/Plans/FXX-PLAN-Architecture-Consolidation-Roadmap.md
```

## 4. Included scope

Allowed work in future phases:

```text
Internal Common helpers for validation/result mechanics.
Internal participant execution mechanics after explicit pilot approval.
Internal non-MonoBehaviour operation kernels.
Internal services that thin Unity authoring bridges.
Documentation, diagrams, mapping tables and closeout evidence.
Synthetic smokes and existing module smokes that prove parity.
```

## 5. Excluded scope

This governance ADR does not allow:

```text
Runtime implementation in this docs-only cut.
Asmdef changes.
Scene, prefab or asset edits beyond requested markdown documentation.
Public API replacement.
Serialized field rename.
Package split.
New technical package.
Gameplay/F34 selection.
Adapter-module expansion.
Service locator, singleton or reflection.
Generic abstraction without at least two concrete use cases.
```

## 6. Ordering policy

Default order:

```text
1. Audit or diagram the current operation.
2. Write or update ADR.
3. Write small implementation plan.
4. Implement one internal additive cut.
5. Compare smoke evidence.
6. Close out with explicit continue/stop decision.
```

No track may jump directly from audit to broad refactor.

## 7. Smoke parity policy

Every future implementation must define:

```text
Affected smokes.
Expected unchanged diagnostics.
Allowed explicit bug-fix differences, if any.
Manual validation checklist.
Rollback decision if diagnostics drift unexpectedly.
```

No implementation should alter smoke text to make a refactor pass. The refactor must preserve the existing observable behavior, or the difference must be documented as an accepted behavior change.

## 8. Common ownership policy

`Runtime/Common` may own internal mechanics such as:

```text
Enum/status validation helpers.
Text normalization already present.
Defensive copy helpers.
Issue counting helpers.
Participant execution mechanics after pilot approval.
Operation result containers if they do not decide domain semantics.
```

`Runtime/Common` must not own:

```text
Route semantics.
Activity semantics.
Pause semantics.
RuntimeContent semantics.
ContentAnchor semantics.
InputMode semantics.
Lifecycle policy.
Global state.
Service discovery.
Authoring UX.
```

## 9. Consequences

Positive:

```text
Consolidation work becomes ordered and reviewable.
Future refactors start from evidence instead of cleanup instinct.
Public APIs and Unity serialization are protected.
Common gets a bounded role.
```

Cost:

```text
More documentation before implementation.
Some duplication remains until a track is explicitly accepted.
Internal abstractions must be justified by concrete use cases.
```

## 10. Next step

Use the roadmap as the active backlog. The first implementable candidate remains the already-scoped Participant pilot only after explicit acceptance of its ADR and plan. All other tracks start with audit/diagram/planning cuts.

