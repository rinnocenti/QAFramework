# ADR-SURFACE-002 — Surface Binding and Content Placement

Status: Draft / Deferred  
Fase: F9  
Tipo: Surface / Runtime  
Escopo: SurfaceBinding

---

## Contexto

Depois de Surface declaration e Runtime materialization, o framework precisa vincular conteúdo a roots/slots/anchors sem criar pause/camera específicos.

## Decisão

Definir:

```text
SurfaceBindingRequest
SurfaceBindingResult
SurfaceContentHandle
RuntimeSurfaceBinding
```

Consumer solicita Surface por identity; runtime resolve root/slot/anchor e materializa ou vincula conteúdo. Consumer não destrói diretamente; libera handle.

## Consequências

### Positivas

- Permite pause/camera/presentation depois.
- Centraliza release order.
- Evita endpoint local materializando e destruindo conteúdo.

### Negativas / trade-offs

- Depende de F7 e F8.
- Pode exigir mais validators.

## Fora do escopo

- Pause overlay concreto.
- Cinemachine.
- Actor presentation.
- Pooling.

## Critérios de validação

- Prefab em slot materializa e libera no exit.
- Binding falha se surface/slot required ausente.
- Release de binding ocorre antes do release do owner scope.

## Impacto esperado

Destrava Pause e Camera consumers.

## Relação com roadmap

F9.

## Notas de implementação

Não deve introduzir `SurfaceManager` global.
