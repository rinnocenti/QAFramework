# QA Player — Slice 1: Session / Join

This focused QA-001 scenario certifies one Manager-Provisioned P1 Session lifecycle against the Framework package resolved by QAFramework. It uses the public Activity-scoped Join command and Session Observer. It does not certify gameplay input, camera behavior, Player movement, or gameplay admission.

## Configure and validate authoring

1. Open QAFramework with Unity `6000.5.0f1` and record the resolved Framework package HEAD and dirty state. The manifest/lock points at the local `.7` package without an immutable hash.
2. Run **Immersive Framework → QA → Player → Session Join → Configure Session Join Slice**.
3. Run Configure a second time. Both runs should report `setup='Configured' baseline='Valid'`; the second must update the QA-owned paths without duplicate scene entries or assets.
4. Inspect the generated GameApplication, Session Profile, P1 Slot Profile, Actor Profile, Host prefab, Activity, Route, content profiles and scene references. Run the installed Inspector validators for GameApplication, Route, Activity, Session/Profile, Actor/Host and Camera Output. Setup's `baseline='Valid'` is authoring postflight, not runtime certification.
5. Confirm all three scene paths are enabled in the active Build Profile/Scene List. Configure adds them to `EditorBuildSettings.scenes` while preserving unrelated entries.

## Run manually

1. Select `Assets/QA-IF-ADR-PLAYER-SESSION/Settings/GameApplication_QaPlayerSessionJoin.asset` in `ImmersiveFrameworkSettings`.
2. Open `Assets/QA-IF-ADR-PLAYER-SESSION/Scenes/QA_PlayerSessionJoin_Persistent.unity` and enter a fresh Play Mode session.
3. Wait for `[QA-PLAYER-SESSION-JOIN]`. Send the complete terminal line and preceding Framework diagnostics for review.

The Activity's primary content scene hosts the scoped Observer, Join command and scenario. The Persistent Content scene hosts the explicit PlayerInputManager and registered provisioning endpoint. The Route scene contains the Route contribution. A minimal fixed Camera Output is present only because the resolved bootstrap validates Camera Session and requires an explicit valid output prefab; it has no Player assignment and is not part of the Player assertion.

## Scenario and terminal

Expected sequence: valid empty/closed Session baseline → null-device public Join rejected as `RejectedInvalidRequest` with unchanged Session revision → public OpenJoining → explicit synthetic Keyboard Join → P1 Slot and technical Host/PlayerInput correlated → `ResolveConfiguredDefault` selects the authored default Actor → Actor logical preparation and physical materialization observed → gameplay admission remains false at `LogicalActorsPrepared` → public Leave and CloseJoining → synthetic Keyboard/Host released and baseline restored.

Expected terminal shape:

```text
[QA-PLAYER-SESSION-JOIN] status='Passed' verdict='PASS' cases='8/8' ... actorSelected='True' actorPrepared='True' actorMaterialized='True' gameplayAdmitted='False' cleanup='BaselineRestored' execution='' cleanupIssue=''.
```

`FAIL` means the valid baseline and public action were exercised and the contract diverged. `BLOCKED` means bootstrap, scoped authority, valid authoring, or another precondition prevented a valid exercise. The invalid Join case passes only when the exact typed rejection and unchanged baseline are observed. Terminal publication follows public Leave/CloseJoining, device removal and final baseline checks. Interrupted execution has no PASS path; it emits BLOCKED/FreshBootRequired if the component is disabled before completion.

## Limitations

The implementation received static review. A user-reported Play Mode PASS is recorded in the results section below; the terminal alone does not establish Inspector validation, Configure idempotence or package compilation/import. Package tests remain responsible for deterministic contracts. IF-ADR-044's independent certification is not duplicated. All used Player Session/Manager-Provisioned surfaces remain Experimental in Framework `.7`.

## Slice 2: Leave / Rejoin / Actor occurrence

