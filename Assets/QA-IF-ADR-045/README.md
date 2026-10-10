# IF-ADR-045 — Persistent Content lifecycle QA

This feature-owned QA fixture validates runtime-safe Persistent Content through the real Framework bootstrap. It reuses the checked-in QA-NEW-004 Route and Activity assets and creates all disposable assets under `Assets/QA-IF-ADR-045/Generated/`. It does not modify QA-NEW-004, runtime code, Build Profiles, Shared Scene List, or the active Game Application.

## Preflight and preparation

1. Open QAFramework with Unity `6000.5.0f1`; let package import/compilation finish.
2. Confirm the QA-NEW-004 source graph is present and imported: `GameApplication_QaNew004.asset`, Routes A/B/D, Activities D-A/D-B, Activity D-A Content Profile, Route A/B/D Primary Scenes, Activity D-A Content Scene, Camera Output prefab, and Session Camera Assignment/Rig. Prepare performs typed-load, main-asset, file, scene-path, Camera Session and assignment validation before creating generated assets. If a typed asset is null while its file exists, resolve Unity import/compile errors first; the diagnostic reports the source path, main asset type and expected type.
3. Save and name every open scene before setup. The setup refuses to proceed while any loaded scene is dirty or untitled; it never offers a Save/Don't Save prompt that could discard work and uses only restorable scene asset paths.
4. In **Project Settings → Editor → Enter Play Mode Settings**, keep Scene Reload enabled. Each lane requires a fresh scene-owned observer and trigger state.
5. Run **Immersive Framework → QA → IF-ADR-045 → Prepare Persistent Content Lifecycle QA**. It refuses to overwrite an existing `Generated` folder. It copies the canonical GameApplication, preserving its Camera Session output prefab and assignment, then replaces only the startup Route and Persistent Content reference fields. It temporarily opens its generated scene in Single mode and restores the captured asset-backed scene setup in `finally`. Before reporting `Prepared`, it independently reopens both saved scenes, checks the serialized Scenario and trigger references/cardinality, then reloads all three generated GameApplications by canonical path and validates their persisted Routes, Persistent Content, Camera Session and dependencies. AssetDatabase create/copy/save operations are checked at each dependency boundary; no broad refresh is used. If preparation stops partway or postflight fails, use the scoped cleanup command; it deletes only the known D7-generated asset paths and preserves this README, scripts, asmdefs, the temporary pre-existing `.meta`, and unrelated assets.
6. Record the active Build Profile / Shared Scene List and current Game Application. Manually include exactly these scenes for the valid lane and restore the previous list afterward:

   - `Assets/QA-IF-ADR-045/Generated/Scenes/QA_IF_ADR_045_Persistent.unity` — Persistent Content source.
   - `Assets/QA-NEW-004/Scenes/QA_NEW_004_RouteA.unity` — startup/restoration Route.
   - `Assets/QA-NEW-004/Scenes/QA_NEW_004_RouteB.unity` — Route transition target.
   - `Assets/QA-NEW-004/Scenes/QA_NEW_004_RouteD.unity` — Route with startup Activity D-A.
   - `Assets/QA-NEW-004/Scenes/QA_NEW_004_ActivityDA.unity` — Activity D-A Content, released by the D-A → D-B transition.

   Keep `Generated/Scenes/QA_IF_ADR_045_Unavailable.unity` excluded for that negative lane. Do not change the Build Profile or Shared Scene List through the setup.
7. Select `Assets/QA-IF-ADR-045/Generated/Settings/GameApplication_QaIfAdr045.asset` in **Project Settings → Immersive Framework → Active Game Application**.

## Scoped migration

Run **Immersive Framework → QA → IF-ADR-045 → Run Scoped Persistent Content Migration Cases**. The command invokes only `PersistentContentSceneReferenceMigration.MigrateAsset` for its three D7-owned Game Applications; it never calls `MigrateAll` or the global migration menu.

The command verifies serialized legacy reference, `scenePath`, and `sceneName` before and after migration. Expected cases: legacy conversion (`Migrated`), a second run (`Unchanged`), invalid non-Scene reference (`Invalid`, unchanged), and a modern/legacy conflict (`Conflict`, unchanged). Expected evidence: `[IF-ADR-045-MIGRATION] status='Passed' cases='4/4' ...`.

## Lifecycle execution

For each lane, select the stated Game Application, enter a fresh Play Mode session, and preserve the full Console diagnostics.

### Valid lifecycle lane

Use `GameApplication_QaIfAdr045.asset`. The runtime scenario records the source scene's exact authored path and handle plus other loaded scene paths/handles observed in `Awake`; it retains the captured source path for terminal evidence because Unity clears the unloaded `Scene.path`. If it cannot observe a co-loaded scene it reports **BLOCKED** rather than claiming additive co-load. Bootstrap/Session bindings are a separate first checkpoint from exact source unload/root transfer. It observes `SceneManager.sceneUnloaded` for the exact source handle, checks each retained original root reference and `EntityId`, and validates its actual owning Scene through `GameObject.scene` and the common persistent-scene handle. A failure names the specific missing observation. It then requests Route A → B → D, verifies each target path is active and the previous Route Primary Scene is unloaded, checks Activity D-A Content loaded, requests Activity D-A → D-B and waits for D-A Content release, and restores Route A while checking the roots remain application-scoped.

