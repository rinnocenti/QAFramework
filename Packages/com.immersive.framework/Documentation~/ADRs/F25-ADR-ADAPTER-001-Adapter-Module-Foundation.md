# F25-ADR-ADAPTER-001 - Adapter Module Foundation

Status: Deferred / Planned after F24  
Phase: F25 - Adapter Module Foundation  
Type: Adapter Module / Gameplay & Subsystem Boundary  
Last updated: 2026-06-26

## Context

The framework now separates three tracks:

1. Framework Core / Contracts
2. Unity Build Surface / Lifecycle Wiring
3. Adapter Modules / Gameplay & Subsystems

F25 belongs to the third track. It starts only after F24 proves the real Unity lifecycle path and minimal build surfaces.

## Decision

F25 is Adapter Module Foundation.

It is broader than gameplay-only work. F25 defines how optional adapter modules consume framework contracts and Unity build surfaces without moving subsystem behavior into framework core.

Adapter modules must consume existing framework contracts instead of redefining them:

- Route
- Activity
- Gate
- Transition
- Loading
- Pause
- Snapshot
- Preferences
- Progression Save
- Runtime Content
- Content Anchor
- Object Entry
- Object Reset
- Pooling boundary
- Diagnostics

## Deferred Adapter Families

F25 may later plan adapters for:

- gameplay
- camera
- audio
- input
- advanced save authoring
- pooling/runtime spawned objects
- actor/player/NPC
- inventory
- combat
- projectile
- damage

The exact adapter cuts remain deferred until F24 closes.

## Guardrails

- Adapter modules must not alter Route/Activity ownership.
- Adapter modules must not create a parallel lifecycle pipeline.
- Adapter modules must not bypass Gate when flow admission is required.
- Adapter modules must not redefine Transition, Loading, Pause, Save or Reset contracts.
- Adapter modules must not use Unity object names or paths as framework identity.
- Adapter modules must not move optional subsystem behavior into framework core.

## Excluded Now

This ADR does not implement runtime code, asmdef changes, scene objects, prefabs, ScriptableObjects, UI, smokes or concrete adapters.
