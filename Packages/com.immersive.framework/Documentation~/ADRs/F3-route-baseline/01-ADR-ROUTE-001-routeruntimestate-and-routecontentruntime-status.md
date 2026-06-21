# ADR-ROUTE-001 — RouteRuntimeState and RouteContentRuntime Status

Status: Draft / Deferred  
Fase: F3  
Tipo: Route  
Escopo: Route lifecycle

---

## Contexto

O package tem `RouteLifecycleRuntime`, `RouteContentRuntime`, `RouteContentBinding` e route events. O risco atual é `RouteContentRuntime` existir sem fluxo real.

## Decisão

Route deve ter estado runtime tipado e o status de `RouteContentRuntime` deve ser explícito:

Opções:
1. `Active`: `RouteLifecycleRuntime` chama enter/exit de route content.
2. `Deferred`: código fica fora do fluxo e marcado como experimental.
3. `Removed`: superfície sai até haver modelo de route content.

Se ativo, route local callbacks devem executar em enter/exit da route ativa e participar de smoke.

## Consequências

### Positivas

- Remove ambiguidade do baseline.
- Permite route-local sem scene scan genérico futuro.
- Ajuda CameraFlow se algum dia voltar como consumer.

### Negativas / trade-offs

- Conectar pode expor bugs de lifecycle.
- Remover pode descartar código já escrito.

## Fora do escopo

- Additive scene loading.
- Route surface.
- Camera consumer.

## Critérios de validação

- `RouteContentRuntime` não fica sem status.
- Se ativo, smoke prova callbacks route enter/exit.
- Se deferred/removed, docs e validators refletem.

## Impacto esperado

Crítico antes de Route scene composition e Surface.

## Relação com roadmap

F3.

## Notas de implementação

Não conectar em F0; conectar/remover em fase Route.