Slice 2 uses a separate GameApplication, Route, Activity and Activity Content Profile, while reusing the validated Slice 1 Session, P1 Slot, default Actor, Host, Persistent Content and technical Camera Output. Run the Slice 1 Configure command twice first; its Host setup now includes the exact co-located `UnityPlayerInputGateAdapter` and `PlayerPauseInput` required by the Framework's Pause binding. The technical `Global/Pause` action is deliberately unbound, so Slice 2 does not certify or implement functional input or Pause behavior. Re-run Slice 1's manual scenario after this Host composition change.

### Configure and validate authoring

1. Run **Immersive Framework → QA → Player → Session Join → Configure Session Join Slice** twice. Both runs should preserve a single set of Slice 1 assets/scenes and report a valid baseline.
2. Run **Immersive Framework → QA → Player → Lifecycle → Configure Leave-Rejoin Slice** twice. It creates/updates only the Slice 2 GameApplication, Route, Activity, Activity Content Profile and two scenes, while adding those scenes to the existing Editor Build Settings list.
3. Select `Assets/QA-IF-ADR-PLAYER-SESSION/Settings/GameApplication_QaPlayerLeaveRejoin.asset` in `ImmersiveFrameworkSettings`.
4. Inspect and validate the GameApplication, Route, Activity, Activity Content Profile, shared Session/Slot/Actor/Host and Persistent Content. Confirm the Activity-scoped Observer, Join command and scenario appear exactly once in `QA_PlayerLeaveRejoin_Activity.unity`.
5. Open `Assets/QA-IF-ADR-PLAYER-SESSION/Scenes/QA_PlayerSessionJoin_Persistent.unity` and confirm the Manager-Provisioned endpoint/PlayerInputManager and shared Host configuration. The new Route and Activity scenes are `QA_PlayerLeaveRejoin_Route.unity` and `QA_PlayerLeaveRejoin_Activity.unity`.

### Run manually

Enter a fresh Play Mode session with the Slice 2 GameApplication selected. Review the ordered `[QA-PLAYER-LEAVE-REJOIN-OCCURRENCE]` stage logs and terminal. The expected lifecycle is P1 Join/Actor A ready → successful Leave and A invalidation → same P1 Slot rejoined with distinct Actor B → stale A Leave rejected as `RejectedForeignOrStaleOccurrence` while B remains current → final Leave and B invalidation → closed, empty Session and physical resources released.

The scenario compares typed Slot, Player occurrence, Host binding and Actor Runtime Content identities, materialization revisions, and owned Host/Input/Actor object references. `ActorId` identifies configured Actor data and is not treated as the physical occurrence identity. Both occurrences must reach logical preparation and physical materialization; gameplay admission must remain false. A successful terminal is `[QA-PLAYER-LEAVE-REJOIN-OCCURRENCE][terminal] verdict='PASS'` with `cleanup='BaselineRestored'`, `leaveA='Succeeded'`, `staleLeave='RejectedForeignOrStaleOccurrence'`, and `leaveB='Succeeded'`. A valid exercised contract divergence is `FAIL`; unavailable setup or required public observability is `BLOCKED`. Cleanup precedes terminal publication. An interrupted run cannot report PASS and requires a fresh Play Mode boot.

The implementation received static review. The user later reported a manual PASS as recorded below; no raw logs/SHA were supplied for independent review. Configure idempotence, compilation/import and Inspector validator evidence remain separate from that scenario result. This scenario does not implement Player 2, functional Input/Pause, gameplay admission, camera gameplay, or Route/Activity transitions.

## Slice 3: Local Multiplayer / Input Ownership

The independent Slice 3 fixture is generated by **Immersive Framework → QA → Player → Multiplayer → Configure Local Multiplayer Ownership Slice**. Run Configure twice. The configurator owns the generated `QaPlayerMultiplayerOwnership` assets/scenes and updates the QA Scene List while preserving unrelated entries. Its `SerializedPostflight` message covers typed links and serialized contract checks only; it does not certify GameApplication Inspector validity.

