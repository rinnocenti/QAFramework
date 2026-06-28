# Immersive Framework ADR Index

This index is the canonical ADR navigation for `com.immersive.framework`.

Current order:

1. F21 - Save / Snapshot / Preferences / Progression Save Foundation
2. F22 - Loading Operation / Progress / Readiness Boundary
3. F23 - Pause Content / Overlay / Input Intent Boundary
4. F24 - Unity Build Surface / Lifecycle Wiring
5. F25 - Activity Content Scene Composition

## Roadmap ADRs

| Phase | ADR | Status |
|---|---|---|
| F00 | [Baseline Reconciliation](F00-ADR-BL-001-Baseline-Reconciliation.md) | Accepted |
| F00 | [Core vs Consumers](F00-ADR-BL-002-Core-vs-Consumers.md) | Accepted |
| F01 | [Framework Facts](F01-ADR-DIAG-001-Framework-Facts.md) | Accepted |
| F01 | [Typed Identity Policy](F01-ADR-ID-001-Typed-Identity-Policy.md) | Accepted |
| F02 | [Session Scope](F02-ADR-SESSION-001-Session-Scope.md) | Accepted |
| F03 | [Route Baseline](F03-ADR-ROUTE-001-Route-Baseline.md) | Accepted |
| F04 | [Activity Baseline](F04-ADR-ACTIVITY-001-Activity-Baseline.md) | Accepted |
| F05 | [Local Identity and Contribution](F05-ADR-LOCAL-001-Local-Identity-and-Contribution.md) | Accepted |
| F06 | [Route Scene Composition](F06-ADR-SCENE-001-Route-Scene-Composition.md) | Accepted |
| F06 | [Content Release](F06-ADR-RELEASE-001-Content-Release.md) | Accepted |
| F07 | [Content Anchor Declaration](F07-ADR-ANCHOR-001-Content-Anchor-Declaration.md) | Accepted |
| F08 | [Runtime Materialization](F08-ADR-RUNTIME-001-Runtime-Materialization.md) | Accepted |
| F09 | [Content Anchor Binding](F09-ADR-ANCHOR-002-Content-Anchor-Binding.md) | Accepted |
| F10 | [Input Ownership](F10-ADR-INPUT-001-Input-Ownership.md) | Accepted |
| F10 | [Pause as Consumer](F10-ADR-PAUSE-001-Pause-as-Consumer.md) | Accepted |
| F10 | [Snapshot Model](F10-ADR-SNAPSHOT-001-Snapshot-Model.md) | Accepted |
| F11 | [Cycle Reset Foundation](F11-ADR-RESET-001-Cycle-Reset-Foundation.md) | Accepted |
| F12 | [Cycle Reset Integration Authoring UX](F12-ADR-RESET-002-Cycle-Reset-Integration-Authoring-UX.md) | Accepted |
| F13 | [Object Entry Foundation](F13-ADR-OBJECT-001-Object-Entry-Foundation.md) | Accepted |
| F14 | [Local Object Reset Foundation](F14-ADR-RESET-003-Local-Object-Reset-Foundation.md) | Accepted |
| F15 | [Unity Reset Adapters](F15-ADR-RESET-004-Unity-Reset-Adapters.md) | Accepted |
| F16 | [GameObject Active State Reset](F16-ADR-RESET-005-GameObject-Active-State-Reset.md) | Accepted |
| F16 | [Player Participant Entry Baseline](F16-ADR-PLAYER-001-Player-Participant-Entry-Baseline.md) | Accepted |
| F17 | [Gate Boundary](F17-ADR-GATE-001-Gate-Boundary.md) | Accepted |
| F17 | [Advanced Consumers Boundary](F17-ADR-CONSUMERS-001-Advanced-Consumers-Boundary.md) | Accepted |
| F18 | [Transition Orchestration](F18-ADR-TRANSITION-001-Transition-Orchestration.md) | Accepted |
| F19 | [Transition Effects Boundary](F19-ADR-TRANSITION-002-Transition-Effects-Boundary.md) | Accepted |
| F20 | [Pause State and Gate](F20-ADR-PAUSE-002-Pause-State-and-Gate.md) | Accepted |
| F21 | [Save / Snapshot / Preferences / Progression Boundary](F21-ADR-SAVE-001-Save-Snapshot-Preferences-Progression-Boundary.md) | Accepted |
| F22 | [Loading Operation / Progress / Readiness Boundary](F22-ADR-LOADING-001-Loading-Operation-Progress-Readiness-Boundary.md) | Accepted |
| F23 | [Pause Content / Overlay / Input Intent Boundary](F23-ADR-PAUSE-003-Pause-Content-Overlay-Input-Boundary.md) | Closed |
| F24 | [Unity Build Surface / Lifecycle Wiring](F24-ADR-UNITY-BUILD-001-Unity-Build-Surface-Lifecycle-Wiring.md) | Accepted / Planned |
| F25 | [Adapter Module Foundation](F25-ADR-ADAPTER-001-Adapter-Module-Foundation.md) | Deferred after Activity scene operation stability |
| F25R | [Activity Scene Operation Architecture Reset](F25R-ADR-ACTIVITY-001-Activity-Scene-Operation-Architecture-Reset.md) | Accepted / Documentation reset |

## Boundary Rules

- F23 is intent/requirement-only.
- F24 is Unity build surface and lifecycle wiring.
- F25 currently stabilizes Activity content scene composition before adapter module foundation resumes.
- F25R resets Activity scene operation architecture: visual policy, LoadingSurface, TransitionSurface, scene load/release and ledger ownership must be decided by `ActivityOperationPlan`.
- Core contracts must not depend on concrete Unity UI, gameplay modules or backend implementations.
- Adapter modules consume framework contracts; they do not redefine route, activity, transition, pause, loading, save or reset ownership.
