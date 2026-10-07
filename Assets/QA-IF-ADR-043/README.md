# IF-ADR-043 — Physical Camera Participation

## Scope

This Scenario certifies the physical participation of two Individual Player Camera Outputs through the real `GameApplication`, ManagerProvisioned Player Session, public Join/Leave APIs and public `CameraOutputAuthoring` state. It observes only physical Unity Camera enablement, `PlayerInput.camera`, active Assignment identity, Slot-to-Output identity and continuous physical Output participation through direct Unity Camera enablement/active-hierarchy observation.

The QA keyboards exist only to make Input System pairing deterministic. Action asset identity, gameplay input behavior and control-scheme behavior are not certification assertions. No Framework package files are changed.

## Generated baseline

Run **Immersive Framework > QA > IF-ADR-043 > Configure Physical Participation Certification**. The Editor setup is an authoring operation: it regenerates and validates the Route, Persistent scene, Game Application, two ordered Slot Profiles, ManagerProvisioned Host, two Output prefabs and IndividualPerPlayer Assignment. It preserves existing asset paths/GUIDs, validates saved scene bindings and Build Settings, and is expected to be run twice to check idempotency.

The ManagerProvisioned composition follows the `planet-devourer` samples: one persistent `PlayerInputManager`, manual Join, C# join notifications, an exact two-Player capacity, one shared Local Player Host prefab, `PlayerInput`, and explicit Local Player Provisioning registration. The setup materializes the same Host prefab on the manager and disables Unity split-screen so the test observes Camera Output participation without also testing viewport layout. `CharacterSelectionMultiplayerSplitScreen` supplies the two-Slot/Output mapping pattern; this QA does not test character selection or viewport layout.

Select `Assets/QA-IF-ADR-043/Settings/GameApplication_QaIfAdr043.asset` in `ImmersiveFrameworkSettings` without changing the project setting through the setup menu. Then open `Assets/QA-IF-ADR-043/Scenes/QA_IF_ADR_043_Persistent.unity` and enter a fresh Play Mode session.

## Ordered cases

The scenario certifies exactly these seven checkpoints:

1. `0` — fresh zero-Player baseline.
2. `P1` — public Join for P1.
3. `P1+P2` — public Join for P2.
4. `P2` — public Leave for P1.
5. `0` — public Leave for P2.
6. `P1` — public Join for P1 after zero.
7. `P1+P2` — public Join for P2 after P1 rejoin.

Each checkpoint checks exact public Session membership, active Assignment identity on both Outputs, each Output's authored Slot/Output identity, expected physical Camera enablement and `PlayerInput.camera` for every Joined Slot. At zero Players exactly one physical Output Camera must be enabled; with Players Joined, only the mapped Output Cameras may be enabled. The Scenario samples the two registered QA Output Cameras every frame and blocks PASS if no Output is physically participating (`Camera.enabled && activeInHierarchy`). Render-pipeline callbacks are intentionally not part of this contract.

Each Join pairs the profile's own QA-created virtual Keyboard with the required `QA.Keyboard` scheme. Cleanup issues public Leaves until no Slot is Joined, waits for the zero-Player camera baseline, then removes both virtual keyboards. If public Leaves cannot release the Players, keyboards remain paired and the terminal disposition is `FreshBootRequired`.

Invalid authoring, missing runtime readiness, an invalid zero-Player baseline or unavailable QA input infrastructure yields `BLOCKED`; a contract divergence after the baseline is established yields `FAIL`. Cleanup diagnostics are reported separately from the first causal divergence.

## Terminal evidence

The single terminal line has the form:

```text
[IF-ADR-043] status='Passed|Failed|Blocked' verdict='PASS|FAIL|BLOCKED' cases='x/7' next='...' completed='x' missing='...' assignment='...' framesObserved='x' framesWithoutCamera='x' firstFrameWithoutCamera='...' execution='...' unwind='public-leave-requests' cleanup='BaselineRestored|FreshBootRequired' cleanupIssue='...'.
```

`PASS` requires 7/7 checkpoints, no observed frame without a physically participating QA Output, public Leave cleanup and `BaselineRestored`. A blocked setup is not a functional failure. Send the complete terminal line and preceding causal diagnostics for review.
