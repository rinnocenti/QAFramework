# F12-03 — ADR-SAVE-002 — Save Slot, Progression and Migration

Status: Draft / Planned
Fase: F12
Ordem no Plano: F12-03
Tipo: Consumer / Save
Escopo: Save progression

---

## Contexto

Snapshot envelope cobre captura/restauração de estado. Jogos reais também precisam de slots, manifest, current save pointer, checkpoint/auto/manual policy e migration.

---

## Decisão

Definir contracts:

```text
SaveSlotId
SaveSnapshotId
SaveSlotManifest
CurrentSave pointer
Checkpoint policy
AutoSave policy
ManualSave policy
SaveMigrationPlan
```

Backend concreto continua port/adapter.

---

## Critérios de validação

- Slot manifest lista snapshot id/schema/version.
- CurrentSave aponta para slot lógico, não path físico.
- Migration rejeita schema incompatível sem fallback silencioso.
