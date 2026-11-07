# IF-ADR-014 Amendment 003 — C4 Dynamic Composition QA

This fixture is an isolated Play Mode consumer scenario for the public com.immersive.framework Object Entry composition API. It creates its own Game Application, Route, replacement Route, Activity, scenes, Base Profile and prefab assets below Assets/QA-IF-ADR-014/C4/Generated/. It does not alter the B2 demo or its assets.

The C4 scenario is source-ready, not certified. The Project Owner must run it in Unity and retain the complete Console output. The prior B2 10/10 result is separate evidence and does not validate dynamic composition. The Project Owner's previously supplied 566/566 Edit Mode result supports the Framework C3 regression baseline (visual evidence, not SHA-fingerprinted); it is not a run of C4's new integrated Play Mode scenario.

## Prerequisites

- Open the QAFramework project with Unity 6000.5.0f1.
- Confirm the resolved packages in Packages/packages-lock.json. The C3 integration baseline is Framework master at 2bf0560b88e35168c159e52240e498e3cb363fcc, with com.immersive.composition@0.1.0-preview.3.
- Let package import and script compilation finish before running setup.
- Keep the active Game Application and Build Profile unchanged until the manual selection step.
- Enable Scene Reload for the Play Mode run. Restore its previous value afterward. The setup tool does not change Editor Settings.
- Before entering Play Mode, stop any current session, close manually loaded B2/C4 gameplay scenes, and start from a neutral non-gameplay scene. Do not open the generated Route or Activity scenes manually. This prevents the B2 controller and C4 controller from issuing GameFlow requests at the same time.

## Generate and select the fixture

1. Run **Immersive Framework → QA → IF-ADR-014 → C4 → Generate Dynamic Composition Certification**.
2. The generator creates or validates the isolated C4 assets. It leaves open scenes and their unsaved edits untouched, authors fixtures in additive scenes, and restores the prior Scene Manager Setup. If no Editor scene is loaded, it temporarily opens the saved QA-NEW-001 Persistent scene as a restoration workspace, then closes it after setup. If an Untitled scene is open, save it as a `.unity` asset, or close it only if it has no changes to preserve, before running setup; Unity does not allow this generator's additive authoring scenes while an Untitled scene remains unsaved. If loaded scenes cannot be activated, make a saved scene active before rerunning setup. A Generated tree containing only folder metadata from an interrupted first run is reused; if any generated files exist but the tree is incomplete or conflicting, the generator preserves them and reports the conflict without overwriting.
3. Add these scenes to the active Build Profile / Shared Scene List:
   - Assets/QA-IF-ADR-014/C4/Generated/Scenes/QA_IF_ADR_014_C4_Persistent.unity
   - Assets/QA-IF-ADR-014/C4/Generated/Scenes/QA_IF_ADR_014_C4_Route.unity
   - Assets/QA-IF-ADR-014/C4/Generated/Scenes/QA_IF_ADR_014_C4_ReplacementRoute.unity
   - Assets/QA-IF-ADR-014/C4/Generated/Scenes/QA_IF_ADR_014_C4_Activity.unity
4. Select GameApplication_QaIfAdr014C4 in Project Settings → Immersive Framework → Active Game Application.
5. Keep Scene Reload enabled, start a fresh Play Mode session, and filter the Console for [IF-ADR-014-C4].

The C4 Game Application starts its own Route and Persistent scene. Its persistent controller requests the Activity, restarts that Activity, and replaces the Route without gameplay input. The setup validator checks that none of the four generated C4 scenes contains the B2 controller; the neutral-scene step also prevents an already loaded B2 controller from running alongside C4.

## Defined Play Mode cases

The terminal count is 21 cases:

1. Plain Object Entry identity, recognition and absence of composition authoring or C4 parts.
2. Profile-only B2 Base materialization with dynamic opt-in off and no dynamic grant.
3. Profileless dynamic add, with a physical part under the exact root.
4. Same-key replace, with the prior instance physically destroyed.
5. Remove, with no physical residue and unchanged root/declaration/ID.
6. Base plus Dynamic override of the same key.
7. Removing the override restores the Base part.
8. Independent contributions with different keys coexist.
9. Closing one contribution preserves its sibling.
10. Higher priority wins a shared key.
11. Equal highest-priority conflict rejects without effects or materialization.
12. A hidden lower-priority tie is rejected when removal would reveal it.
13. A dependency resolves across independent contributions.
14. A missing dependency rejects without effects.
15. Read-only recognition and occurrence queries resolve the same ID/root before and after mutation; the grant remains on exact authoring.
16. Activity-owned dynamic part is committed under the admitted occurrence.
17. Activity Restart releases the old part while the original owner scene is loaded and the original root/declaration/ID remain intact.
18. Readmission keeps the logical ID and produces a different physical root; the old grant cannot open and the old contribution returns Stale.
19. The new Activity occurrence accepts a mutation through its new grant.
20. A non-empty physical snapshot confirms active parts on Profile-only, Dynamic-only, Base-plus-Dynamic and current Activity roots, tied to the exact loaded owner scenes before Route replacement.
21. Route replacement releases Route and current Activity parts physically before both owner scenes unload; old grants/handles remain stale.

Each case logs its ID, observed and expected values, pass flag, ObjectEntryId, physical evidence and diagnostic. Passed is emitted only as cases='21/21'. Fixture/readiness timeouts are deadlock guards and produce Blocked; elapsed time is not accepted as cleanup evidence.

