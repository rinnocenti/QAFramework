# ADR Naming Convention

Status: Active  
Cut: F1E1  
Scope: Documentation navigation

---

## Purpose

ADR files must be navigable in the same order as the roadmap. The project uses plan cuts such as `F0A`, `F1A`, `F1B` and implementation cuts such as `F1E`. ADR identifiers such as `ADR-ID-001` remain stable architectural identifiers, but the file names now start with the plan order.

## File naming rule

```text
<plan-order>-<adr-id>-<slug>.md
```

Examples:

```text
F0A-01-ADR-BL-001-baseline-reconciliation.md
F1A-01-ADR-ID-001-typed-identity-policy.md
F1A-02-ADR-DIAG-001-frameworkfact-vs-human-log.md
F1A-03-ADR-CONTENT-001-content-identity-domain.md
```

## Reading rule

- Use the `F*` prefix to follow the roadmap order.
- Use the `ADR-*` id to cite a stable architectural decision.
- ADR status remains `Accepted`, `Draft / Deferred`, etc.
- Cut/phase status remains `Closed`, `Applied`, or `Pending Smoke` in the closure documents.

## Renamed files

| Old path | New path |
|---|---|
| `ADRs/F0A-baseline-adrs/01-ADR-BL-001-baseline-reconciliation.md` | `ADRs/F0A-baseline-adrs/F0A-01-ADR-BL-001-baseline-reconciliation.md` |
| `ADRs/F0A-baseline-adrs/02-ADR-BL-002-core-vs-consumers.md` | `ADRs/F0A-baseline-adrs/F0A-02-ADR-BL-002-core-vs-consumers.md` |
| `ADRs/F0A-baseline-adrs/03-ADR-BL-003-public-api-status-policy.md` | `ADRs/F0A-baseline-adrs/F0A-03-ADR-BL-003-public-api-status-policy.md` |
| `ADRs/F0A-baseline-adrs/04-ADR-BL-004-qa-and-diagnostics-boundary.md` | `ADRs/F0A-baseline-adrs/F0A-04-ADR-BL-004-qa-and-diagnostics-boundary.md` |
| `ADRs/F0A-baseline-adrs/05-ADR-BL-005-dependency-policy.md` | `ADRs/F0A-baseline-adrs/F0A-05-ADR-BL-005-dependency-policy.md` |
| â€” | `ADRs/F0A-baseline-adrs/F0A-06-ADR-BL-006-minimal-bootstrap-and-incremental-construction.md` |
| `ADRs/F1-api-status-identity-and-diagnostics/03-ADR-ID-001-typed-identity-policy.md` | `ADRs/F1-api-status-identity-and-diagnostics/F1A-01-ADR-ID-001-typed-identity-policy.md` |
| `ADRs/F1-api-status-identity-and-diagnostics/02-ADR-DIAG-001-frameworkfact-vs-human-log.md` | `ADRs/F1-api-status-identity-and-diagnostics/F1A-02-ADR-DIAG-001-frameworkfact-vs-human-log.md` |
| `ADRs/F1-api-status-identity-and-diagnostics/01-ADR-CONTENT-001-content-identity-domain.md` | `ADRs/F1-api-status-identity-and-diagnostics/F1A-03-ADR-CONTENT-001-content-identity-domain.md` |
| `ADRs/F2-session-scope/01-ADR-SESSION-001-session-scope-and-owner.md` | `ADRs/F2-session-scope/F2-01-ADR-SESSION-001-session-scope-and-owner.md` |
| `ADRs/F2-session-scope/02-ADR-SESSION-002-sessioncontent-ownership-semantics.md` | `ADRs/F2-session-scope/F2-02-ADR-SESSION-002-sessioncontent-ownership-semantics.md` |
| — | `ADRs/F2-session-scope/F2-03-ADR-SETTINGS-001-settings-source-policy.md` |
| `ADRs/F3-route-baseline/01-ADR-ROUTE-001-routeruntimestate-and-routecontentruntime-status.md` | `ADRs/F3-route-baseline/F3-01-ADR-ROUTE-001-routeruntimestate-and-routecontentruntime-status.md` |
| `ADRs/F3-route-baseline/02-ADR-ROUTE-002-routecontentset-semantics.md` | `ADRs/F3-route-baseline/F3-02-ADR-ROUTE-002-routecontentset-semantics.md` |
| `ADRs/F4-activity-content-and-readiness/01-ADR-ACTIVITY-001-activitycontentset-and-readiness-baseline.md` | `ADRs/F4-activity-content-and-readiness/F4-01-ADR-ACTIVITY-001-activitycontentset-and-readiness-baseline.md` |
| â€” | `ADRs/F4-activity-content-and-readiness/F4-02-ADR-ACTIVITY-002-activity-content-binding-minimal-observable.md` |
| `ADRs/F5-local-contribution/01-ADR-LOCAL-001-local-identity.md` | `ADRs/F5-local-contribution/F5-01-ADR-LOCAL-001-local-identity.md` |
| `ADRs/F5-local-contribution/02-ADR-LOCAL-002-local-contribution-discovery-and-requiredness.md` | `ADRs/F5-local-contribution/F5-02-ADR-LOCAL-002-local-contribution-discovery-and-requiredness.md` |
| `ADRs/F6-route-scene-composition-and-release/01-ADR-RELEASE-001-content-release-plan-by-scope.md` | `ADRs/F6-route-scene-composition-and-release/F6-01-ADR-RELEASE-001-content-release-plan-by-scope.md` |
| `ADRs/F6-route-scene-composition-and-release/02-ADR-SCENE-001-route-scene-composition-plan-and-result.md` | `ADRs/F6-route-scene-composition-and-release/F6-02-ADR-SCENE-001-route-scene-composition-plan-and-result.md` |
| `ADRs/F7-content-anchor-declaration/01-ADR-ANCHOR-001-content-anchor-as-placement-contract.md` | `ADRs/F7-content-anchor-declaration/F7-01-ADR-ANCHOR-001-content-anchor-as-placement-contract.md` |
| `ADRs/F8-runtime-roots-and-materialization/01-ADR-RUNTIME-001-runtime-ownership-and-roots.md` | `ADRs/F8-runtime-roots-and-materialization/F8-01-ADR-RUNTIME-001-runtime-ownership-and-roots.md` |
| `ADRs/F8-runtime-roots-and-materialization/02-ADR-RUNTIME-002-materialization-request-result-handle.md` | `ADRs/F8-runtime-roots-and-materialization/F8-02-ADR-RUNTIME-002-materialization-request-result-handle.md` |
| `ADRs/F9-content-anchor-binding-and-runtime-placement/01-ADR-ANCHOR-002-content-anchor-binding-and-runtime-placement.md` | `ADRs/F9-content-anchor-binding-and-runtime-placement/F9-01-ADR-ANCHOR-002-content-anchor-binding-and-runtime-placement.md` |

| — | `ADRs/F10-activity-content-execution-core/F10-01-ADR-ACTIVITY-003-activity-entry-exit-content-execution-core.md` |
| — | `ADRs/F10-activity-content-execution-core/F10-02-ADR-ACTIVITY-004-activity-content-execution-ordering-and-lifecycle.md` |
| — | `ADRs/F10-activity-content-execution-core/F10-03-ADR-ACTIVITY-005-activity-content-execution-readiness-failure-diagnostics.md` |
