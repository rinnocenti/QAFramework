# IF-READY-04-QA-01 — Activity Entry Readiness Foundation

## Objective

Prove one direct Activity entry with `ObserveOnly` readiness through the official runtime host and ports, without Player or gameplay dependencies.

## Scope

The Play Mode regression resolves the active `FrameworkRuntimeHost`, captures the current Route and Activity, creates a temporary scene-scoped required `ActivityReadinessParticipant` and `ActivityReadinessEvents`, then creates a runtime-only `ActivityAsset` with `ObserveOnly` policy. It proves that the direct request completes and makes the target Activity authoritative while the participant remains `Preparing`. `CompletePreparation()` then drives the public readiness event to `Ready`.

The temporary readiness participant lives in the Route Primary Scene and is therefore eligible for every Activity in that Route. When the target entered, cleanup clears it, observes one release, removes and destroys the temporary readiness surface, and only then restores the initial Activity. This prevents a second occurrence from being started during restoration.

Cleanup also supports pre-entry failures: if the target never becomes authoritative and the initial Activity remains active, it preserves that initial authority and may destroy the temporary Route-scoped surface only when the participant never started. If no Activity is authoritative, the same zero-start/zero-release evidence is accepted. Any third authority is rejected explicitly.

Request coordination races participant start against the typed request completion, and Ready propagation permits at most one Unity lifecycle frame after `CompletePreparation()`. It uses neither timeout nor polling. Target runtime-only `ScriptableObject` destruction is confirmed after Unity's deferred destruction frame. Execution, authority preparation, listener cleanup, surface destruction, authority restoration, and target asset destruction failures are reported separately.

## Out of scope

This foundation does not cover `WaitVisible`, `WaitCovered`, presentation ordering, failure, invalidation, cancellation, replacement interruption, recovery gates, Route startup Activity, startup flow, or persistent QA assets/scenes.

## Prerequisites

- Open `QAFramework` in Unity 6.5.0f1.
- Enter a fresh Play Mode session with Game Flow started.
- Ensure a current Route with its declared primary scene loaded.

## Run

`Immersive Framework > QA > Regressions > Game Flow > Run Activity Entry Readiness Foundation Regression`

Expected success evidence:

```text
[IF_READY_04_QA_FOUNDATION] status='Passed'
```

## Cases proved

- official host and Route/Activity ports are resolved;
- initial authority and Route primary scene are captured;
- temporary required readiness participant/events surface is created in the Route primary scene;
- runtime-only `ObserveOnly` Activity has no Player participation or content scene requirement;
- request completion occurs while the participant is still preparing;
- target Activity remains authoritative through the public readiness completion and `Ready` observation;
- participant release and listener removal occur before Route-scoped surface destruction;
- the temporary surface is absent before initial authority restoration, preventing participant reentry;
- target asset destruction occurs only after final authority restoration and is confirmed after one frame.

## Foundation boundary

This is a focused technical foundation, not presentation evidence instrumentation.

Next authorized cut: `IF-READY-04-QA-02 — Presentation Evidence Instrumentation`.
