# Player Join provenance and certification

This procedure records observed Manager-Provisioned Join executions without assigning retrospective execution metadata. Unity Test Runner and Play Mode results below were reported by the user; Codex did not execute Unity, NUnit, Play Mode or build. The original run bytes are not currently linked to these summaries, so provenance remains **partial**.

The current I4 evidence is summarized in [IF-ADR-046 I4 — Scoped Join Binding](IF-ADR-046-I4-Scoped-Binding-Scenario.md). Keep its two lanes separate:

- **OBSERVABLE** covers public lifecycle, current Join/Leave, stale public rejection, no unintended Session revision change and cleanup.
- **LEASE** concerns internal validation of a stale lease. It remains **BLOCKED** where a destroyed Unity component returns before the lease comparison; this is not an OBSERVABLE failure and must not be reported as PASS.

I4 is **accepted by composite evidence with documented limitations**. P1 (the exact identity tuple captured by the real binder) and P2 (a real stale port's zero delegation to the Manager) are supported by static review, not demonstrated directly by test. The LEASE lane remains BLOCKED 3/3 as historical coverage; this is not converted to PASS and does not invalidate the proportional I4 acceptance. I4 acceptance opens I5 consumer migration under approved plan v3; it does not promote an API to Stable or authorize legacy entrypoint retirement.

## Current observed evidence

| Run | User-reported result | Qualification |
|---|---|---|
| Framework EditMode | 291/291 PASS; PlayerParticipation 100/100; zero failures | Partial provenance; no execution SHA, timestamp, raw XML/log, artifact hash or exact executed workspace bytes associated. |
| I4 minimal | PASS 1/1; `BaselineRestored` | Partial provenance; exact terminal/log and execution metadata are not associated. |
| I4.2 OBSERVABLE | PASS 8/8; `BaselineRestored`; five Join/Leave outcomes `Joined/Admitted`; Activity and Route A→B→A reentries observed; three stale calls `RejectedUnavailable` without Session revision change | Public behavior as reported. Stale-call rejection is not evidence that internal lease validation ran. |
| I4.2 LEASE | BLOCKED 3/3 | Destroyed-object guard prevents attributing the rejection to the internal lease comparison. |

The existing EditMode record is [Evidence/NUnit-EditMode/manifest.json](Evidence/NUnit-EditMode/manifest.json). It records only reported values and leaves execution-specific provenance unknown. The I4 provenance manifest is a **template**, not a record of these completed runs; do not fill it with guessed SHAs, timestamps, hashes, or synthetic terminal lines.

## Before a new run

1. Create a unique execution ID and record scenario, contract, start/end times, branches, full SHAs, dirty state, Unity version and resolved Framework source/version at execution time.
2. For a dirty checkout, record fingerprints of relevant changed/untracked files. A commit SHA alone does not identify dirty bytes.
3. Capture the complete raw Unity log before scenario start. Preserve it unchanged and record its path and SHA-256.
4. Keep each execution in its own evidence directory. Do not overwrite earlier failures or combine separate Play Mode sessions into one execution record.

## After a new run

1. Save exact terminal lines, first causes, execution/unwind diagnostics and cleanup results. Do not synthesize PASS from individual stages.
2. Complete a copy of the provenance template only with values observed for that execution. Verify file fingerprints and log hash against original bytes.
3. Keep OBSERVABLE and LEASE results separate, including when one passes and the other is blocked.
4. Retain failed, blocked and superseded evidence; a later result does not rewrite the earlier run.

## Related evidence

- Slice 1 has an earlier user-reported PASS 8/8 and `BaselineRestored`; it predates the callback-compensation correction and was not replayed by this task.
- The rollback EditMode suite previously reported by the user used a fake backend that records compensation requests; it does not certify physical Unity object destruction.
- I4/I4.2 observations here are not linked retrospectively to the current QAFramework or Framework HEAD. Do not claim same-candidate certification from checkout state alone.

## Manual validation

For a fresh run, use the Configure commands and lane-specific procedure in the [I4 scenario specification](IF-ADR-046-I4-Scoped-Binding-Scenario.md). Run minimal and occurrence fixtures in separate fresh Play Mode sessions; capture both I4.2 terminal lines and restore the previous GameApplication setting. Do not declare the internal lease lane passed based on a public `RejectedUnavailable` result.
