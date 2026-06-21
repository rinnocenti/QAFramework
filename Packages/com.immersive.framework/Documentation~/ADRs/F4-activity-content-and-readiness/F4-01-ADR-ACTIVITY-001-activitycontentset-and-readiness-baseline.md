# F4-01 — ADR-ACTIVITY-001 — ActivityContentSet and Readiness Baseline

Status: Draft / Deferred  
Fase: F4  
Ordem no Plano: F4-01  
Tipo: Activity  
Escopo: ActivityFlow

---

## Contexto

O package tem Activity switching e `ActivityContentBinding`, mas ainda não possui `ActivityContentSet` nem readiness formal.

## Decisão

Criar `ActivityContentSet` mínimo e `ActivityReadinessState` básico.

`ActivityContentBinding` deve ser classificado como Local Visibility Adapter, não como materialização canônica.

Readiness inicial:

```text
Ready = Activity request succeeded + content bindings applied + no required baseline validation failed.
```

Futuras phases podem ampliar readiness com contributions, surfaces, runtime content e consumers.

## Consequências

### Positivas

- Activity ganha base de discovery.
- Evita confundir SetActive com materialization.
- Prepara LocalContribution.

### Negativas / trade-offs

- Readiness inicial é simples.
- Pode exigir mudança em logs e QA Canvas.

## Fora do escopo

- ActivityContentProfile avançado.
- Actor/input/camera.
- Reset/snapshot/release.

## Critérios de validação

- Activity enter gera ContentSet.
- Activity switch gera readiness.
- Clear limpa ou invalida ContentSet.

## Impacto esperado

Pré-requisito para LocalContributionDiscovery.

## Relação com roadmap

F4.

## Notas de implementação

Não copiar `ActivityEntryPipeline` do NewScripts.
