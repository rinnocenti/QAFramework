# IF-ADR-046 I4 — Scoped Join Binding QA scenario

Status: **Accepted by composite evidence with documented limitations; P1/P2 remain static-review residuals**
Owner: **QAFramework**
Framework candidate reference: `325b73af06fd3fc7d273b6f7b5729dc92f7b9103` (not associated retrospectively with the execution bytes)

This document records user-reported Unity results. Codex did not execute Unity, NUnit, Play Mode or build. No run is assigned a retrospective SHA, timestamp, manifest, raw log or hash. The evidence has **partial provenance**. This status is consistent with IF-ADR-046, accepted Amendment 001, approved plan v3 and IF-GOV-001: evidence does not itself promote maturity or retire legacy entrypoints.

## Fixture lanes

The independent minimal lane is `Assets/QA-IF-ADR-046/`, configured by **Immersive Framework > QA > Player > IF-ADR-046 I4 > Configure Minimal Join Fixture**. The occurrence lane is generated under `Assets/QA-IF-ADR-046/OccurrenceBinding/` by **Configure Occurrence Binding Fixture**. It has its own GameApplication, Route A/B, Activity A/B and Persistent Content scene. Each setup preserves unrelated Build Settings entries and validates its own scene and asset references. Slice 1 remains independent and outside these setup commands.

Run minimal and occurrence lanes in separate fresh Play Mode sessions and select the matching GameApplication in `ImmersiveFrameworkSettings`. Do not combine their results into one execution record.

## Latest user-reported Unity observations

| Lane / evidence | Observed result | Scope and limit |
|---|---|---|
| I4 minimal | **PASS 1/1**, `BaselineRestored` | Activity-scoped public Join/Leave path. Does not alone certify I2/I3 or the occurrence cases. |
| I4.2 OBSERVABLE | **PASS 8/8**, `BaselineRestored` | Five current-occurrence Join/Leave outcomes were `Joined/Admitted`; Activity A→B→A and Route A→B→A reentries were observed; three stale public calls returned `RejectedUnavailable` without changing Session revision. |
| I4.2 LEASE | **BLOCKED 3/3** | The destroyed Unity-object guard can reject before internal lease comparison. These results are not evidence that `PlayerSessionJoinBindingLease.ValidateCurrent` ran. |
| Framework EditMode | **291/291 PASS**, PlayerParticipation **100/100**, zero failures | User-reported aggregate. See the separate EditMode manifest; it does not establish the exact I4 execution bytes or directly prove P1/P2. |

The public observations establish the reported transitions and outcomes. `RejectedUnavailable` plus unchanged Session revision proves the observed public no-effect result; it does not establish a zero-call assertion on `manager.TryJoin` for the real stale port.

### Earlier I4.2 failure retained as historical evidence

Before the latest reported run, an I4.2 execution reached **4/8 OBSERVABLE**, then failed its Route A→B→A compound check; both Route requests had returned `Succeeded`, Activity A was republished at Activity occurrence 7, and cleanup was `BaselineRestored`. The earlier terminal did not emit each predicate value, so the exact failed runtime subcondition cannot be reconstructed. Static review of the old oracle found an inverted trigger-identity expectation: it failed when the reloaded Route A correctly supplied a new scene-owned trigger. The latest 8/8 result is a later user-reported execution; it does not erase that earlier failure or retroactively attach it to a SHA/artifact.

The current Route re-entry oracle treats separate facts separately: public Route lifecycle `Entered` context for the exact Route/contribution, Route A asset identity, scene unload/reload observations and fresh scene/component identities. Activity occurrence numbers remain Activity-only evidence; the public Route lifecycle does not expose a Route occurrence ordinal, so none is fabricated. The I4.2 scenario uses public `RouteRequestTrigger`, `ActivityRequestTrigger`, `PlayerSessionJoinTrigger`, Session observation and Unity scene/device APIs; it does not access lease internals or add another Join authority.

## Internal properties still pending (P1/P2)

| Property | What current evidence supports | What is not directly demonstrated |
|---|---|---|
| **P1 — real binder capture**: exact owner, Manager, Route occurrence and Activity occurrence correspond to the active Runtime occurrence | Static tracing of `RefreshBindings`/`TryCapture` and the reported current-occurrence public Join behavior; EditMode identity-comparison coverage is a separate component-level proof. | A test observing the identity tuple actually captured by the real binder and matching it to the active lifecycle occurrence. The public I4 result does not expose that tuple. |
| **P2 — no delegation by stale real port**: rejected/disposed binding does not reach canonical Manager Join | Static call-order review: the real port checks disposal and lease validity before `manager.TryJoin`; binder disposal releases the old binding. Public stale-call rejection/no Session revision change is complementary behavior evidence. | An executed assertion counting canonical Manager calls for an invalidated/disposed real port. The destroyed-trigger guard can return before the port/lease path. |

These are **not demonstrated directly by test** and remain separate from the public OBSERVABLE result. The three BLOCKED lease checks stay BLOCKED; they are retained as coverage history and do not invalidate the proportional I4 acceptance. P1/P2 are accepted as known residual risks under this gate, not relabeled PASS by static reasoning. Revisit only on a production defect, relevant lifecycle/lease/port change, or a new production requirement for direct observability. Do not add host/lifecycle fixtures or permanent seams solely to observe these internals.

This composite acceptance closes the I4 integration gate for beginning I5 consumer migration. It is not Stable approval, a statement that every internal predicate was directly tested, or authorization to remove legacy APIs. Apply plan v3 for FirstGame/QAFramework migration and later retirement gates.

## Provenance

The current report contains user-supplied aggregate outcomes only. It does not provide execution timestamps/IDs, exact terminal text, raw Unity logs or hashes, original capture files, Unity run-time checkout fingerprints, or verified association between the evidence and the current Framework/QAFramework bytes. Keep `provenanceCompleteness` as `partial`; do not infer values from today's Git state.

`I4-provenance-manifest.template.json` is a template for **future** executions, not the record for these observations. Copy it into a unique execution directory for a new run and populate only values captured during that run. Keep minimal and occurrence lane executions separately identifiable. Do not backfill this template with the candidate reference SHA or current checkout metadata.

## Manual procedure for a new execution

1. Open QAFramework in Unity `6000.5.0f1` and run **Configure Minimal Join Fixture** twice. Confirm `scenes='3/3-registered'` without duplicate entries. Run it in its own fresh Play Mode session and save the exact terminal/log.
2. Run **Configure Occurrence Binding Fixture** twice. Confirm the five `I4_Binding_*` scenes are enabled exactly once and unrelated entries remain. Select `Assets/QA-IF-ADR-046/OccurrenceBinding/Settings/GameApplication_I4_ScopedBinding.asset` and run a separate fresh Play Mode session.
3. Capture the complete Unity log and exact terminals `[QA-IF-ADR-046-I4.2-OBSERVABLE]` and `[QA-IF-ADR-046-I4.2-LEASE]`. Preserve each lane's result independently; only the observed OBSERVABLE lane can report its public case status, while lease stays BLOCKED unless internal validation is legitimately observed.
4. Restore the prior `activeGameApplication`. Save a new manifest with actual execution-time SHAs, dirty fingerprints, Unity/package resolution, timestamps/ID, raw log hash, exact terminal lines and cleanup. Do not associate these values with earlier runs.

No new Unity validation is requested or claimed by this documentation update.
