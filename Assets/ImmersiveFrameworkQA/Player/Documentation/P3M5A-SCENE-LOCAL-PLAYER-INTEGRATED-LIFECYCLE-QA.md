# P3M5A — Scene Local Player Integrated Activity Lifecycle QA

## Objective

Prove the `Scene Local Player Admission` product surface through the real
`FrameworkRuntimeHost` Activity request path.

The smoke does not invoke the composite participant directly.

```text
FrameworkRuntimeHost.RequestActivityAsync
  -> Activity scene composition
  -> SceneLocalPlayerAdmissionRuntimeHostModule scene binding
  -> Scene Local Player admission
  -> Actor Profile selection
  -> ExternalSceneOwned Actor adoption
  -> canonical Activity Player Actor lifecycle
  -> Activity readiness
```

## Scope

```text
real additive Activity scene from the Build Profile
real OnActivityEnter surface discovery
deterministic loaded-scene reconciliation before every Activity resolution
exact configured Slot admission
Host commit
Actor selection
ExternalSceneOwned adoption
LogicalActorsPrepared readiness
same-Route exit to a no-Player Activity
reverse release and RuntimeContent cleanup
same-Route re-entry with new typed identities
stale adoption-token rejection
GameplayReady negative that reaches an explicit canonical Player admission boundary
rejected-target residue and scene-release proof
original Activity restoration after the smoke
```

## Out of scope

```text
Route-to-Route transition
multi-Slot ordering and compensation matrix
duplicate Slot/Host/Actor authoring negatives
complete GameplayReady fixture
FIRSTGAME integration
```

These remain in P3M5B or P3M6.

## Setup

Outside Play Mode:

```text
Immersive Framework
  > QA
    > Player
      > P3M5A Apply Scene Local Player Integrated Lifecycle Fixture
```

The setup is idempotent. It creates or updates:

```text
Assets/ImmersiveFrameworkQA/Player/P3M5A/
  P3M5A_SceneLocalPlayerActivity.unity
  P3M5A_SceneLogicalPlayerActor.prefab
  P3M5A_SceneActorProfile.asset
  P3M5A_ActivityContent.asset
  P3M5A_ExplicitSlotProjection.asset
  P3M5A_LogicalActorsPrepared.asset
  P3M5A_GameplayReady.asset
  P3M5A_NoPlayersProjection.asset
  P3M5A_NoPlayersRequirements.asset
  P3M5A_ScenePlayerPreparedActivity.asset
  P3M5A_ScenePlayerGameplayReadyActivity.asset
  P3M5A_NoPlayersActivity.asset
```

The Activity scene is enabled in `EditorBuildSettings.scenes`.

Transition policy is explicit:

```text
LogicalActorsPrepared target -> Seamless
GameplayReady target with scene side effects -> FadeWithLoading
No Players target -> Seamless
```

The `GameplayReady` target uses visual occlusion because it declares scene side effects.
The expected negative may stop at either canonical boundary:

```text
same-Route Player admission preflight
  -> RejectedCurrentGameplayNotReady when the previous Activity has no current gameplay

or

canonical Activity Player enter
  -> explicit gameplay-authoring/readiness failure after target preparation
```

Both paths are typed Player-pipeline rejections. Transition-policy rejection, missing
configuration before Player admission, silent success, or retained target residue remain
invalid.

## Runtime smoke

Enter a fresh normal Framework Play Mode session. After boot completes, run:

```text
Immersive Framework
  > QA
    > Player
      > P3M5A Run Scene Local Player Integrated Lifecycle Smoke
```

Expected:

```text
[P3M5A_SCENE_LOCAL_PLAYER_INTEGRATED_LIFECYCLE_SMOKE]
status='Passed'
cases='17'
```

Expected cases:

```text
play-mode-required
fixture-assets-resolved
official-runtime-authorities-ready
session-initially-clean
real-activity-request-entered
activity-scene-surface-bound
slot-host-selection-committed
external-actor-adopted-canonically
activity-runtime-owner-authoritative
same-route-exit-completed
reverse-release-left-no-residue
reentry-created-new-identities
stale-reentry-token-rejected
second-exit-clean
gameplay-ready-canonical-boundary-rejected
gameplay-ready-rejection-left-no-residue
failed-target-scene-released
```

## Acceptance criteria

```text
compiles
setup is idempotent
positive Activity request succeeds
Scene Local Player becomes Joined and selected
external Actor is prepared without duplication
ownership is ExternalSceneOwned
exit returns Slot to non-Joined and clears selection
preparation and adoption tokens are removed
released Activity RuntimeContent root is removed
re-entry reconciles the newly loaded scene surface before lifecycle execution
re-entry generates new preparation/adoption identities
stale token cannot release the current adoption
bare GameplayReady target fails at an explicit canonical Player admission boundary
same-Route preflight rejection is valid when the previous Activity has no current GameplayReady owner
later canonical Activity enter failure remains valid when target preparation is reached
rejected target leaves no Scene, Slot, selection, preparation or adoption residue
smoke restores the original Activity before reporting PASS
```


## Re-entry diagnostics

If a loaded Activity scene does not reach active admission within the smoke window,
the failure now reports the scene load state, discovered surface count, bound surface
count, active admission count, current Activity readiness, lifecycle diagnostics, and
the surface's latest admission/adoption results. The smoke does not convert a
`NotReady` Activity into a timeout-only diagnosis.