The current public QA surface cannot prove normal Session shutdown release. `SceneLifecycleEvents` is dispatched only for Scene scopes; Persistent Content source unloading is performed directly by `GlobalUiSceneRuntime`, and Session release has no public notification. D8 classified this checkpoint as **PACKAGE-LEVEL**: the release protocol belongs to internal Framework lifecycle tests, and no public shutdown API will be added solely for this QA. Therefore the valid run's terminal is intentionally **BLOCKED** at `6/7`, with `routeActivityLifecycle='Passed'` and `sessionShutdown='BLOCKED'`. This is not an overall PASS. The former `[IF-ADR-045-SHUTDOWN]` callback was not valid evidence and has been removed. Only the terminal's overall `status`/`verdict` represents the result; the scoped lifecycle sub-result must not be promoted to overall PASS.

### D7–D10 evidence status

D7's user-reported Unity run completed six of seven runtime checkpoints:

1. Bootstrap and Session composition.
2. Exact Persistent Content source unload and transfer of the original roots to application lifetime.
3. Route A → Route B.
4. Route B → Route D and startup Activity D-A Content load.
5. Activity D-A → D-B and D-A Content release.
6. Route restoration to A, cleanup, and root/binding baseline checks.

Checkpoint 7, Session shutdown/release, remains **BLOCKED**. The terminal is
`6/7 BLOCKED`, not `7/7 PASS`. Prepare/cleanup were reported PASS and scoped
migration was reported `4/4 PASS`. These are the supplied D7 results; this README
does not convert them into a fresh run or certify other environments.

D9 assigns internal release regression coverage to the Framework package. D10
records the user-reported Unity Test Runner Edit Mode result: 194 passed, 0 failed,
0 ignored; the supplied report separately identifies `SessionCompositionReleaseTests`
as PASS, Audio 5/5, and `PersistentContentSceneReferenceTests` 12/12. This package
evidence covers protocol behavior; it does not make the consumer-observed Session
shutdown possible or prove the full
`FrameworkRuntimeHost.OnDestroy → ReleaseSessionScope` integration. That host
integration remains an explicit residual gap. Release detaches Session participants
and feature bindings; it is not an instruction to destroy the Persistent Content
GameObjects.

### Negative and retry lanes

If a negative lane fails before loading the Persistent Content scene, its runtime scenario cannot emit a terminal. Preserve the Framework diagnostic and scene state; a missing D7 terminal is never PASS.

| Lane | Setup | Expected evidence |
|---|---|---|
| Invalid explicit path | Select `GameApplication_QaIfAdr045_InvalidPath.asset`; it has a missing explicit path and a valid retained legacy SceneAsset/name | Framework rejects the exact path; preserve the causal diagnostic and verify no homonymous scene was accepted. |
| Valid but unavailable path | Select `GameApplication_QaIfAdr045_UnavailablePath.asset`; keep its exact scene path out of the active scene set | Framework rejects the unavailable path. If Unity reports it loadable in the current Editor profile, mark the lane BLOCKED; do not rewrite the list automatically. |
| Fresh bootstrap after pre-composition rejection | Exit Play Mode after an invalid/unavailable lane, select the valid D7 application, then start a fresh session | Proves only that a new valid boot can run after exiting a rejected boot. This is not an in-session retry and does not prove rollback after partial Session composition. |

Injected participant rejection, partial Session composition, shutdown after partial composition, and compensation/retry after partial composition remain **BLOCKED**. The relevant participant/scope contracts are internal and there is no supported fault injection surface. D7 adds no runtime seam, reflection, or public contract.

## Restore and cleanup

1. Exit Play Mode and save/name all modified or untitled open scenes. Cleanup refuses to continue while any scene is dirty or untitled.
2. Restore the previous Active Game Application and Build Profile / Shared Scene List. Remove both D7 scene paths from every profile/list where you added them. Cleanup checks that no D7 Game Application remains active and aborts without deleting if one does.
3. Run **Immersive Framework → QA → IF-ADR-045 → Remove Generated Persistent Content Lifecycle QA**. The command closes only the two explicitly generated D7 scenes, deletes only the known D7 asset paths, and removes generated folders only when empty. A `BlockedResiduals` cleanup status means unknown files remain; inspect and remove them deliberately. Authored QA code and the pre-existing temporary `.meta` are outside its deletion scope.
4. Confirm `Assets/QA-IF-ADR-045/Generated/` is absent, the prior application/profile configuration is restored, and the source scene setup is intact. Cleanup never deletes the QA feature root or its authored source files.

The static review itself did not run Unity. The D7 Play Mode and D9 Edit Mode
results above are supplied user evidence; they were not rerun during D10. Standalone
and WebGL Player Builds remain later validation gates.
