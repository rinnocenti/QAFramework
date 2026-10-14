# Player Join provenance and certification

This record preserves observed Manager-Provisioned Join evidence without assigning retrospective execution metadata. Unity Test Runner, QA terminal and FirstGame results were supplied by the user; Codex did not execute Unity, NUnit, Play Mode or build. The original FirstGame log and Test Runner screenshot are now preserved in [PlayerStable-2026-10-12 evidence](Evidence/PlayerStable-2026-10-12/manifest.json). QA transcripts remain user-supplied terminal-line transcriptions, not full raw runner logs. Execution-time Framework/QA SHAs and exact run timestamps remain unknown, so provenance is **partial** and is accepted for the bounded Player Join decision only under IF-ADR-046 Amendment 003.

The I4 evidence is summarized in [IF-ADR-046 I4 — Scoped Join Binding](IF-ADR-046-I4-Scoped-Binding-Scenario.md). Keep the Play Mode Observable fixture separate from Framework package EditMode lease/identity coverage:

- **OBSERVABLE** covers public lifecycle, current Join/Leave, occurrence reentry identity and cleanup. The current fixture does not invoke retired Join triggers.
- The historical integrated **LEASE** probe is **BLOCKED 3/3** where a destroyed Unity component returns before lease comparison. This result remains historical and is not emitted by the current fixture. Framework package EditMode tests continue to cover lease capture and identity comparison; neither result should be substituted for the other.

I4 is **accepted by composite evidence with documented limitations**. P1 (the exact identity tuple captured by the real binder) and P2 (a real stale port's zero delegation to the Manager) are supported by static review, not demonstrated directly by test. The LEASE lane remains BLOCKED 3/3 as historical coverage; this is not converted to PASS and does not invalidate the proportional I4 acceptance. I5 migration and legacy Join entrypoint retirement have since been completed; neither action promotes an API to Stable. The final regression and consumer observations are recorded below with their provenance limits.

## Current observed evidence

| Run | User-reported result | Qualification |
|---|---|---|
| Framework EditMode, after legacy entrypoint removal | 292/292 PASS; PlayerParticipation 101/101; zero failures | Supplied Test Runner screenshot is archived in the Player Stable evidence set; no execution SHA, timestamp, raw XML/log or exact executed workspace bytes are associated. Provenance partial; risk explicitly accepted by IF-ADR-046 Amendment 003 for the bounded closure. |
| Slice 1 — Session / Join, after bridge correction | PASS 8/8; `BaselineRestored` | Later user-reported replay; exact terminal/log, timestamp and execution SHAs were not supplied or associated. The historical `Slice1-PlayMode/manifest.json` remains the earlier pre-correction run and is not rewritten. |
| I4 minimal | PASS 1/1; `BaselineRestored` | Partial provenance; exact terminal/log and execution metadata are not associated. |
| I4.2 OBSERVABLE | Revised fixture reported PASS 8/8; `BaselineRestored`; five Join/Leave outcomes `Joined/Admitted`; Activity and Route reentries observed | User supplied the terminal after probe removal. The eight cases now cover current Join/Leave, lifecycle and reentry assertions. Execution source identity and raw log remain unavailable. |
| I4.2 LEASE | Historical BLOCKED 3/3 | Destroyed-object guard prevented attributing rejection to internal lease comparison. Current occurrence fixture no longer emits this terminal; Framework EditMode lease/identity tests remain separate and unchanged. |
| Post-removal QA regression: IF-ADR-042/043/044 | 22/22 PASS: 042 7/7, 043 7/7, 044 8/8; all `BaselineRestored`, cleanup 3/3 | User-reported after legacy entrypoint removal, with partial provenance; no retrospective SHA or raw log association. |

The supplied [Player Stable evidence set](Evidence/PlayerStable-2026-10-12/manifest.json) includes the FirstGame raw log, EditMode screenshot, QA terminal transcript, supplied README and checksum list, plus the original ZIP. Their SHA-256 values were checked after preservation. The artifact hashes establish that these files were preserved unchanged; they do not establish the Framework/QA SHA or timestamp used during execution. The responsible authority accepted this residual provenance risk for this bounded maturity decision; no missing execution metadata is inferred.

The FirstGame Local Multiplayer flow was also reported functional after migration: keyboard/mouse and simulated gamepad Join, correct observed Slots/indices, Actor preparation, gameplay readiness, Leave and Rejoin. This consumer evidence is recorded by the Framework tracker; Character Selection was not independently validated. Neither consumer result supplies execution-byte provenance.

The existing EditMode record is [Evidence/NUnit-EditMode/manifest.json](Evidence/NUnit-EditMode/manifest.json); its 291/291 and 100/100 values are an earlier historical report and are not overwritten. The later 292/292 and 101/101 screenshot is preserved in the Player Stable evidence set, but it does not supply execution metadata or raw XML. The I4 provenance manifest is a **template**, not a record of these completed runs; do not fill it with guessed SHAs, timestamps, hashes, or synthetic terminal lines.

## Before a new run

1. Record the scenario/property, result, known Unity and resolved Framework source/version, cleanup/restoration, and available revision. Keep independently meaningful scenario lanes and Play Mode sessions identifiable.
2. Choose additional provenance for the gate and risk. Use the [provenance manifest template](provenance-manifest.template.json) when execution identity, exact candidate association, or a structured record is required or materially useful. Exact timestamps, full SHAs, dirty-state fingerprints, raw log preservation, and artifact hashes are not universal requirements; leave unknown fields unknown.
3. Preserve raw logs when an applicable gate requires them or when needed to diagnose a failure or ambiguity. When an artifact hash is collected, it establishes the archived bytes only and does not prove which source/package bytes ran.
4. Do not overwrite earlier failures or combine separate executions in a way that obscures their individual results.

## After a new run

1. Save exact terminal lines, first causes, execution/unwind diagnostics and cleanup results. Do not synthesize PASS from individual stages.
2. If the provenance template is used, fill it only with values observed for that execution. Verify recorded fingerprints or artifact hashes against the original bytes; do not fabricate missing metadata.
3. Keep the OBSERVABLE Play Mode result separate from Framework package EditMode lease/identity coverage. Retain the historical BLOCKED Play Mode LEASE result without attaching it to a new OBSERVABLE run.
4. Retain failed, blocked and superseded evidence; a later result does not rewrite the earlier run.

## Related evidence

- `Slice1-PlayMode/manifest.json` retains the earlier user-reported PASS 8/8 and `BaselineRestored`; that execution predates the callback-compensation correction. A later post-correction PASS 8/8 and `BaselineRestored` was reported, but its raw log, timestamp and execution SHAs were not supplied; the historical manifest is not rewritten and provenance remains partial.
- The rollback EditMode suite previously reported by the user used a fake backend that records compensation requests; it does not certify physical Unity object destruction.
- I4/I4.2 and post-removal regression observations here are not linked retrospectively to exact QAFramework or Framework bytes. The user reported the latter as post-removal candidate results, but no SHA/log/hash was captured; do not claim immutable same-candidate certification from checkout state alone.

## Manual validation

For a fresh run, use the Configure commands and lane-specific procedure in the [I4 scenario specification](IF-ADR-046-I4-Scoped-Binding-Scenario.md). Run minimal and occurrence fixtures in separate fresh Play Mode sessions; capture both I4.2 terminal lines and restore the previous GameApplication setting. Do not declare the internal lease lane passed based on a public `RejectedUnavailable` result.
