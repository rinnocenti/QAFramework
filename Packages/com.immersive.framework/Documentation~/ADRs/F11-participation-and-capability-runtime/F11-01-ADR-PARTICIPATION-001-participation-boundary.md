# F11-01 — ADR-PARTICIPATION-001 — Participation Boundary

Status: Draft / Planned
Fase: F11
Ordem no Plano: F11-01
Tipo: Participation
Escopo: Session/Route/Activity

---

## Contexto

Player participation no `NewScripts` era relevante, mas estava acoplado a PlayerActor, Input, Camera e ActivityEntry. O framework precisa preservar o conceito sem implementar PlayerActor cedo.

---

## Decisão

Definir boundary mínimo:

```text
ParticipantId
PlayerSlot
ParticipationScope
ParticipationState
ParticipationBindingRequest
ParticipationBindingResult
```

Input, Actor, Camera e Save consomem Participation depois. Participation não controla lifecycle core.

---

## Fora do escopo

- Multiplayer/networking.
- PlayerActor concreto.
- Input command hub.
- Camera target binding final.

---

## Critérios de validação

- Participant identity não depende de GameObject.name.
- Slot/binding possui owner scope.
- Binding stale/foreign é rejeitado.
