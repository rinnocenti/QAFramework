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

- Slice 1 has a new user-reported `PASS 8/8` with unchanged revision on invalid Join, subsequent valid Join, Actor preparation/materialization, Leave and `BaselineRestored`. It remains partial-provenance until originals and hashes are copied into `Evidence/Slice1-PlayMode/`.
- Slices 2–3 preserve their separately reported outcomes; their recorded results are not upgraded by this procedure.
- A package-owned fake backend already exists as an internal seam: `LocalPlayerProvisioningBridge` accepts `ILocalPlayerProvisioningBackend`; the test assembly has `InternalsVisibleTo`. New tests exercise reserve/rollback and callback/Host failure paths without changing Runtime. They are authored but not yet run.
- The existing EditMode suite result is user-reported: 220 total QAFramework tests, 0 failures; PlayerParticipation 29/29; `LocalPlayerJoinContractTests` 26/26 and `ActivityPlayerActorLeaveLedgerTests` 3/3. It does not include the newly authored `LocalPlayerProvisioningBridgeRollbackTests`.
- Real PlayerInputManager behavior and actual Unity object destruction timing remain unproven by fake-backend EditMode tests. Integrated `BaselineRestored` requires original evidence and a provenance-complete run.

## Package-owned rollback matrix

All rows marked **authored** refer to the nine tests in `LocalPlayerProvisioningBridgeRollbackTests`; none has been executed yet.

| Failure point | Test / induced condition | Expected typed result and evidence | Session/physical cleanup assertion | Subsequent Join | State |
|---|---|---|---|---|---|
| Before reservation | `JoiningClosed_RejectsBeforeReservationOrBackendCall` | `RejectedJoiningClosed`; no reservation/rollback evidence | Revision and available count unchanged; backend untouched | Not needed for unchanged baseline; not separately run in this test | Authored |
| After reservation | `BackendThrowsAfterReservation_RollsBackSlotAndReturnsTypedFailure` | `FailedAdmission`; successful reservation and rollback evidence | One Slot Available, zero Reserved/Joined; no PlayerInput returned | Available baseline supports retry; this test does not retry | Authored |
| During creation, after callback | `BackendCreatesCallbackPlayerThenThrows_RollsBackAndRequestsCompensation` | `FailedAdmission`; successful reservation and rollback evidence | Session returns Available; fake observes `RejectPlayer` request for callback PlayerInput | Available baseline supports retry; this test does not retry | Authored; actual Unity destruction not proven |
| Destroyed PlayerInput reference | `DestroyedPlayerInputReferenceAfterCallback_IsRejectedAndReservationReleased` | `RejectedMissingPlayerInput`; successful reservation and rollback evidence | Session Available; fake destroys the object with `DestroyImmediate` before returning it | Available baseline supports retry; this test does not retry | Authored; EditMode fake-null branch only, not frame timing |
| Null provisioning result and recovery | `BackendReturnsNullAfterReservation_RollsBackAndAllowsLaterJoin` | First `RejectedProvisioningReturnedNull` with rollback; next result `SucceededJoined` with commit/callback evidence | Snapshot after first failure is Available; next Join reaches Joined | Explicitly succeeds | Authored |
| Callback correlation | `DifferentCallbackAndReturnedPlayerInputs_RollsBackAndRejectsBothInputs` | `RejectedCorrelationMismatch`, `RejectedDifferentPlayerInput`, successful rollback | Session Available; fake receives both distinct rejection requests | Available baseline supports retry; this test does not retry | Authored; real Manager callback timing not proven |
| Host missing/invalid | `MissingLocalPlayerHost_RollsBackAndRequestsCompensation`; `InvalidLocalPlayerHostConfiguration_RollsBackAndRequestsCompensation` | Typed Host rejection and successful rollback | Session Available; fake receives rejection request for provisioned input | Available baseline supports retry; these tests do not retry | Authored; destruction timing not proven |
| Session commit then Host commit failure | `HostCommitThrowsAfterSessionCommit_CompensatesHostSlotAndProvisionedPlayer` | `FailedAdmission`, successful Session commit evidence plus successful rollback evidence | Host not Joined, Session Available, fake receives input rejection request | Available baseline supports retry; this test does not retry | Authored; fault is injected by a package-owned Session change listener |
| Session commit itself fails | No test | The concrete Session context has no injectable commit collaborator; no runtime mutation or authority-corrupting fake added | Failure and cleanup semantics remain unproven | Unknown | Blocked by test seam |
| Rollback itself fails | No test | No natural deterministic failure seam found while preserving a valid reservation/Joined state | Must not infer restored baseline or fake Unity destruction | Unknown | Blocked by test seam/semantics |

The fake backend records calls to `RejectPlayer`; it deliberately does not claim Unity PlayerInputManager ownership or physical destruction. The new tests do not replace integrated Play Mode evidence.

## Manual Unity validation still required

1. Open QAFramework in Unity `6000.5.0f1` and run `LocalPlayerJoinContractTests` and the new `LocalPlayerProvisioningBridgeRollbackTests` from Test Runner; retain the original 26/26 capture separately from the new result and capture NUnit XML/logs.
2. Run the Slice 1 Configure command twice and inspect authoring validators/idempotence as described in the Slice README.
3. Select the Slice 1 GameApplication, run a fresh Play Mode session, capture the raw Unity log from before scenario start through `[QA-PLAYER-SESSION-JOIN]`, then complete the manifest with the exact terminal.
4. Repeat Slice 1 under the recorded baseline if its cleanup/composition has changed. Preserve any FAIL/BLOCKED run and its evidence; do not replace it with a later result.

The 220/0 NUnit result and Slice 1 PASS above were reported by the user from Unity, not executed independently by this agent. The new bridge rollback tests and integrated failure scenarios remain unexecuted.
