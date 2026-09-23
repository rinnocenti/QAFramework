# Player QA architecture

## Canonical boundaries

| Area | Owner | Primary fixture or regression |
|---|---|---|
| Player Session | Player | `QaPlayerSessionContractRegression`, `QaPlayerParticipationAuthoringRegression` |
| Scene-Provided Host | Player | `QaP3M5BRouteTransitionAndNegativeMatrixSetup` and smoke |
| Manager-Provisioned Host | Player | `QaManagerProvisionedPlayerFixture`, bridge contract and lifecycle regressions |
| Actor Selection / Lifecycle | Player | Actor selection binding and gameplay admission regressions |
| Scoped Consumer Access | Public Surface | `QaPlayerSurfacePublicNavigationFixture` and Q1/Q2 regressions |
| Activity Participation | Game Flow | `QaM07*` and participant-aware regressions |
| Public Player Surface | Player/Public Surface | public fixture, positive contract and negative contract |

The source filenames retain some historical P3/M07 identifiers where changing a
Unity-owned source path would only add import risk. Those identifiers are not
operational architecture: canonical menu paths and fixture APIs use the areas
above.

## Canonical operation

Normal operation starts at `Immersive Framework/QA/Player/Run Full Player QA`.
`QaPlayerFullCertificationOrchestrator` is the sole Player QA master state
owner. It coordinates, without reimplementing, the following isolated phases:

1. serialization identity and Session contract (Edit Mode);
2. Scene-Provided lifecycle, Leave and Session-termination proofs;
3. Manager-Provisioned lifecycle, no-Activity and Session-termination proofs;
4. Actor lifecycle binding and public-surface positive/negative proofs;
5. failed first SceneProvided adoption rollback;
6. failed contextual reprojection rollback after physical commit;
7. normal A -> B without physical candidate/handoff.

The master uses its own `SessionState` only for phase/result continuity across
Play Mode transitions. Fixture references remain owned by their typed setup;
notably, Public Surface preparation receives its Player Session through the
Manager-Provisioned typed context. Individual menus remain advanced diagnostic
entrypoints.

## Mandatory lifecycle matrix

`QaPlayerFullCertificationOrchestrator.MandatoryContracts` is the executable
25-row authority for certification. Every entry carries `ContractId`,
description, owning QA case, mandatory flag and result key; the final verdict
requires exactly 25 mandatory contracts, all executed and all `PASS`. The three
explicit rollback/transition rows are 14 (failed first SceneProvided adoption),
15 (failed contextual reprojection) and 25 (no physical handoff on normal
A -> B). A phase count alone is not a certification condition.

## Audit classification

| Source group | Classification | Current role |
|---|---|---|
| `QaPlayerSessionContractRegression`, `QaPlayerParticipationAuthoringRegression` | canonical | Player Session contract |
| `QaManagerProvisionedPlayerFixture`, `QaManagerProvisionedLifecyclePublicContractRegression` | canonical | Manager-Provisioned fixture and contract |
| `QaP3G3ProvisioningBridgeSyntheticSmoke` | internal-contract regression | Advanced bridge evidence; not a fixture dependency |
| `QaPlayerGameplayAdmissionRegression`, `QaPlayerActorSelectionRuntimeBindingRegression` | canonical | Actor selection and lifecycle |
| `QaP3M5B*` | canonical Scene-Provided fixture/support | Scene host, route/activity lifetime and release |
| `QaP3M5A*`, `QaP3H4*`, `QaP3J5*`, `QaP3J6*` | useful but historical/advanced | retained fixture diagnostics, not primary entrypoints |
| `GameFlow/InternalEditor/QaPlayerSurfacePublicNavigation*`, Q1/Q2 and certification orchestrator | cross-domain integration regression | Public Player commands and scoped access exercised through the internal Activity lifecycle harness |
| `QaM07*`, `QaParticipantAware*` | cross-domain integration regression | Game Flow/Activity participation using Player as input |
| `QaM07CloseGateRegression` | obsolete duplicate | removed: reflective mega-orchestrator |
| Hub Player Surface runtime fixtures | fixture/support | composition-bound Route, Activity and UIGlobal surfaces |
| Historical P3/M07 and pre-R2 documents | historical/obsolete | traceability only; not operational instructions |

## Fixture ownership

