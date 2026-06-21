# F5-01 — ADR-LOCAL-001 — Local Identity

Status: Draft / Deferred  
Fase: F5  
Ordem no Plano: F5-01  
Tipo: Local  
Escopo: LocalContentIdentity

---

## Contexto

O `NewScripts` usava `targetId` como cola universal. O package precisa de uma identidade local tipada antes de contribution discovery.

## Decisão

Definir `LocalContentIdentity` como identidade funcional de objetos/contributions locais.

Regras:

- Não usar GameObject name, transform path ou scene path como identity funcional.
- Labels podem existir para Inspector/diagnostics.
- Identity deve ser validável e única dentro do escopo relevante.
- O escopo pode ser Activity, Route, SceneContent ou outro definido pelo ContentSet.

## Consequências

### Positivas

- Evita repetir `targetId`.
- Permite validators.
- Base para requiredness e capability descriptors.

### Negativas / trade-offs

- Exige authoring explícito.
- Pode exigir migration de markers existentes.

## Fora do escopo

- Capability inventory.
- Runtime references.
- Snapshot/restore.

## Critérios de validação

- Marker sem identity falha em validation.
- Duplicidade no mesmo scope é detectada.
- Paths/nomes só aparecem como diagnóstico.

## Impacto esperado

Base para LocalContributionSet e Surface.

## Relação com roadmap

F5.

## Notas de implementação

Alinha com ADR-ID-001.
