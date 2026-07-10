# P2A-QA0 PlayerComposer Smoke Report

## Objective

Add a dedicated Editor regression smoke for the public `PlayerComposer` product surface before P2B changes its configuration or materialization.

## Cases implemented

- valid `Validate`;
- first Apply/Rebuild;
- idempotent second Apply/Rebuild;
- PlayerRecipe defaults;
- preservation of typed PlayerInput and concrete camera targets;
- PlayerActor/PlayerSlot declaration materialization;
- UnityPlayerInputGateAdapter materialization;
- `_Framework/_Bindings` materialization;
- F52 target materialization;
- missing PlayerInput;
- missing action asset;
- missing action map;
- deterministic stable diagnostics;
- identity/reference stability after GameObject rename;
- preexisting Gate adapter with missing `sourceSlot`;
- duplicate PlayerActor/PlayerSlot owners and F52 targets outside their canonical owners/root.

## Execution result

Not executed in this documentation/implementation session.

```text
Cases executed: 0
Cases PASS: 0
Known gaps reproduced: pending manual Unity execution
Final result: PENDING UNITY COMPILE/IMPORT AND MANUAL SMOKE
```

No operational PASS is claimed.

## Expected known-gap diagnostics

The runner reports these separately from baseline failures:

- `KnownGapGateSourceSlotNotRepaired`: a preexisting Gate adapter with `sourceSlot = null` remains without the typed reference after Apply/Rebuild.
- `KnownGapExternalDuplicateOwnersAndTargetsAccepted`: external PlayerActor/PlayerSlot owners and F52 targets remain alongside the canonical materialization, while Apply/Rebuild reports no blocker and silently ignores the external set.
- `KnownGapMissingActionAssetAccepted`: a PlayerInput without an InputActionAsset is accepted by PlayerComposer validation.

The smoke does not repair any of these gaps.

## Files created

- `Assets/ImmersiveFrameworkQA/Editor/PlayerAuthoring/QaP2APlayerComposerRegressionSmoke.cs`
- corresponding Unity `.meta` files;
- this report.

## Files altered

None.

## Blockers for P2B

P2B may start only after manual execution confirms:

- all baseline cases pass;
- the second Apply/Rebuild creates and repairs nothing;
- Recipe defaults preserve concrete references;
- GameObject rename does not change identity or resolution;
- all three known gaps are reproduced and logged explicitly.

## How to run

```text
Immersive Framework > QA > Player > P2A-QA0 PlayerComposer Regression Smoke
```

Expected final status while the known package gaps remain:

```text
status='SucceededWithKnownGaps'
baselinePassed='15'
baselineFailed='0'
gapsReproduced='3'
gapsMissing='0'
cases='18'
```

## Boundary

The smoke uses a transient Preview Scene and in-memory Recipe/InputAction assets. It uses no reflection, private Inspector calls, name lookup, package mutation, FIRSTGAME scene or persistent QA scene asset.
