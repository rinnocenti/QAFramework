# IF-ADR-014 — Object Entry Recognition Play Mode demo

This QAFramework example exercises the public IObjectEntryRecognitionQuery against real Route and Activity lifecycle commits. It does not add gameplay perception, target selection, or contextual interaction.

## Prepare and run

1. Open QAFramework in Unity 6000.5.0f1 and wait for package import and script compilation.
2. Run **Immersive Framework → QA → IF-ADR-014 → Generate Object Entry Recognition Demo**. It creates three scenes and their settings under Assets/QA-IF-ADR-014/Generated/. Existing complete output is reused on subsequent runs; a missing Game Application startup Route is repaired, an unexpected Route startup Activity is cleared, and missing Persistent-scene Activity references are repaired in place. The generator preserves loaded Editor scenes. If there is no valid loaded scene, it uses an empty scene as a safe restoration target.
3. Add these generated scenes to the active Build Profile / Shared Scene List:
   - Assets/QA-IF-ADR-014/Generated/Scenes/QA_IF_ADR_014_Persistent.unity
   - Assets/QA-IF-ADR-014/Generated/Scenes/QA_IF_ADR_014_Route.unity
   - Assets/QA-IF-ADR-014/Generated/Scenes/QA_IF_ADR_014_Activity.unity
4. Select GameApplication_QaIfAdr014 in **Project Settings → Immersive Framework → Active Game Application**.
5. Keep Scene Reload enabled, enter a fresh Play Mode session, and filter the Console for IF-ADR-014-DEMO.

The scenario runs automatically. It waits for the Route commit, queries a declared but unadmitted Persistent Content object, requests the Activity, and checks both colliders. Immediately before Clear Activity, it moves only the observed target root to Unity DontDestroyOnLoad so its Collider remains available during the post-unload query; the generated target is destroyed after the probes. No gameplay input is needed.

## Expected Console evidence

- declared-but-not-admitted: Unavailable. The declaration and Collider exist in Persistent Content, while the Route query anchor is committed.
- multiple-colliders-one-occurrence: both Collider queries are Resolved; both results contain the same typed Object Entry ID and the same declaration root as Occurrence.
- target-owner-released-query-anchor-valid: the released Activity target is Unavailable; the Route-owned query still resolves its own Route target.
- query-anchor-owner-released: the retained Activity query returns Unavailable for the still-live, committed Route target after the Activity scene starts unloading.
- The terminal line reports status=Passed cases=4/4. Any unexpected result reports Failed.

Each result line includes the public result Diagnostic. Output follows the QAFramework structured Unity Console convention; the Framework logging adapter is internal and is not a consumer API.

## Restore and repeat

Exit Play Mode, restore the previous Active Game Application and Build Profile / Shared Scene List, and remove the generated scene paths from the list. To repeat setup safely, run the same menu command again; it reuses the generated files and does not recreate scenes. If the Generated folder is incomplete or contains conflicting references, setup stops without deleting or replacing the existing resources. Authored source files are outside that generated folder.
