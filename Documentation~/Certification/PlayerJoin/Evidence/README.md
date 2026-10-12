# Player Join evidence storage

Each `manifest.json` below records results as reported by the user. The original Unity Test Runner capture and Slice 1 Console/Editor log were not found in the QAFramework workspace during this review. Do not recreate those originals from the summaries in these manifests.

Copy the original files into the corresponding `originals/` directory and update the matching manifest with exact relative paths and SHA-256 checksums. Keep the original bytes unchanged. Only then may `provenanceCompleteness` be reconsidered. Do not fill in a prior execution's commit SHA, dirty state, timestamp or execution ID from today's checkout.

Historical checkout snapshots recorded below describe when earlier evidence documents were prepared; they are not execution provenance. Current checkout state must never be substituted for an earlier Unity run's state.

- `NUnit-EditMode/manifest.json`: latest user-reported 291/291 PASS, PlayerParticipation 100/100, zero failures. No current suite-level breakdown, execution metadata, original XML/log, capture file or hash is associated; provenance remains partial.
- Later user-reported post-removal EditMode regression: 292/292 PASS, PlayerParticipation 101/101, zero failures. The execution artifacts and metadata were not supplied, so this result is summarized in the certification README but not inserted into the older manifest.
- Later user-reported post-removal QA regression: IF-ADR-042 7/7, IF-ADR-043 7/7, IF-ADR-044 8/8; all `BaselineRestored`, cleanup 3/3. No execution SHA or raw log was supplied; see the certification README for scope and limits.
- `../IF-ADR-046-I4-Scoped-Binding-Scenario.md`: user-reported I4 minimal PASS 1/1 and I4.2 OBSERVABLE PASS 8/8, both with `BaselineRestored`; I4.2 LEASE remains BLOCKED 3/3. P1/P2 and provenance limits are recorded separately.
- `Slice1-PlayMode/manifest.json`: prior user-reported Slice 1 PASS 8/8 and cleanup evidence. That run predates the bridge correction. A later post-correction PASS 8/8 / `BaselineRestored` was reported, but no original log, timestamp or execution SHA was supplied; preserve the manifest as the historical run and do not claim candidate-linked provenance.
- `../I4-provenance-manifest.template.json`: template only, not an execution record. Populate execution-specific fields only from a future run's captured evidence.
