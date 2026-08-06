# P3 Canonical Pre-FIRSTGAME QA

Status: Superseded operational guide; retained as historical inventory  
Last updated: 2026-08-02

## Purpose

This document records the scope formerly orchestrated by the P3 canonical Pre-FIRSTGAME mega-runner.

The public command below no longer exists:

```text
Immersive Framework/QA/Player/P3 Run Canonical Pre-FIRSTGAME Smoke
```

It was removed during QA smoke consolidation. No global or Player mega-suite replaces it. Current evidence must come from the focused regression that owns each contract.

## Current Player regression surface

### Edit Mode

```text
Immersive Framework/QA/Regressions/Player/Run Player Participation Authoring Regression
Immersive Framework/QA/Regressions/Player/Run Session Player Slots Regression
Immersive Framework/QA/Regressions/Player/Run Local Player Provisioning Regression
```

### Play Mode

```text
Immersive Framework/QA/Regressions/Player/Run Player Actor Selection Runtime Binding Regression
Immersive Framework/QA/Regressions/Player/Run Player Gameplay Admission Regression
Immersive Framework/QA/Regressions/Player/Run Scene Player Route Lifecycle Regression
```

### Local Player Host Prefab authority sequence

Run this focused sequence; it reuses `QA_UIGlobal`, its one `PlayerInputManager`, the
registered provisioning authoring and the normal Player gameplay fixture:

1. In Edit Mode, run `Immersive Framework/QA/Setup/Prepare Canonical Local Player Runtime Fixture`.
   It saves the manager with an empty technical `playerPrefab` and an authored
   `Local Player Host Prefab`.
2. Run `Run Player Participation Authoring Regression` for authoring, validation,
   non-mutation and divergent-prefab diagnostics.
3. Enter a fresh Play Mode and run `Run Player Gameplay Admission Regression`.
   It proves official boot materialization, no automatic join, `RequestJoin`, authored Host
   provenance, Slot admission, Actor preparation and cleanup.
4. For the blocking path, in Edit Mode run
   `Prepare Local Player Prefab Divergence Play Mode Fixture`, enter Play Mode and run
   `Run Local Player Prefab Divergence Regression`. It proves unavailable runtime, explicit
   divergence diagnostic, blocked request and zero Host/Slot residue. The setup restores the
   canonical empty-manager fixture when Play Mode returns to Edit Mode.

### Player Gameplay Admission execution order

This regression is one-shot per Play Mode session. Any Player joined before it runs invalidates
its zero-player precondition.

1. Exit Play Mode.
2. Run `Immersive Framework/QA/Setup/Prepare Player Gameplay Admission Regression`.
3. Confirm `[PLAYER_GAMEPLAY_ADMISSION_SETUP] status='Prepared'`.
4. Enter a fresh Play Mode.
5. Do not run Pause preflight, Player preflight or any other Player join.
6. Run `Run Player Gameplay Admission Regression`.
7. Exit Play Mode after the result.

Each regression owns its own preconditions, cases, cleanup and PASS evidence. A PASS from one regression must not be counted as evidence for another.

The current package exposes this manual product surface:

```text
Assets/Create/Immersive Framework/Player/Player Slot Profile
```

The former aggregate **Complete Local Player Profile Set** command is not part of the
current package. P3C deliberately does not automate this interactive menu. Its temporary
fixture contains two valid `PlayerSlotProfile` assets, one reused for duplicate and empty
identity validation, and one `GameApplicationAsset`; the temporary folder is removed in
`finally`.

## Historical coverage mapping

The former canonical Editor phase combined:

- Player Slot and participation authoring;
- ordered Session Slot initialization and allocation;
- Local Player Host provisioning, correlation and rollback;
- fixture setup for later gameplay and lifecycle proofs.

That coverage now belongs to the three focused Edit Mode regressions.

The former canonical Play Mode phase combined:

- real local Player join and technical-host admission;
- public default Actor selection;
- Actor materialization and preparation;
- gameplay occupancy, input binding and camera eligibility;
- Route/Activity transition, re-entry and cleanup.

That coverage now belongs to the three focused Play Mode regressions.

## Pre-Authored Player Composer

The former command below is also not part of the current public QA menu:

```text
Immersive Framework/QA/Player Alternatives/P3B Run Pre-Authored Player Composer Smoke
```

Do not use this historical path as proof that a current `PlayerRecipe` or `PlayerComposer` product surface exists. Any future designer-facing Local Player surface requires its own official package implementation and focused QA contract.

## Removed contracts

Current Player regressions must not restore `PlayerSlotDeclaration`, `PlayerSlotOccupancy`, `PlayerEntry`, `PlayerViews`, `PlayerControls`, `PlayerTopology`, F49/F51/F52 PlayerBinding or `SessionPlayerInputManagerDeclaration`.

## Validation guidance

1. Import and compile Framework and QAFramework.
2. Confirm the focused Player regressions appear under `Immersive Framework/QA/Regressions/Player`.
3. Confirm the two historical mega-runner menu paths do not appear.
4. Run each required Edit Mode regression independently.
5. Prepare the selected runtime fixture and enter a fresh Play Mode.
6. Run each required Play Mode regression independently and retain its own PASS evidence.
7. Do not report a combined canonical P3 PASS; report the focused regression results.

For the full historical inventory and consolidation decisions, see `../../Documentation/QA-SMOKE-CONSOLIDATION-AUDIT.md`.
