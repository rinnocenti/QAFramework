# F10-02 — ADR-PAUSE-001 — Pause as Content Anchor Input Activity Consumer

Status: Draft / Deferred  
Fase: F10  
Ordem no Plano: F10-02  
Tipo: Consumer / Pause  
Escopo: Pause

---

## Contexto

No `NewScripts`, Pause mostrou bom uso de content anchor, mas também misturou endpoint, overlay, content materializer e input policy.

## Decisão

Pause deve ser consumer de:

- Content Anchor;
- Input;
- Activity state;
- optional RuntimeContentAnchorBinding.

Pause não possui o Content Anchor. Pause não cria roots. Pause não controla Activity lifecycle core.

## Consequências

### Positivas

- Evita `RoutePauseSurfaceEndpoint` monolítico.
- Usa Content Anchor baseline corretamente.
- Permite pause contextual por Activity/Route.

### Negativas / trade-offs

- Depende de Content Anchor e Input.
- Pause visível chega mais tarde.

## Fora do escopo

- Pause overlay final.
- UI/HUD complexa.
- Save menu.

## Critérios de validação

- Pause usa ContentAnchorBinding.
- Pause libera content/bindings sem destruir Content Anchor owner.
- Input mode troca no pause/resume sem global locator.

## Impacto esperado

Consumer intermediário importante.

## Relação com roadmap

F10.

## Notas de implementação

Pause é bom primeiro teste real de Content Anchor consumer.
