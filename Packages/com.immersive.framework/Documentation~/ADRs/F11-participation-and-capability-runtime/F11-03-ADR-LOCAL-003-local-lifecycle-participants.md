# F11-03 — ADR-LOCAL-003 — Local Lifecycle Participants

Status: Draft / Planned
Fase: F11
Ordem no Plano: F11-03
Tipo: Local / Lifecycle
Escopo: Local participants

---

## Contexto

Objetos locais authored e runtime content precisam participar de release, reset e snapshot sem virar owners de Activity/Route.

---

## Decisão

Definir contracts mínimos:

```text
ILocalReleaseParticipant
ILocalResetParticipant
ILocalSnapshotParticipant boundary
LocalParticipantResult
Exit freeze
Participant ordering
```

Snapshot completo e backend entram em F12. F11 define participant/lifecycle boundary.

---

## Regras

- Participant decide como reagir; pipeline/lifecycle decide quando.
- Exit freeze congela a lista de participants/handles antes do teardown.
- Novos registrations são rejeitados quando scope está releasing.
- Falhas required são diagnosticadas por policy.

---

## Critérios de validação

- Release participants rodam antes do owner content desaparecer.
- Reset participants rodam sem trocar Route.
- Snapshot participant boundary não exige backend de save.
- Exit freeze impede novos participants durante teardown.
