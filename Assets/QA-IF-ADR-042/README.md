# IF-ADR-042 SharedGroup Certification

This QA certifies the public SharedGroup contract using one ManagerProvisioned Player Session, two QA-owned keyboard devices, one shared Camera Output, and one stable SharedGroup Assignment occurrence.

## Setup and run

1. In Unity, run **Immersive Framework → QA → IF-ADR-042 → Configure SharedGroup Certification**.
2. Run Configure a second time. Both runs must complete with `setup='Configured' baseline='Valid'`; this checks that the generated assets, scene bindings, and Build Settings remain idempotent.
3. Select `GameApplication_QaIfAdr042` in `ImmersiveFrameworkSettings` and open `Assets/QA-IF-ADR-042/Scenes/QA_IF_ADR_042_Persistent.unity`.
4. Start a fresh Play Mode session and wait for the terminal `[IF-ADR-042-SHARED-GROUP]` line.

The expected terminal is `verdict='PASS'` and `cleanup='BaselineRestored'`. `BLOCKED` means an authored/runtime precondition prevented the contract from being exercised; `FAIL` means an observed contract divergence. The terminal includes the first causal issue and cleanup result.

The Manager-Provisioned endpoint and Scenario live in Persistent Content. The Activity-scoped `PlayerSessionObserver` is authored in the generated Activity content scene, where the Framework exposes Activity-scoped Player Session access. The Scenario resolves that observer from the declared Activity content scene at runtime.

## Certified transitions

The scenario checks zero Players, `0 -> P1`, `P1 -> P1+P2`, public replacement of P1's Actor/Subject, `P1+P2 -> P2`, `P2 -> 0`, and `0 -> P1`. It validates Output/Assignment/occurrence identity, physical fallback and TargetGroup membership, current Observation uniqueness, and projected radius/weight. Cleanup uses public Leave/CloseJoining APIs and removes both QA-owned Keyboards.

No Framework code is changed by this fixture.