Select `Assets/QA-IF-ADR-PLAYER-SESSION/Settings/GameApplication_QaPlayerMultiplayerOwnership.asset` in `ImmersiveFrameworkSettings`, then inspect the generated GameApplication, P1/P2 Slot Profiles, Session Profile, Route, Activity, Activity Content Profile, Persistent Content Scene, Host, Actor Runtime Host, Input Actions and technical Camera Output. Run the installed validators in the corresponding Inspectors and verify all generated scenes appear enabled in the active Build Profile Scene List. The Session uses `AllowDuplicates` because both Slots use the same default Actor Profile; this is a QA fixture choice, not a consumer-wide policy.

Open `Assets/QA-IF-ADR-PLAYER-SESSION/Scenes/QA_PlayerMultiplayerOwnership_Persistent.unity` and enter a fresh Play Mode session with the Slice 3 GameApplication selected. The Activity Scene runs the scenario automatically. It creates two QA-owned synthetic Keyboard devices, explicitly joins them in order, checks Slot/PlayerInput/Host/Actor occurrence and public input ownership evidence, leaves P1 while P2 remains active, rejoins P1 with device A, then leaves all Players and removes both devices.

Expected terminal:

```text
[QA-PLAYER-MULTIPLAYER-OWNERSHIP][terminal] verdict='PASS' ... cleanup='BaselineRestored'
```

A valid exercised contract divergence is `FAIL`; unavailable boot, scoped authority, or required public observation is `BLOCKED`. The terminal is emitted after cleanup. An interrupted run cannot report PASS and requires a fresh Play Mode boot.

This slice does not certify gameplay action reads, Input Gate/Pause behavior, camera assignments, movement, selection UI, Route/Activity transitions, or Player 2-specific policies. Player Session, Manager-Provisioned lifecycle, Actor occurrence and input ownership surfaces used here are `Experimental` in the QA-resolved Framework `.7`. A user-reported Play Mode PASS is recorded below; Editor import, Configure idempotence and Inspector validation were not established by that terminal.

## Resultados relatados dos Slices (registrados em 2026-10-11)

Os terminais abaixo foram fornecidos pelo usuário após execução manual no Unity. A data acima é de registro documental; timestamp de execução, Editor.log bruto e SHA imutável do Framework carregado não foram fornecidos. Portanto são resultados relatados, não reexecução independente nesta revisão. O projeto atualmente declara Unity `6000.5.0f1` e resolve Framework `.7` por caminho local, o que não prova a versão binária usada nos runs.

| Slice | Terminal relatado | Evidência e fronteira |
|---|---|---|
| Session / Join | `PASS 8/8`, `BaselineRestored` | `RejectedInvalidRequest`, Join P1 `SucceededJoined`, default Actor selecionado/preparado/materializado, Leave `SucceededLeft`; não demonstra gameplay admission ou outras políticas |
| Leave / Rejoin / Actor Occurrence | `PASS`, `BaselineRestored` | A/B Joined e Leaves `SucceededLeft`; mesmo Slot, B distinta de A; stale Leave A `RejectedForeignOrStaleOccurrence`, B preservada |
| Local Multiplayer / Input Ownership | Primeira execução `FAIL` / `FreshBootRequired`, `firstCause='Final cleanup did not restore the Session baseline.'`; execução posterior `PASS` / `BaselineRestored`, `firstCause=''`, `cleanupIssue=''` | P1/P2 Joined em devices distintos, isolamento confirmado; P2 sobrevive ao Leave/Rejoin P1; occurrence nova P1; leaves finais `SucceededLeft`; dois Slots disponíveis, zero Joined/Reserved/Leaving/selections, Host/PlayerInput/Actor destruídos, devices despareados/removidos |

O `FAIL` inicial do Slice 3 permanece como diagnóstico e não é substituído pelo `PASS` posterior. Slice 3 demonstra ownership/isolation; não prova action reads do `PlayerGameplayInputReader`, Input Gate/Pause, movimento, camera assignment ou readiness matrix. Nenhum dos três resultados certifica a família Player inteira ou altera maturidade de API.
