# F3A — Route Baseline ADR Acceptance

Status: Closed / ADRs Accepted  
Fase: F3  
Tipo: ADR  
Escopo: Route baseline

---

## Objetivo

Fechar a revisão arquitetural inicial da F3 antes de qualquer implementação técnica de Route baseline.

Este corte aceita os ADRs necessários para seguir o roadmap da F3 sem pular para additive scene composition, Surface, RuntimeMaterialization ou consumers.

---

## ADRs aceitos

| Ordem no Plano | ADR | Decisão |
|---|---|---|
| `F3-01` | `ADR-ROUTE-001 — RouteRuntimeState and RouteContentRuntime Status` | Route terá estado runtime tipado. `RouteContentRuntime` será ativado na F3 com escopo limitado a callbacks locais de Route Content na Primary Scene carregada. |
| `F3-02` | `ADR-ROUTE-002 — RouteContentSet Semantics` | `RouteContentSet` é snapshot imutável de conteúdo conhecido da Route; ownership deve ser explícito; release avançado fica para F6. |

---

## Decisões consolidadas

```text
RouteRuntimeState entra na F3.
RouteExitResult mínimo entra na F3.
RouteContentRuntime será Active na F3, não Deferred.
RouteContentRuntime não executa additive scene loading.
RouteContentRuntime não cria Surface ou RuntimeMaterialization.
RouteContentSet é snapshot/registro com ownership explícito.
Release real fica fora da F3.
```

---

## O que F3A não implementa

```text
RouteRuntimeState
RouteExitResult
RouteContentRuntime integration
RouteContentOwnership
Route validator expansion
Route local callback smoke
Additive scene loading
Surface
RuntimeMaterialization
Consumers
```

---

## Próximo corte autorizado pelo roadmap

```text
F3B — IF-FW-ROAD-3A — RouteRuntimeState tipado
```

Escopo do próximo corte:

```text
- criar RouteRuntimeState tipado;
- conectar o estado ao RouteLifecycleRuntime/RouteLifecycleStartResult;
- manter RouteContentRuntime ainda sem integração ativa se o corte ficar restrito ao estado;
- não criar RouteContentSet ownership ainda;
- não criar additive scene loading;
- não criar Surface;
- não criar RuntimeMaterialization.
```

---

## Validação

F3A é documentation/ADR only. Não exige smoke próprio.

A próxima validação de runtime deve ocorrer no primeiro corte técnico da F3.
