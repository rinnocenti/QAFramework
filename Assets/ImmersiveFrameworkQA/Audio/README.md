# Audio QA Harness

Last updated: **2026-08-24**

Audio is one QA feature with one setup, Hub entry, primary scene and consolidated panel.

```text
1. Immersive Framework > QA > Setup > Audio > Configure Audio QA
2. Open Assets/ImmersiveFrameworkQA/Hub/Scenes/QA_Hub.unity
3. Enter Play Mode
4. Open Audio QA
5. Run All Audio QA
6. Inspect the consolidated PASS / FAIL output
```

The setup is idempotent and configures generated clips, Core Audio fixtures, Framework BGM fixtures, Route/Activity lifecycle fixtures and Hub entry.

## Internal groups

```text
Core Audio
  com.immersive.audio provider proof

Framework BGM
  Route/Activity BGM intent and lifecycle proof

ADR-013A
  provider-confirmed execution / rejection semantics

Audio continuity
  physical same/different cue and stop-transition behavior
```

The Framework BGM fixture uses official components only:

```text
FrameworkBgmDirector
FrameworkRouteBgmBinding
FrameworkActivityBgmBinding
FrameworkBgmRoutePolicy
FrameworkBgmActivityPolicy
RouteRequestTrigger
ActivityRequestTrigger
```

It does not use FIRSTGAME assets or consumer-specific BGM authority.


## Generated / maintained QA assets

The Audio setup creates or refreshes the canonical QA fixtures, including:

```text
Assets/ImmersiveFrameworkQA/Audio/Scenes/QA_Audio.unity
Assets/ImmersiveFrameworkQA/Audio/Scenes/QA_AudioRouteB.unity
Assets/ImmersiveFrameworkQA/Audio/ScriptableObjects/QA_AudioDefaults.asset
Assets/ImmersiveFrameworkQA/Audio/ScriptableObjects/QA_SfxCue.asset
Assets/ImmersiveFrameworkQA/Audio/ScriptableObjects/QA_SfxCue_Pooled.asset
Assets/ImmersiveFrameworkQA/Audio/ScriptableObjects/QA_SfxCue_MissingClip.asset
Assets/ImmersiveFrameworkQA/Audio/ScriptableObjects/QA_AudioSfxPool.asset
Assets/ImmersiveFrameworkQA/Audio/ScriptableObjects/QA_BgmCue.asset
Assets/ImmersiveFrameworkQA/Audio/Routes/QA_FrameworkBgmRoute.asset
Assets/ImmersiveFrameworkQA/Audio/Routes/QA_FrameworkBgmRouteB.asset
Assets/ImmersiveFrameworkQA/Audio/Activities/QA_FrameworkBgmStartupActivity.asset
Assets/ImmersiveFrameworkQA/Audio/Activities/QA_FrameworkBgmOwnActivity.asset
Assets/ImmersiveFrameworkQA/Audio/Activities/QA_FrameworkBgmRetainPreviousActivity.asset
Assets/ImmersiveFrameworkQA/Audio/Activities/QA_FrameworkBgmRouteFallbackActivity.asset
Assets/ImmersiveFrameworkQA/Audio/Activities/QA_FrameworkBgmSilenceActivity.asset
Assets/ImmersiveFrameworkQA/Audio/ScriptableObjects/QA_FrameworkBgm_RouteCue.asset
Assets/ImmersiveFrameworkQA/Audio/ScriptableObjects/QA_FrameworkBgm_RouteBCue.asset
Assets/ImmersiveFrameworkQA/Audio/ScriptableObjects/QA_FrameworkBgm_StartupActivityCue.asset
Assets/ImmersiveFrameworkQA/Audio/ScriptableObjects/QA_FrameworkBgm_ActivityCue.asset
```

Synthetic clips/tones are QA assets only and are not final game content.

## Scene topology

`QA_Audio.unity` contains the Core Audio host/panel fixtures and the primary Framework BGM fixture. `QA_AudioRouteB.unity` is internal Route-B lifecycle infrastructure. Typical Framework BGM topology:

```text
QA_FrameworkBgmRoot_*
  QA_FrameworkBgm_AudioRuntimeHost
  FrameworkBgmDirector
  FrameworkRouteBgmBinding

QA_FrameworkBgm_Activity_*
  ActivityLocalVisibilityAdapter
  FrameworkActivityBgmBinding

QA_FrameworkBgmPanel_*
  FrameworkBgmQaPanel
  RouteRequestTrigger / ActivityRequestTrigger
```

## Panel operations

The panel retains the normal Core Audio operations (`Compose Runtime Host`, direct/pooled SFX smokes, missing-authority smokes, Listener smoke, BGM play/stop) plus consolidated `Run All Audio QA` execution. Framework BGM cases are reported through the same consolidated result.

## Current BGM ownership model

```text
FrameworkRouteBgmBinding
  Route intent only

FrameworkActivityBgmBinding
  Activity intent only

ActivityFlowRuntime
  deterministic Activity entry completion

FrameworkBgmDirector
  persistent intent/presentation authority
```

There is no Route -> Startup Activity BGM binding reference.

When a Route has a Startup Activity:

