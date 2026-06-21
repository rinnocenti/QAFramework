# ADR-LOCAL-002 — Local Contribution Discovery and Requiredness

Status: Draft / Deferred  
Fase: F5  
Tipo: Local  
Escopo: LocalContributionSet

---

## Contexto

O package tem bindings locais simples e marker precursor, mas não possui discovery scoped, contribution set ou requiredness policy.

## Decisão

Local discovery deve operar sobre `ActivityContentSet`/`RouteContentSet`, não sobre busca global.

Fluxo:

```text
ContentSet
→ LocalContributionMarker
→ LocalContributionDiscovery
→ LocalContributionSet
→ Required/Optional policy
```

Required ausente gera failure/fact estruturado. Optional ausente gera skip diagnosticado.

## Consequências

### Positivas

- Evita `FindObjectsByType` global como eixo.
- Cria base para capabilities.
- Dá semântica clara para required/optional.

### Negativas / trade-offs

- Depende de ContentSet maduro.
- Pode exigir mais validators.

## Fora do escopo

- Reset/snapshot/release.
- Actors, Surface, RuntimeSpawned.

## Critérios de validação

- Discovery limitado ao content set ativo.
- Required ausente falha de forma estruturada.
- ContributionSet é produzido em Activity enter.

## Impacto esperado

Pré-requisito para Surface, snapshot participants e capability inventory.

## Relação com roadmap

F5.

## Notas de implementação

Não criar scanner por capability nesta fase.
