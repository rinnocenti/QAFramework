# F9-01 — ADR-ANCHOR-002 — Content Anchor Binding and Runtime Placement

Status: Draft / Deferred  
Fase: F9  
Ordem no Plano: F9-01  
Tipo: Content Anchor / Runtime  
Escopo: ContentAnchorBinding

---

## Contexto

Depois de Content Anchor declaration e Runtime materialization, o framework precisa vincular conteúdo a roots/slots/anchors sem criar pause/camera específicos.

## Decisão

Definir:

```text
ContentAnchorBindingRequest
ContentAnchorBindingResult
ContentAnchorContentHandle
RuntimeContentAnchorBinding
```

Consumer solicita Content Anchor por identity; runtime resolve root/slot/anchor e materializa ou vincula conteúdo. Consumer não destrói diretamente; libera handle.

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
- Binding falha se content anchor/slot required ausente.
- Release de binding ocorre antes do release do owner scope.

## Impacto esperado

Destrava Pause e Camera consumers.

## Relação com roadmap

F9.

## Notas de implementação

Não deve introduzir `ContentAnchorManager` global.
