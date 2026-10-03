# QA-NEW-005 — Audio/BGM Continuity

**Status: NOT RUN.** This certification has not been imported or executed in Unity.

## Scope

QA-NEW-005 certifies the current Framework Audio/BGM integration after IF-ADR-040: Session-scoped `FrameworkBgmDirector` composition, Route/Activity authoring binding and release, sticky confirmed presentation, public Route/Activity request flow, and startup Activity resolution. It observes only public Framework state and operation results.

It does not certify physical fade/crossfade behavior, provider internals, same-scope `Available` idempotence, authority conflicts, partial rollback, or isolated binder behavior. The last four composition contracts belong to the existing Framework EditMode composition tests. No result here establishes physical continuity beyond the provider-confirmed logical state exposed by the Framework.

## Topology

Run the Editor setup command **Immersive Framework > QA > QA-NEW-005 > Configure Audio BGM Continuity**. It creates `Assets/QA-NEW-005/Scenes`, `Settings`, `Scripts`, explicit audio cues and a tone clip. The generated `GameApplication_QaNew005` uses a neutral startup Route and a dedicated Persistent Content scene containing one `AudioRuntimeHost`, one `FrameworkBgmDirector`, public Route/Activity request triggers, and the scenario runner.

Routes are neutral, PlayOwn, PreserveCurrent, Silence, Play-after-Silence, Startup Activity with BGM, and Startup Activity without content. Activities cover own cue, no new intent, UseRoute, and Startup Activity with its own cue. Route/Activity intent stays on the corresponding scene authoring components; cue references are not stored on Route/Activity assets.

Each Route authoring is a child of a `RouteContentContribution` in that Route's primary scene, with the contribution referencing the exact owning Route asset. Each Activity authoring is a child of an `ActivityContentContribution` in an additive scene declared by that Activity's `ActivityContentProfile`, with the contribution referencing the exact owning Activity asset. The authoring is a `RouteContentBehaviour` / `ActivityContentBehaviour` lifecycle receiver; merely placing it in a loaded scene is insufficient. Session SceneLifecycle injects the director separately from Route/Activity content entry dispatch.

The setup adds generated scenes to Build Settings. It does not change the project’s active Game Application. Select `GameApplication_QaNew005` in the active `ImmersiveFrameworkSettings` before entering Play Mode.

## Cases

The scenario has 12 terminal cases, in causal order:

1. Route `PlayOwn` binds and confirms its cue.
2. Route A→B→A proves B's no-request policy preserves the cue, releases A's previous binding, and attaches a fresh A binding on reentry.
3. Activity own cue binds and is confirmed.
4. Exiting the own-cue Activity releases its authoring without an implicit stop.
5. The entering neutral Activity publishes no new intent and preserves the confirmed cue. Cases 4 and 5 use the same transition but assert distinct owner-release and neutral-entry contracts.
6. Activity `UseRoute` resolves the current Route cue.
7. Explicit Route Silence is provider-confirmed.
8. Silence remains sticky through owner exit and no request.
9. A later Play replaces explicit silence.
10. Startup Activity with its own cue ends with the Activity cue confirmed.
11. Contentless Startup Activity resolves the pending Route cue.
12. Cleanup restores neutral Route, releases temporary Route/Activity scenes, clears transient intents through explicit Silence, and confirms explicit silence before PASS.

The runner stops functional cases at the first divergence and still attempts cleanup. Cleanup failure is reported separately from the first functional divergence. The current neutral Route primary scene is the permitted final loaded Route; `loadedTemporaryScenes` lists only other authored Route and Activity scenes.

## Run procedure

1. Wait for Unity to finish importing the QA-NEW-005 scripts and generated assets without compile/import errors.
2. Run the Editor setup command above twice consecutively to verify idempotent regeneration of this dedicated QA fixture.
3. Select `GameApplication_QaNew005` in `ImmersiveFrameworkSettings`.
4. Open `Assets/QA-NEW-005/Scenes/QA_NEW_005_Persistent.unity` and enter a fresh Play Mode session.
5. Wait for the terminal line. Do not infer success from intermediate Audio logs.

Expected terminal format:

```text
[QA-NEW-005] status='Passed' verdict='PASS' cases='12/12' confirmedBgm='<...>' explicitSilence='true' routeDirectorAttached='true' routeContentEntered='true' routeContentExited='true' activityDirectorAttached='true' activityContentEntered='true' activityContentExited='true' routeLastOperationResult='<...>' activityLastOperationResult='<...>' directorLastOperationResult='<...>' routeProviderConfirmed='true' activityProviderConfirmed='true' loadedTemporaryScenes='<none>' routeConsumersBound='0' activityConsumersBound='0' consumersDestroyedWhileBound='0' cleanup='BaselineRestored' firstDivergence='' cleanupIssue=''
```

Failure uses `status='Failed' verdict='FAIL'`; `firstDivergence` identifies the first functional divergence and `cleanupIssue` reports any independent cleanup problem.

## Observability limits

`FrameworkBgmDirector` exposes the final confirmed cue, explicit-silence state, and last operation result. QA-NEW-005 uses those values and QA probes attached to the actual scene authorings to observe binding and detach. It does not infer physical playback position, audible output, fade completion, or crossfade shape from logical confirmation.

For Startup Activity with its own cue, the scenario verifies that the Route request completes with the Activity cue confirmed and that the public last operation result is coherent. It cannot prove that no frame transiently played the pending Route cue because the current public surface does not expose a durable operation sequence or physical playback history. No production instrumentation is added for this certification.

## Historical 44/44

The former Audio QA harness and its Hub/assets were removed; QA-NEW-005 is a new focused certification, not a restoration or case-for-case port. The historical 44/44 remains dated evidence for the pre-IF-ADR-040 implementation. QA-NEW-005 does not claim equivalence to those 44 cases and must not relabel that result. The IF-ADR-013 rerun gate remains open until this fixture is actually imported and executed successfully.
