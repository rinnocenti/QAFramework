# IF-READY-04-QA-02 — Presentation Evidence Instrumentation

## Objective

Provide passive, bounded presentation evidence for the existing QA Loading surface and official Transition curtain, ready for later Activity Entry Readiness policy regressions.

## Scope

`QaLoadingSurfaceVisibilityHoldAdapter` records request, visual application, and typed-result evidence with frame and realtime ordering. Its history is bounded to 256 entries and reset never changes visual state.

`QaTransitionPresentationEvidenceObserver` is a temporary passive observer bound to the official `UnityFadeCurtainEffectAdapter`. It samples only visual state changes from `LateUpdate`, records a bounded history of 128 entries, and never implements or invokes a Transition adapter contract.

`LateUpdate` entries are best-effort frame observations: a transient `Transitioning`
sample is diagnostic and is not guaranteed on every frame cadence. Deterministic settled
evidence is captured as observer checkpoints only after the official adapter's async
result succeeds. The regression requires at least one passive `StateChanged` entry, but
uses those checkpoints—not transient sampling—to prove visible and hidden settled states.
No timeout or polling is used.

## Out of scope

This cut does not prove `WaitVisible`, `WaitCovered`, readiness failure, interruption, recovery gates, or Loading/Transition behavior changes. It adds no scene, prefab, Route, Activity, or package modification.

## Why these owners

Loading evidence belongs in the existing QA-owned adapter because it observes that adapter's own public requests and applications. Transition remains executed exclusively by the official package adapter; the QA observer is intentionally not an adapter, decorator, or visual implementation.

## Run

The regression starts from the canonical QA Hub Game Application. It does not use
`QA_TransitionGameApplication`, and `QA_UIGlobal` does not need to remain loaded.
No persistent presentation prefab is required: the regression creates minimal Transition
and Loading surfaces in runtime memory, configures them before activation, and saves no
scene or asset.

1. Exit Play Mode.
2. Run `Immersive Framework > QA > Setup > Activity Entry Readiness > Prepare Presentation Evidence Regression`.
3. Enter a fresh Play Mode session.
4. Run:

`Immersive Framework > QA > Regressions > Game Flow > Run Activity Entry Presentation Evidence Regression`

5. Confirm the PASS log.
6. Exit Play Mode. The setup automatically restores Standard QA Hub.

For an emergency/manual restore, run:

`Immersive Framework > QA > Setup > Activity Entry Readiness > Restore Standard QA Hub`

Expected success evidence:

```text
[IF_READY_04_QA_PRESENTATION_EVIDENCE] status='Passed'
```

The setup only standardizes canonical QA Hub configuration. The regression creates one
temporary root in the current Route Primary Scene, exercises the official
`UnityFadeCurtainEffectAdapter` and QA `QaLoadingSurfaceVisibilityHoldAdapter` directly
with valid synthetic requests, verifies unchanged GameFlow authority, resets Loading
evidence, and destroys every runtime object before PASS. The setup removes hidden manual
Project Settings dependencies; Restore Standard QA Hub remains the emergency recovery action.

## Next authorized cut

`IF-READY-04-QA-03 — Direct Activity Readiness Policies`
