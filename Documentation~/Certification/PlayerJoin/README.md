# Player Join provenance and certification

This procedure applies to QAFramework Manager-Provisioned Join runs. It records new executions only. User-reported NUnit EditMode and Slice 1 results are now recorded under `Evidence/` as observed results with partial provenance; Slice 2–3 remain their earlier reported runs. No run is retroactively assigned a SHA or exact timestamp.

## Before each run

1. Create a unique execution ID (UTC timestamp plus a random UUID) and record the scenario/slice and covered contract.
2. Record the full Framework and QAFramework commit SHAs, branch names, dirty state, and whether relevant uncommitted changes exist. Do not label a dirty checkout by commit SHA alone.
3. Record the Framework `package.json` version, QAFramework Unity Editor version, manifest/lock source, and effective resolved Framework path. For this project the manifest resolves Framework through the local `file:C:/Projetos/ImmersivePackages/com.immersive.framework` path, so record both the commit and the dirty state/content fingerprint.
4. For a dirty Framework checkout, record SHA-256 for each changed/untracked file under the relevant Runtime, Editor, and package test paths, plus a deterministic aggregate of sorted relative paths and file hashes. If that cannot be captured, set `provenanceCompleteness` to `partial`.
5. Start raw log capture before opening the tested Play Mode scenario. Preserve the complete Unity Editor log or captured raw stream unchanged; record its relative path and SHA-256 after execution.

## After each run

1. Save the exact terminal line, including status, first cause, execution diagnostic and cleanup state. Do not synthesize PASS from individual stages.
2. Record the run start/end timestamps, execution ID, scenario ID, contract IDs, raw log path/hash and terminal result in a copy of `provenance-manifest.template.json`.
3. Verify the manifest SHAs and file fingerprints against the actual repositories and raw log. A Git commit SHA identifies a commit tree; it does not identify dirty bytes loaded by Unity.
4. Keep each execution in its own directory, for example `CertificationEvidence/PlayerJoin/<execution-id>/`, with the completed manifest, raw log and terminal text. Do not overwrite earlier failures.

## Certification coverage status

- Slice 1 has a user-reported `PASS 8/8` with unchanged revision on invalid Join, subsequent valid Join, Actor preparation/materialization, Leave and `BaselineRestored`. This execution predates the bridge rollback correction and has not been replayed afterward. It remains partial-provenance until original artifacts and hashes are associated in `Evidence/Slice1-PlayMode/`.
- Slices 2–3 preserve their separately reported outcomes; their recorded results are not upgraded by this procedure.
- A package-owned fake backend already exists as an internal seam: `LocalPlayerProvisioningBridge` accepts `ILocalPlayerProvisioningBackend`; the test assembly has `InternalsVisibleTo`. The user reports all nine `LocalPlayerProvisioningBridgeRollbackTests` passed after the internal callback compensation correction. Fake `RejectPlayer` records a compensation request; it does not certify real Unity destruction.
- The user reports Unity Test Runner `229/229 PASS`, zero failures; PlayerParticipation `38/38`; `LocalPlayerJoinContractTests` `26/26`; `LocalPlayerProvisioningBridgeRollbackTests` `9/9`. This agent did not execute Unity or NUnit. The capture is in the conversation; execution metadata, bytes and SHA are not associated with the manifest, so provenance stays partial.
- Real PlayerInputManager behavior and actual Unity object destruction timing remain unproven by fake-backend EditMode tests. Integrated `BaselineRestored` requires original evidence and a provenance-complete run.

## Package-owned rollback matrix

The user reports the nine tests in `LocalPlayerProvisioningBridgeRollbackTests` passed after the callback compensation fix. The fake verifies compensation requests, not physical Unity object destruction.

