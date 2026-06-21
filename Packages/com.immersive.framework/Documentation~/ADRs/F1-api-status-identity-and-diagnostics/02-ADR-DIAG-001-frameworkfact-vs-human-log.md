# ADR-DIAG-001 — FrameworkFact vs Human Log

Status: Draft / Deferred  
Fase: F1  
Tipo: Diagnostics  
Escopo: Diagnostics

---

## Contexto

No `NewScripts`, logs e facts às vezes se confundem. O package atual tem logging via `FrameworkLogger`, mas ainda não possui facts estruturados mínimos.

## Decisão

Adicionar `FrameworkFact` como diagnóstico estruturado e manter log humano separado.

`FrameworkFact` deve registrar:

- code/type;
- scope;
- severity;
- source;
- subject identity;
- reason;
- optional details.

Logs são texto para humanos. Facts são dados para smoke, validators, QA e relatórios.

## Consequências

### Positivas

- Evita usar log como contrato.
- Melhora smoke e QA.
- Ajuda validators e decisões fail-fast.

### Negativas / trade-offs

- Exige modelagem mínima.
- Pode gerar duplicidade inicial com logs.

## Fora do escopo

- Telemetry externa.
- Dashboard visual.
- Fact schema avançado.

## Critérios de validação

- Pelo menos boot/content/validation podem emitir facts estruturados.
- Logs continuam existindo, mas não são fonte funcional de verdade.

## Impacto esperado

Pré-requisito para Local requiredness e release diagnostics.

## Relação com roadmap

F1/F5.

## Notas de implementação

Não criar fact recorder monolítico nesta fase.