The witnesses capture the exact declaration root, logical ID and owner scene when a prefab part is enabled. Their destruction record is checked after Unity has completed destruction. The lifecycle cases preserve roots with DontDestroyOnLoad only to inspect the root and declaration after the owner releases acquired parts. The scenario destroys these retained roots after confirmed Route replacement.

## C3 coverage and limits

There is no public consumer API in this Framework checkout for requesting Framework Host Controlled Stop. C4 therefore does not access FrameworkRuntimeHost or fabricate a stop path. C3 source tests cover the internal Host lifecycle:

- FrameworkRuntimeHostControlledStopTests.FullStartupAndControlledStop_RetainExactRootsAndSameHostForReleaseRetry exercises Controlled Stop with pending dynamic preparation, remove and contribution-close cleanup.
- StableObjectBindingHostTeardownTests.OccurrenceQuery_FrameworkHostDestroyUnbindsPreviouslyHandedOffInterface exercises abrupt Host destruction, grant invalidation and later cleanup after the test releases its gate.
- ObjectEntryCompositionMutationTests covers cancellation, owner-release drain, callback re-entry, failure, degraded replacement cleanup, retries and independent operation cleanup.

These are source coverage references. The Project Owner supplied visual evidence for a 566/566 full Edit Mode run after C3, including the named Controlled Stop and abrupt-teardown tests. Exact per-case XML and commit-fingerprinted execution evidence have not been supplied, and this does not certify the new C4 Play Mode scenarios. Abrupt teardown invalidates grants and requests cancellation; cleanup completion is explicitly unconfirmed and is not asserted by C4.

### C4 and C1–C3 coverage differences

- C4 Play Mode verifies Activity Restart readmission to the same logical `ObjectEntryId` on a different physical root. The same-root/same-owner stale-binding case remains in Edit Mode: `StableObjectBindingTests.DynamicComposition_GrantAndHandleStayStaleAfterExactRootReadmission` and `ObjectEntryCompositionMutationTests.DynamicContribution_OldHandleCannotMutateReadmittedSameRoot`.
- C4 exercises a hidden lower-priority tie revealed by `RemovePartAsync`. The separate `CloseAsync` degraded-cleanup contract is covered by `ObjectEntryCompositionMutationTests.DynamicContribution_CloseReportsHiddenLowerPriorityTieAsDegraded`.
- The corresponding non-degraded rejection for `RemovePartAsync` is covered by `ObjectEntryCompositionMutationTests.DynamicContribution_RemoveOverrideRejectsTieHiddenByHigherWinner`; both tie variants remain complementary to C4's Play Mode mutation of physical objects.
- Pending acquired-part cleanup and retry are covered by `StableObjectBindingTests.PendingAcquiredPartCleanup_RetainsOwnerUntilRetryConfirmsDestruction`. Cancellation, owner-release drain, callback re-entry, failures, degraded replacement cleanup and independent operation cleanup remain in `ObjectEntryCompositionMutationTests`.
- Controlled Stop uses `FrameworkRuntimeHostControlledStopTests.FullStartupAndControlledStop_RetainExactRootsAndSameHostForReleaseRetry`. Abrupt Host teardown uses `StableObjectBindingHostTeardownTests.OccurrenceQuery_FrameworkHostDestroyUnbindsPreviouslyHandedOffInterface`; its public Play Mode observability remains limited as described above.
- Actor Composition and ordinary Object Entry query regressions are outside the 21 C4 cases. Include `ActorCompositionRecompositionTests`, `PlayerActorCompositionAccessTests` and the full `StableObjectBindingTests` coverage in the Project Owner's Edit Mode validation. None of these source references imply that the tests ran for this delivery.

The public IObjectEntryRecognitionQuery and IObjectEntryOccurrenceQuery remain read-only. The C4 scenario obtains write authority only from the exact occurrence's ObjectEntryCompositionAuthoring.

## Restore and repeat

1. Exit Play Mode and restore the previous Active Game Application, Build Profile / Shared Scene List, and Scene Reload setting.
2. The generator reuses a complete valid Generated tree. If a generated tree is incomplete or conflicting, it stops without deleting files. After reviewing and backing up the generated data, remove only Assets/QA-IF-ADR-014/C4/Generated/ manually if a clean regeneration is required.
3. To repeat QA, re-run setup validation, select the C4 application and scenes, then enter a fresh Play Mode session.

## Evidence status

No Unity import, compile, Edit Mode or Play Mode run is claimed by this change. C4 Amendment 003 certification remains pending the Project Owner's C4 Play Mode transcript and the required C3 Unity validation. Do not use the C4 scenario result to promote the API to Stable or claim production readiness.

### Tracker evidence summary

- QAFramework baseline: `180f30895f66e194000112911fe9459d14b21d67`; Unity Editor: `6000.5.0f1`.
- Framework source baseline: `2bf0560b88e35168c159e52240e498e3cb363fcc`.
- Resolved Composition package: `0.1.0-preview.3`, package-lock hash `ef577765d0b565a020f3e98f6e5eaae681333c29`.
- C4 source defines 21 Play Mode cases; C4 Play Mode execution remains pending Project Owner logs. The separate 566/566 C3 Edit Mode screenshot has already been supplied, without an NUnit XML report or execution SHA. Preserve the Unity version, QA/Framework commits, resolved package hash, full case output and Edit Mode counts with the eventual certification record.