| Failure point | Test / induced condition | Expected typed result and evidence | Session/physical cleanup assertion | Subsequent Join | State |
|---|---|---|---|---|---|
| Before reservation | `JoiningClosed_RejectsBeforeReservationOrBackendCall` | `RejectedJoiningClosed`; reservation operation result is rejected, no successful reservation/rollback evidence | Snapshot confirms Joining closed, unchanged revision/counts, zero reservations; backend untouched | Not needed for unchanged baseline | User-reported 9/9; `HasReservationEvidence` means a reservation result exists, not that a reservation was acquired |
| After reservation | `BackendThrowsAfterReservation_RollsBackSlotAndReturnsTypedFailure` | `FailedAdmission`; successful reservation and rollback evidence; same method also induces stale token and expects `FailedRollback` | Successful rollback returns Available; stale-token case records failed rollback result; zero Reserved/Joined | Available after successful rollback; stale-token mutation was consumed by the competing Session owner | User-reported 9/9; stale-token failure is synthetic reentrant contention, not an ordinary backend failure |
| During creation, after callback | `BackendCreatesCallbackPlayerThenThrows_RollsBackAndRequestsCompensation` | `FailedAdmission`; callback PlayerInput exact reference reaches fake `RejectPlayer`; reservation rollback succeeds | Session Available; Host not staged/joined; operation pending cleared. Fake records request only, not destruction | Available baseline supports retry; not retried in this test | User-reported 9/9; real Unity destruction not proven |
| Destroyed PlayerInput reference | `DestroyedPlayerInputReferenceAfterCallback_IsRejectedAndReservationReleased` | `RejectedMissingPlayerInput`; successful reservation and rollback evidence | Session Available; fake destroys object with `DestroyImmediate` before returning it | Available baseline supports retry; not retried in this test | User-reported 9/9; EditMode fake-null branch only, not frame timing |
| Null provisioning result and recovery | `BackendReturnsNullAfterReservation_RollsBackAndAllowsLaterJoin` | First `RejectedProvisioningReturnedNull` with rollback; next result `SucceededJoined` with commit/callback evidence | Snapshot after first failure is Available; next Join reaches Joined | Explicitly succeeds | User-reported 9/9 |
| Callback correlation | `DifferentCallbackAndReturnedPlayerInputs_RollsBackAndRejectsBothInputs` | `RejectedCorrelationMismatch`, `RejectedDifferentPlayerInput`, successful rollback | Session Available; fake receives both distinct rejection requests | Available baseline supports retry; not retried in this test | User-reported 9/9; real Manager callback timing not proven |
| Host missing/invalid | `MissingLocalPlayerHost_RollsBackAndRequestsCompensation`; `InvalidLocalPlayerHostConfiguration_RollsBackAndRequestsCompensation` | Typed Host rejection and successful rollback | Session Available; fake receives rejection request for provisioned input | Available baseline supports retry; not retried in these tests | User-reported 9/9; destruction timing not proven |
| Session commit then Host commit failure | `HostCommitThrowsAfterSessionCommit_CompensatesHostSlotAndProvisionedPlayer` | `FailedAdmission`, successful Session commit evidence plus successful rollback evidence | Host not Joined, Session Available, fake receives input rejection request | Available baseline supports retry; not retried in this test | User-reported 9/9; fault is injected by a package-owned Session change listener |
| Session commit itself fails | No test | The concrete Session context has no injectable commit collaborator; no runtime mutation or authority-corrupting fake added | Failure and cleanup semantics remain unproven | Unknown | Blocked by test seam |
| Rollback itself fails | `BackendThrowsAfterReservation_RollsBackSlotAndReturnsTypedFailure` stale-token subcase | `FailedRollback`, original `FailedAdmission`, failed rollback result `RejectedForeignOrStaleReservation` | Competing Session mutation consumes the reservation first; bridge does not claim its recovery succeeded | Slot is Available due to competing release, not bridge rollback | User-reported 9/9; physical compensation failure remains unrepresented because backend rejection is `void` |

The fake backend records calls to `RejectPlayer`; it deliberately does not claim Unity PlayerInputManager ownership or physical destruction. The new tests do not replace integrated Play Mode evidence.

## Manual Unity validation still required

1. Open QAFramework in Unity `6000.5.0f1` and rerun `LocalPlayerJoinContractTests` (26) and `LocalPlayerProvisioningBridgeRollbackTests` (9) from Test Runner; retain the user-reported 229/229 capture separately and save fresh NUnit XML/logs with execution provenance.
2. Run the Slice 1 Configure command twice and inspect authoring validators/idempotence as described in the Slice README.
3. Select the Slice 1 GameApplication, run a fresh Play Mode session, capture the raw Unity log from before scenario start through `[QA-PLAYER-SESSION-JOIN]`, then complete the manifest with the exact terminal.
4. Repeat Slice 1 under the recorded baseline if its cleanup/composition has changed. Preserve any FAIL/BLOCKED run and its evidence; do not replace it with a later result.

The 229/229 NUnit result and Slice 1 PASS above were reported by the user from Unity, not executed independently by this agent. Slice 1 remains pending replay after the bridge correction; integrated failure scenarios and real Unity physical destruction remain unproven.