```text
Route intent
  -> may remain pending

Activity entry
  -> Activity binding publishes its own intent if authored

Activity entry completion
  -> persistent Director receives completion through explicit runtime wiring

Activity intent published
  -> Activity wins; no transient Route playback

no Activity intent
  -> pending Route intent resolves
```

The completion path must work with:

```text
ActivityContentProfile = null
activityContentHandles = 0
no FrameworkActivityBgmBinding
```

## Core Audio smokes

`Run All Audio QA` covers:

- direct SFX success;
- missing clip failure;
- missing defaults failure;
- pooled SFX success;
- missing pool failure;
- listener duplicate/report-only behavior;
- direct BGM play/stop.

Negative outcomes are expected QA evidence, not suite failures when the expected typed result is produced.

## Framework BGM matrix

The current suite validates:

```text
Route
  PlayOwn
  PreserveCurrent
  Silence

Activity
  UseRoute
  UseOwnOrRoute
  UseOwnOrPreserveCurrent
  Silence
  own cue wins where authored

Continuity
  same cue -> NoChange
  owner exit -> Preserve
  explicit Silence sticky
  rejected provider operation preserves confirmed state
```

## Startup Activity isolation

The Startup Activity proof is deliberately isolated from prior confirmed presentation state.

### `startup-activity-neutral-baseline`

Expected:

```text
confirmedBgm = null
confirmedExplicitSilence = false
provider stopped
```

### `startup-route-is-deferred`

Expected:

```text
RouteCue requested
outcome = NoChange
confirmedBgm = null
provider has no RouteCue presentation
```

This proves that Route intent is retained pending and does not physically flash before the Startup Activity resolves.

### `startup-activity-prevents-route-transient-play`

Expected:

```text
ActivityCue requested
outcome = Applied
confirmedBgm = ActivityCue
provider playing ActivityCue
```

The strong property being certified is:

> `RouteCue` is never physically presented before `ActivityCue`.

The suite must not weaken this to accepting a polluted prior confirmed state.

## Real lifecycle empty-Activity regression

The QA topology also covers the regression that originally exposed the lifecycle gap:

```text
Route
  PlayOwn = RouteMusic

Startup Activity
  ActivityContentProfile = null
  no Activity BGM binding
  activityContentHandles = 0
```

Expected:

```text
Route intent deferred
Activity entry completes
completion reaches FrameworkBgmDirector
no Activity intent was published
pending Route intent applied
RouteMusic confirmed
```

Completion must not depend on Activity content existing.

## Provider-confirmed execution — ADR-013A

The suite proves:

```text
Play success
  -> Applied
  -> requested presentation becomes confirmed

Play rejection
  -> Rejected
  -> previous confirmed presentation preserved
  -> request remains retryable

Stop success
  -> Released
  -> explicit silence confirmed

Stop rejection
  -> Rejected
  -> previous confirmed presentation preserved

missing optional AudioRuntimeHost
  -> OptionalAuthorityUnavailable
  -> no confirmed presentation fabricated
```

The expected error log for the intentionally missing `AudioRuntimeHost` fixture is part of this negative test and is followed by a PASS when `OptionalAuthorityUnavailable` is returned correctly.

## Physical continuity

Current physical-provider checks:

```text
same-cue-no-restart
  source position/volume preserved synchronously

different-cue-no-abrupt-cut
  previous source remains active while controlled transition begins

different-cue-transition-completes
  requested second cue becomes active and audible

explicit-stop-fades-to-silence
  source continues during fade and then reaches stopped/null presentation
```

## Current certified result

Canonical Play Mode run after Startup Activity lifecycle/wiring correction:

```text
Core Audio         7/7 PASS
Framework BGM     28/28 PASS
ADR-013A            5/5 PASS
Audio continuity    4/4 PASS
TOTAL              44/44 PASS
FAILED               0
```

Important focused PASS lines include:

```text
startup-activity-neutral-baseline
startup-route-is-deferred
startup-activity-prevents-route-transient-play
```

The historical `30/30` run remains valid evidence for the earlier BGM-CONTINUITY-1 boundary; it is not relabeled as certification of this later lifecycle cut.

## What this QA proves

- Core provider configuration and typed failures remain explicit.
- Route and Activity BGM bindings publish independent intents.
- Activity BGM works without a Route BGM binding.
- Route `PlayOwn` resolves after a Startup Activity with zero content/BGM intent.
- Activity own BGM wins over pending Route intent without transient Route playback.
- `UseRoute` inherits the complete Route intent.
- No Request preserves confirmed presentation.
- Route/Activity owner exit does not restore or stop BGM automatically.
- explicit Silence is sticky until a later Play succeeds.
- same confirmed cue does not restart playback.
- provider rejection does not corrupt confirmed Framework state.
- Activity entry completion reaches the persistent Director by explicit runtime wiring.

## What this QA does not prove

- AudioMixer routing beyond current metadata/resolution contracts;
- final game UI;
- every consumer/sample topology;
- unrelated Player, Camera or Game Flow behavior.

Consumer usability remains demonstrated in Samples/FIRSTGAME; QA remains the technical certification surface.
