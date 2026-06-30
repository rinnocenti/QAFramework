# Immersive Framework Notes Archive

This folder now consolidates the historical tracking notes for the framework documentation stream.
The individual note files were collapsed into this archive so the folder stays readable and the record remains searchable by phase.


## F8R / F9R RuntimeContent and ContentAnchor Re-entry History

| Note | Status | Summary |
|---|---|---|
| F8R-B1 | Accepted / docs-only | Runtime Root / Handle / Release Policy ownership baseline accepted. |
| F8R-C1 | Accepted / docs-only | Runtime materialization adapter boundary accepted. |
| F8R-D1 | Accepted / docs-only | Physical release adapter boundary accepted. |
| F8R-E | Implemented / QA proof | Unity prefab RuntimeContent materialization adapter proof. |
| F9R-B | Implemented / QA proof | Unity ContentAnchor physical placement adapter proof. |
| F9R-C | Implemented / QA proof | Unity ContentAnchor materialization pipeline proof. |
| F9R-D | Implemented / QA proof | Explicit scope release proof for materialized ContentAnchor content. |
| F9R-E | Implemented / QA proof | Authored opt-in ContentAnchor materialization bridge proof. |
| F9R-F | Implemented / QA proof | Authored opt-in bridge set proof. |
| F9R-G | Implemented / QA proof | Bridge set preflight-before-side-effects proof. |
| F9R-H | Implemented / QA proof | Authoring validation for bridge and bridge set surfaces. |
| F9R-I | Implemented / QA proof | Runtime authoring gate before bridge set materialization. |
| F9R-J | Closed / PASS | Query-only diagnostics snapshot proof for bridge set state. |
| F9R-K | Accepted / docs-only | Documentation sync and closeout; selects no new implementation axis. |

## F24 Transition / Loading / Surface History

| Note | Status | Summary |
|---|---|---|
| F24A6-F24A9 | Historical QA tracking | Transition QA routes/scenes, game application, route switch panels and activity switch panels. |
| F24B | Historical contract note | Transition contract wiring between Transition and GameFlow. |
| F24B1 | Ready to apply / cleanup | Temporary QA tooling cleanup and delete-manifest handling. |
| F24C | Historical wiring | Transition Unity surface wiring. |
| F24D | Historical loading surface | Loading Unity surface wiring. |
| F24D1-F24D5 | Historical loading fixes | Loading hold, warm visible state, ordered route sequence, cascade sequence, hide boundary, visibility and warnings. |
| F24E | Historical baseline | Canonical UIGlobal scene and Unity-facing surface baseline. |
| F24E1-F24E3 | Historical cleanup | Legacy loading cleanup, route/activity visual operation policy, inspector cleanup. |
| F24F / F24F1 | Policy history | Activity transition policy and the historical loading-reserved finding. |

## F25 Activity Scene / Ledger History

| Note | Status | Summary |
|---|---|---|
| F25H2 | Implemented / route-scope hardening | Activity scene ledger route-scoped queries and route-instance ownership hardening. |
| F25R | Accepted / documentation reset | Activity scene operation architecture reset and visual-policy clarification. |

## F26 Loading Progress History

| Note | Status | Summary |
|---|---|---|
| F26A1 | Accepted / diagnostics + documentation | Activity content execution diagnostics clarification. |
| F26B | Accepted / diagnostics + documentation | Internal loading progress contract and diagnostic fields. |
| F26C | Accepted / Unity surface receiver | Loading surface progress bar receiver. |
| F26D | Accepted / runtime progress bridge | Determinate loading progress source from real scene operations. |
| F26E | Closed / PASS | Aggregated loading progress across Route and Activity operations. |
| F26F | Closed / documentation and QA polish | Loading progress closeout, typo fix and delete-manifest cleanup. |

## F27 Pause / Gate History

| Note | Status | Summary |
|---|---|---|
| F27A | Ready for smoke | Pause UIGlobal surface baseline. |
| F27B | Closed / pending smoke | Narrow PauseToggle input binding. |
| F27C | Closed / audit PASS | Gate / input capability audit and boundary correction. |
| F27D | Ready for smoke | Pause capability gate reframe and diagnostic vocabulary correction. |
| F27E | Cancelled / do not apply | Input consumers gate replan rejected; do not reintroduce that path. |

## F28 Baseline / Dependency History

| Note | Status | Summary |
|---|---|---|
| F28A | Closed / documentation-only / no runtime changes | Frozen baseline reconciliation. |
| F28B | Closed / docs-only | Completion dependency map for the remaining implementation tracks. |
| F28C | Closed / docs-only | Adapter module taxonomy and ownership split. |
| F28D | Closed / docs-only | Player / Actor / Unity Input ownership plan. |

## Consolidation Notes

- All standalone notes previously stored in this folder were consolidated here.
- Temporary delete manifests were folded into the archive history:
  - `F24B1-DELETE-MANIFEST.txt`
  - `F26F-DELETE-MANIFEST.txt`
- Some higher-level package documentation still references the old note names, but the canonical file in this folder is now `Notes/README.md`.
