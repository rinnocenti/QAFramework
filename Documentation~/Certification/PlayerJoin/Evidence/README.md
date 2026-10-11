# Player Join evidence storage

Each `manifest.json` below records results as reported by the user. The original Unity Test Runner capture and Slice 1 Console/Editor log were not found in the QAFramework workspace during this review. Do not recreate those originals from the summaries in these manifests.

Copy the original files into the corresponding `originals/` directory and update the matching manifest with exact relative paths and SHA-256 checksums. Keep the original bytes unchanged. Only then may `provenanceCompleteness` be reconsidered. Do not fill in a prior execution's commit SHA, dirty state, timestamp or execution ID from today's checkout.

Current checkout snapshot when these evidence records were prepared (not attributed to either execution): Framework `master...origin/master`, commit `b25b74824d60054cf6d95f70083226ea3dad47bb`, with this task's matrix edit, test asmdef edit and untracked bridge test/source meta; QAFramework `main...origin/main`, commit `f00676563b5adfdc4c66323234c60a0a733c8217`, with QA scene/configuration changes from the two Configure runs, existing certification README edit and evidence-document changes. QA currently resolves Framework from the local path at version `1.1.0-preview.7`; ProjectVersion declares Unity `6000.5.0f1`. These current values are context only; they do not identify the bytes used by the NUnit or Play Mode runs.

- `NUnit-EditMode/manifest.json`: user-reported 229/229 PASS, PlayerParticipation 38/38, Join contract 26/26 and bridge rollback 9/9 after the callback-compensation correction. Capture is reported as supplied in the conversation; original execution metadata, local artifact bytes and SHA are not associated, so provenance remains partial.
- `Slice1-PlayMode/manifest.json`: prior user-reported Slice 1 PASS 8/8 and cleanup evidence. This run predates the bridge correction and has not been repeated afterward.
- `../provenance-manifest.template.json`: template for future executions; these records remain partial-provenance observations.
