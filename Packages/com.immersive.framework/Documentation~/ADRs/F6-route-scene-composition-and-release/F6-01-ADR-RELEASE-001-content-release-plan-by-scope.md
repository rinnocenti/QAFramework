# F6-01 — ADR-RELEASE-001 — Content Release Plan by Scope

Status: Draft / Deferred  
Fase: F6  
Ordem no Plano: F6-01  
Tipo: Release / Ownership  
Escopo: Session/Route/Activity content

---

## Contexto

O package ainda depende muito do efeito de `LoadSceneMode.Single` para descarte físico. O framework precisa de release explícito por escopo.

## Decisão

Definir `ContentReleasePlan` por escopo:

- Activity exit libera Activity-owned content.
- Route exit libera Route-owned content e activities associadas.
- Session shutdown libera Session-owned content.
- Diagnostic-only content não é liberado.
- Release deve produzir result/facts.

## Consequências

### Positivas

- Evita órfãos.
- Prepara RuntimeSpawned.
- Torna cleanup testável.

### Negativas / trade-offs

- Exige state mais claro.
- Pode duplicar cleanup de scenes se não for cuidadoso.

## Fora do escopo

- Pooling.
- RuntimeContentHandle avançado.
- Snapshot/restore.

## Critérios de validação

- Route/Activity exit geram release result.
- Zero orphan em smoke de content simples.
- Release duplo é idempotente ou rejeitado com fact.

## Impacto esperado

Pré-requisito de Runtime materialization e Surface binding.

## Relação com roadmap

F6/F8.

## Notas de implementação

Release deve ser não destrutivo quando o owner não possui o objeto.
