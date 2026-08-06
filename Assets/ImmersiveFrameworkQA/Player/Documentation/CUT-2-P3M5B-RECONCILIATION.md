# CUT-2 — P3M5B persisted fixture reconciliation

## Type

QA technical integration.

## Objective

Make the P3M5B setup produce a Play Mode-ready persisted scene matrix in one canonical command.

## Root cause

The original generator assigned `SceneLocalPlayerAdmissionAuthoring.sceneLogicalPlayerActor` to a stripped component on a prefab instance. In the affected Unity persistence state, reopening the scene could turn that component reference into Unity fake-null even while the YAML retained a fileID.

A separate repair menu already knew how to unpack the Actor, rebind the references, save and reopen the scenes. The normal fixture Apply did not execute that repair, so reapplying the fixture restored the invalid persistence shape.

## Scope

- Add one canonical P3M5B Apply command.
- Execute existing generation and persisted-reference repair as one workflow.
- Reopen and validate all seven scenes before declaring the fixture ready.
- Preserve the exact negative shapes for duplicate Slot, missing Actor, mismatched evidence and reused Host.

## Out of scope

- No framework package runtime changes.
- No Activity transaction changes.
- No Player admission contract changes.
- No FIRSTGAME changes.
- No change to the P3M5B Play Mode smoke assertions.

## Files created

```text
Assets/ImmersiveFrameworkQA/Player/Editor/QaP3M5BReconciledFixtureSetup.cs
Assets/ImmersiveFrameworkQA/Player/Editor/QaP3M5BPersistedFixturePreflight.cs
Assets/ImmersiveFrameworkQA/Player/Documentation/CUT-2-P3M5B-RECONCILIATION.md
```

## Files altered or removed

None.

## Canonical use

Outside Play Mode:

```text
Immersive Framework
> QA
> Player
> P3M5B Apply Reconciled Route Transition Fixture
```

Expected terminal log:

```text
[P3M5B_RECONCILED_FIXTURE]
status='Applied'
generation='Passed'
persistedRepair='Passed'
postSavePreflight='Passed'
readyForPlayMode='True'
```

Then enter a fresh Play Mode session and run:

```text
Immersive Framework
> QA
> Player
> P3M5B Run Route Transition and Negative Matrix Smoke
```

## Technical acceptance

```text
all generated scenes save and reopen
valid scenes retain four required references
no valid scene contains Unity fake-null admission references
duplicate-Slot negative shape is preserved
missing-Actor negative shape is preserved
mismatched-evidence negative shape is preserved
reused-Host negative shape is preserved
package remains unchanged
```

## Product acceptance

```text
one fixture command prepares P3M5B for Play Mode
no separate repair step is required
a failed persistence shape is reported before Play Mode
```

## Architectural gain

Keeps the issue in QA because the fault is fixture persistence, not an official framework contract. The package remains the product authority and the QA harness owns deterministic fixture preparation.

## Suggested commit message

```text
QA: reconcile P3M5B persisted scene fixture
```
