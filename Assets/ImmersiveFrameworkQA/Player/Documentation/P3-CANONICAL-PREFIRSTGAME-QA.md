# P3 Canonical Pre-FIRSTGAME QA

Status: Superseded operational guide; retained as historical inventory  
Last updated: 2026-07-22

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

Each regression owns its own preconditions, cases, cleanup and PASS evidence. A PASS from one regression must not be counted as evidence for another.

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
5. Enter a fresh Play Mode with the fixture required by the selected runtime regression.
6. Run each required Play Mode regression independently and retain its own PASS evidence.
7. Do not report a combined canonical P3 PASS; report the focused regression results.

For the full historical inventory and consolidation decisions, see `../../Documentation/QA-SMOKE-CONSOLIDATION-AUDIT.md`.