`QaPlayerSessionQaSupport` is the single Player Session support layer. It lives
in the existing `ImmersiveFrameworkQA.Player.Internal.Editor` assembly, resolves
and validates `PlayerSessionProfile`, exposes ordered Supported Slots, and owns
the serialized `PlayerInputManager` bridge derived from `SupportedSlotCount`.
Game Flow consumes this explicit Player support but never authors or mutates the
Player fixture.

`QaManagerProvisionedPlayerFixture.PrepareAndValidate()` owns the persisted
Manager-Provisioned composition: profile, active Game Application, Input System
manager, host prefab, provisioning authoring, registration and actor-selection
authoring. It returns `QaManagerProvisionedPlayerFixtureContext`, whose
references are the sole handoff to Public Surface preparation. It lives in the
existing Player Internal Editor assembly. Public Surface and Game Flow
Participation invoke this typed fixture directly; neither invokes a menu,
reflection, global rediscovery of the Player Session, nor assumes a previous
test left the fixture prepared.

`QaP3M5BRouteTransitionAndNegativeMatrixSetup` owns Scene-Provided fixture
assets and scenes. It does not use `PlayerInputManager` as Scene-Provided
evidence.

## Activity/Game Flow classification

`QaM07ActiveProjectionFreezeRegression`,
`QaM07ActivitySessionLifecycleProjectionRegression`,
`QaM07PlayerRequirementPolicyMatrixRegression`,
`QaM07PlayerZeroParticipantPolicyMatrixRegression` and
`QaManagerProvisionedLifecycleWaitingProjectionRegression` remain in GameFlow
because their primary
contract is Activity participation, readiness, occurrence or projection.
They validate the Manager-Provisioned Player fixture only when Player is a
participant; they no longer provide infrastructure for Public Surface QA.

The former M07 internal reconcile and included/excluded release-scope
regressions were retired: both depended on per-Activity physical Actor release
or internal semantic reflection, which is not a contract of the Session-owned
physical Player model.

## Public Surface isolation

The public certification prepares its navigation fixture after the explicit
Manager-Provisioned Player context is available. Its source remains in
`GameFlow/InternalEditor` because Q1/Q2 exercise Player public commands through
the internal Activity lifecycle/readiness harness, whose result types are
internal to that assembly; moving only these sources would leak those internals
across an assembly boundary. Q1/Q2 no longer require
`QaM07InternalReconcileSetup`, its menu, or its `SessionState` prepared marker.
The certification keeps `SessionState` only for its explicit two-fresh-Play-Mode
phase coordinator.

## Historical / obsolete

P3 and M07 labels in source names and historical documentation are retained
only as traceability. Capacity, `PlayerProvisioningProfile`, Slot provisioning
overrides and the former `GameApplicationAsset` local-slot API are not active
QA contracts.

## Certified baseline — 2026-08-09

The canonical one-button flow completed successfully in Unity:

```text
[QA_PLAYER_FULL] status='Passed' verdict='PLAYER QA CERTIFIED' session='PASS' sceneProvided='PASS' managerProvisioned='PASS' actor='PASS' publicSurface='PASS' participation='PASS'.
```

Focused evidence observed in the same certification run:

| Proof | Result |
|---|---|
| Player Participation Authoring | PASS — 7 cases |
| Scene-Provided Route Transition / Negative Matrix | PASS — 25 cases |
| Manager-Provisioned Lifecycle Public Contract | PASS — 9 cases |
| Manager-Provisioned Waiting Projection | PASS — 14 cases |
| Actor Selection Runtime Binding | PASS — 13 cases |
| Player Gameplay Admission | PASS — 114 cases |
| Public Surface Q1 | PASS — 28 cases |
| Public Surface Q2 | PASS — 36 cases |
| Activity Session Projection | PASS — 30 cases |

Manager-Provisioned preparation reported `supportedSlots='2'` and
`maxPlayers='2'`, confirming the serialized `PlayerInputManager` bridge matched
`PlayerSessionProfile.SupportedSlotCount`. Scene-Provided preparation reported
`hostProvisioning='SceneProvided'` with two Supported Slots, proving the two
provisioning models were prepared independently before their Play Mode phases.

Q2 intentionally emits framework error diagnostics for rejected, stale,
wrong-scope, destroyed-binding and unbound-trigger cases. Those expected error
logs are negative-case evidence; Q2 still completed 36 cases with `status='Passed'`.

See `PLAYER-QA-CERTIFICATION-2026-08-09.md` for the retained certification
record. Normal operation remains the single `Run Full Player QA` entrypoint;
focused menus are for diagnosis.
