# IF-ADR-014 — Object Entry Recognition and Composition Lifecycle Play Mode demo

This QAFramework example exercises the public IObjectEntryRecognitionQuery and optional Object Entry composition against real Route and Activity lifecycle commits. Its composed prefab is only a lifecycle witness; it does not add gameplay perception, target selection, or contextual interaction.

## Prepare and run

1. Open QAFramework in Unity 6000.5.0f1 and wait for package import and script compilation.
2. Run **Immersive Framework → QA → IF-ADR-014 → Generate Object Entry Recognition and Composition Lifecycle Demo**. It creates or repairs the B1 recognition fixtures and adds a composed Object Entry to the Route and Activity scenes, a replacement Route, and the generated witness prefab/profile assets under Assets/QA-IF-ADR-014/Generated/. Existing resources are reused; conflicting or dirty scenes are preserved and reported. The generator preserves loaded Editor scenes. If there is no valid loaded scene, it uses an empty scene as a safe restoration target.
3. Add these generated scenes to the active Build Profile / Shared Scene List:
   - Assets/QA-IF-ADR-014/Generated/Scenes/QA_IF_ADR_014_Persistent.unity
   - Assets/QA-IF-ADR-014/Generated/Scenes/QA_IF_ADR_014_Route.unity
   - Assets/QA-IF-ADR-014/Generated/Scenes/QA_IF_ADR_014_ReplacementRoute.unity
   - Assets/QA-IF-ADR-014/Generated/Scenes/QA_IF_ADR_014_Activity.unity
4. Select GameApplication_QaIfAdr014 in **Project Settings → Immersive Framework → Active Game Application**.
5. Keep Scene Reload enabled, enter a fresh Play Mode session, and filter the Console for IF-ADR-014-DEMO.

The scenario runs automatically. It waits for the Route commit, queries a declared but unadmitted Persistent Content object, requests the Activity, and checks both colliders. It then restarts the same Activity while preserving the old physical root in Unity DontDestroyOnLoad, verifies that its capability is removed before scene unload, and checks that the re-admitted ObjectEntryId points to a new physical root. Before replacing the Route, it preserves the new Activity and Route roots the same way, proving both capabilities are removed before their owner scenes unload. The witness captures the owner Scene before the root moves to DontDestroyOnLoad. The generated roots are destroyed after all probes. No gameplay input is needed.

## Expected Console evidence

- declared-but-not-admitted: Unavailable. The declaration and Collider exist in Persistent Content, while the Route query anchor is committed.
- multiple-colliders-one-occurrence: both Collider queries are Resolved; both results contain the same typed Object Entry ID and the same declaration root as Occurrence.
- target-owner-released-query-anchor-valid: the released Activity target is Unavailable; the Route-owned query still resolves its own Route target.
- query-anchor-owner-released: the retained Activity query returns Unavailable for the still-live, committed Route target after the Activity scene starts unloading.
- `activity-restart-composition-released-before-reentry` and `activity-composition-released-before-scene-unload` pass only when each composed witness is destroyed while its exact declaration root and ObjectEntryId are still intact and its original owner Scene is still loaded.
- `activity-restart-readmitted-new-physical-occurrence` passes only when the restarted Activity resolves the same ObjectEntryId to a different declaration root.
- `route-composition-released-before-scene-unload` proves the same release order for Route content.
- The terminal line reports status=Passed cases=10/10. Any unexpected result reports Failed.

Each result line includes the public result Diagnostic. Output follows the QAFramework structured Unity Console convention; the Framework logging adapter is internal and is not a consumer API.

## Validation record

The Project Owner reports that the scenario was executed in Unity 6 and ended with `[IF-ADR-014-DEMO] status='Passed' cases='10/10'`. The reported run includes Object Entry recognition, same-occurrence multiple-collider queries, query invalidation, Activity restart/readmission with a new physical root, cleanup before Activity/Route scene unload, and Replacement Route loading. This is owner-reported Play Mode evidence; no raw Console log or execution source SHA was attached, and Codex did not run Unity.

The earlier full Framework Edit Mode run reported 514/517 tests. The three reported regressions were corrected in the merged Framework PR, but no post-fix full-suite run is available. A later Framework cleanup guard is under follow-up review and is not part of the merged Framework revision; rerun the full 517-test suite and this 10-case Play Mode scenario against the final integrated Framework revision before technical closure.

## Restore and repeat

Exit Play Mode, restore the previous Active Game Application and Build Profile / Shared Scene List, and remove the generated scene paths from the list. To repeat setup safely, run the same menu command again; it reuses the generated files and does not recreate scenes. If the Generated folder is incomplete or contains conflicting references, setup stops without deleting or replacing the existing resources. Authored source files are outside that generated folder.
