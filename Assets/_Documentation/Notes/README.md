# Immersive Framework Notes Archive

This folder now consolidates the historical tracking notes for the framework documentation stream.
The individual note files were collapsed into this archive so the folder stays readable and the record remains searchable by phase.

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


## F8R / F9R RuntimeContent + ContentAnchor Materialization Track

| Note | Status | Summary |
|---|---|---|
| F8R-E | Implemented / QA smoke | Unity prefab RuntimeContent materialization adapter proof. |
| F9R-B | Implemented / QA smoke | Unity ContentAnchor physical placement adapter proof. |
| F9R-C | Implemented / QA smoke | Materialization + logical binding + physical placement pipeline proof. |
| F9R-D | Implemented / QA smoke | Explicit scope release proof for materialized ContentAnchor content. |
| F9R-E | Implemented / QA smoke | Authored opt-in materialization bridge proof. |
| F9R-F | Implemented / QA smoke | Authored bridge set proof for explicit batch submit/release. |
| F9R-G | Implemented / QA smoke | Bridge set preflight before batch side effects. |
| F9R-H | Implemented / QA smoke | Authoring validation proof for bridge/bridge set configuration. |
| F9R-I | Implemented / QA smoke | Runtime authoring gate proof before materialization. |
| F9R-J | Closed / PASS | Query-only diagnostics snapshot proof. |
| F9R-K | Accepted / docs-only | F9R closeout / documentation sync. |
| F9R-L | Closed / PASS | Partial bridge set materialization rollback proof validated by QA smoke. |
| F9R-M | Accepted / Plan / docs-only | Lifecycle-owned materialization registry planning baseline; no implementation or lifecycle auto-materialization selected. |
| F9R-N | Ready for smoke | Minimal lifecycle-owned materialization registry contract proof; registry evidence only, no Route/Activity auto-wiring. |

## Consolidation Notes

- All standalone notes previously stored in this folder were consolidated here.
- Temporary delete manifests were folded into the archive history:
  - `F24B1-DELETE-MANIFEST.txt`
  - `F26F-DELETE-MANIFEST.txt`
- Some higher-level package documentation still references the old note names, but the canonical file in this folder is now `Notes/README.md`.
