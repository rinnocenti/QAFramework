# F34-ADR-Architecture-Consolidation

Status: Accepted / Aggregate  
Last updated: 2026-07-01  
Supersedes: fragmented architecture ADR set previously kept in subfolders  
Superseded by: none

## Context

The architecture consolidation work had drifted into many small ADR files, plus older audits, plans and closeouts with placeholder phase names. That made active navigation harder than the architecture itself.

This ADR consolidates the active decisions for the Immersive Framework architecture consolidation track into one decision file. Historical source material remains in the legacy documentation folders only as reference.

## Decision

Use one active architecture ADR for the current consolidation program:

`Assets/_Documentation/Architecture/F34-ADR-Architecture-Consolidation.md`

Use one immutable plan and one mutable tracker next to it:

- `Assets/_Documentation/Architecture/F34-PLAN-Architecture-Consolidation.v1.md`
- `Assets/_Documentation/Architecture/F34-TRACK-Architecture-Consolidation.md`

The active architecture files use `F34` so the execution order is explicit.

## Accepted decisions

| Track | Decision | Status |
| --- | --- | --- |
| COMMON - Internal Mechanics | Keep COMMON small and internal. Accept only domain-neutral repeated mechanics such as enum/status validation, defensive collection copying and issue counting. | Closed |
| CONS - Participant Consolidation | Accept the participant consolidation as a bounded pilot for selected participant mechanics. Do not generalize to every participant-like flow automatically. | Closed pilot |
| LIFECYCLE - Route/Activity Operation Kernel | Keep lifecycle partial. Scope-tail mechanics are closed, but broader route/activity operation ownership remains pending. | Partial |
| MAT - RuntimeContent/ContentAnchor Materialization | Materialization orchestration belongs behind a reusable non-MonoBehaviour service; Unity bridges remain thin authoring wrappers. MAT core is closed after owner Unity validation; broad materialization consumers remain gated by adapter/surface readiness. | Closed / core validated |
| INPUT - Pause/InputMode Apply Boundary | Pause/InputMode apply semantics require a future explicit boundary. QA cleanup did not close this problem. | Pending |
| GAMEFLOW - Lifecycle Request Envelope | Keep the request envelope pending until lifecycle and input boundaries are clearer. Do not port old session shapes directly. | Pending |
| STATUS - Mapping Policy | Status mapping is domain-first. A future policy may normalize mapping mechanics, but must not create a universal status enum or hide original failures. | Pending |
| FLOWTRIGGER - Request/Trigger/State | Keep trigger helper extraction pending until lifecycle and GameFlow ownership is clearer. | Pending |
| PAUSEVIS - Consumer Readiness | Pause visual readiness is not approved for expansion. It needs a consumer readiness decision after input/apply and failure policy gates. | Pending |
| EXT - Adapter and Surface Readiness | The framework has adapter and surface candidates, but is not ready for broad adapter or Surface layer expansion. | Partial / readiness only |

## Accepted scope

- Compact active documentation with one ADR, one plan and one tracker.
- Architecture consolidation and adapter/surface readiness governance.
- Honest status tracking for closed, partial, blocked and pending work.
- Bounded internal mechanics only when ownership is clear.
- Future adapter/surface work only through finite gates.

## Rejected scope

- Multiple active ADR files for the same architecture program.
- Placeholder phase names in active architecture files.
- Public Surface or Adapter expansion before readiness gates close.
- Direct Base 2.0 runtime shape port.
- Package split without independent versioning and reuse need.
- Service locator, singleton shortcut, reflection routing or hidden compatibility rail.
- Universal status enum or generic result type that erases domain semantics.

## Consequences

The active architecture navigation is intentionally small. Detailed historical evidence remains available in legacy folders, but daily decision-making should start from this ADR, the F34 plan and the F34 tracker.

## Current implementation coverage

COMMON, CONS and MAT core are closed within their bounded scopes. LIFECYCLE remains partial because broader route/activity operation ownership is still open. INPUT, GAMEFLOW, STATUS, FLOWTRIGGER, PAUSEVIS and EXT readiness remain pending or blocked by explicit gates. Broad materialization consumers are not approved by MAT closure alone; they depend on adapter/surface readiness gates.

## Pending decisions

- Whether lifecycle operation kernel work should resume before or after the adapter/surface readiness branch.
- Whether EXT Surface Model deserves a separate ADR after `EXT-SURFACE-1`, or remains inside this aggregate ADR until a pilot is selected.
- Which single surface should be selected for `SURFACE-PILOT-1`.
