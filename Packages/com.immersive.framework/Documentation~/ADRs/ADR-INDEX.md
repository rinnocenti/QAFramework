# ADR Index

Canonical framework plan:

```text
Documentation~/Planning/Immersive-Framework-Roadmap-Revisado.md
```

ADRs record accepted architectural decisions. They do not replace the operational roadmap and must not redefine phase order.

F11 is closed as `Cycle Reset Foundation`. F12 is closed as `Cycle Reset Integration & Authoring UX`. F13 is the next planned phase.

## Accepted ADRs

| Phase | ADR | Status |
|---|---|---|
| F00 | [Baseline Reconciliation](F00-ADR-BL-001-Baseline-Reconciliation.md) | Accepted |
| F00 | [Core vs Consumers](F00-ADR-BL-002-Core-vs-Consumers.md) | Accepted |
| F01 | [Typed Identity Policy](F01-ADR-ID-001-Typed-Identity-Policy.md) | Accepted |
| F01 | [Framework Facts](F01-ADR-DIAG-001-Framework-Facts.md) | Accepted |
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
| F10 | [Snapshot Model](F10-ADR-SNAPSHOT-001-Snapshot-Model.md) | Accepted |
| F10 | [Pause as Consumer](F10-ADR-PAUSE-001-Pause-as-Consumer.md) | Accepted |
| F11 | [Cycle Reset Foundation](F11-ADR-RESET-001-Cycle-Reset-Foundation.md) | Applied through F11G |
| F12 | [Cycle Reset Integration and Authoring UX](F12-ADR-RESET-002-Cycle-Reset-Integration-Authoring-UX.md) | Applied through F12E |
| F13 | [Object Entry Foundation](F13-ADR-OBJECT-001-Object-Entry-Foundation.md) | Accepted / Planned |
| F14 | [Local Object Reset Foundation](F14-ADR-RESET-003-Local-Object-Reset-Foundation.md) | Accepted / Planned |
| F15 | [Unity Reset Adapters](F15-ADR-RESET-004-Unity-Reset-Adapters.md) | Accepted / Planned |
| F16 | [Player Participant Entry Baseline](F16-ADR-PLAYER-001-Player-Participant-Entry-Baseline.md) | Accepted / Planned |
| F17 | [Advanced Consumers Boundary](F17-ADR-CONSUMERS-001-Advanced-Consumers-Boundary.md) | Accepted / Planned |
| F18 | [Gameplay Capabilities Boundary](F18-ADR-GAMEPLAY-001-Gameplay-Capabilities-Boundary.md) | Accepted / Planned |

## Rule

Past ADRs record accepted/applied decisions. Future ADRs guide implementation and should be improved before each phase starts.

F12 decision note: Cycle Reset Unity Event Bridges are optional. The trigger is the primary component; bridges only expose result callbacks in the Inspector.
