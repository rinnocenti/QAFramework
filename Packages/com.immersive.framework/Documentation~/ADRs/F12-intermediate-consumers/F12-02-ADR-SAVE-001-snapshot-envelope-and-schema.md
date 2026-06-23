# F12-02 — ADR-SAVE-001 — Snapshot Envelope and Schema

Status: Draft / Renumbered
Fase: F12
Ordem no Plano: F12-02
Tipo: Consumer / Save
Escopo: Snapshot

---

## Contexto

O `NewScripts` tinha snapshot envelope, mas também payload detection textual. O framework precisa de schema versionado antes de backend concreto.

F11 fornece local lifecycle participants e runtime capability references. F12 pode então criar Snapshot participant/set.

---

## Decisão

Snapshot deve usar envelope tipado:

- owner identity;
- capability/participant identity;
- schema id;
- schema version;
- payload;
- source scope;
- validation result.

Não fazer detecção por busca textual em payload.

---

## Critérios de validação

- Snapshot participant captura envelope.
- Restore rejeita schema incompatível.
- Nenhum payload kind é inferido por string search.
- SnapshotSet consome participants F11, não faz global scan.
