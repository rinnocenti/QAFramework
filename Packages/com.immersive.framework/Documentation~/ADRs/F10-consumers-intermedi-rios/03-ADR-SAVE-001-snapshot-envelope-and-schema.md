# ADR-SAVE-001 — Snapshot Envelope and Schema

Status: Draft / Deferred  
Fase: F10  
Tipo: Consumer / Save  
Escopo: Snapshot

---

## Contexto

O `NewScripts` tem snapshot envelope, mas também payload detection textual. O framework precisa de schema versionado antes de backend concreto.

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

## Consequências

### Positivas

- Save fica desacoplado do backend.
- Restore pode validar schema.
- Prepara local snapshot participants.

### Negativas / trade-offs

- Mais estrutura antes de save visível.
- Precisa decidir serialização depois.

## Fora do escopo

- Backend de save concreto.
- UI de save/load.
- Cloud/prefs/files.

## Critérios de validação

- Snapshot participant captura envelope.
- Restore rejeita schema incompatível.
- Nenhum payload kind é inferido por string search.

## Impacto esperado

Base para Save consumer.

## Relação com roadmap

F10.

## Notas de implementação

Backend é port/adaptor separado.
