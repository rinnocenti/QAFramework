# F12-04 — ADR-PAUSE-001 — Pause as Content Anchor Input Activity Transition Consumer

Status: Draft / Renumbered
Fase: F12
Ordem no Plano: F12-04
Tipo: Consumer / Pause
Escopo: Pause

---

## Contexto

No `NewScripts`, Pause mostrou bom uso de content anchor, mas também misturou endpoint, overlay, content materializer, input policy e activity state.

---

## Decisão

Pause deve ser consumer de:

- Content Anchor / ContentAnchorBinding;
- Input;
- Activity state;
- Transition policy;
- optional RuntimeContentAnchorBinding.

Pause não possui o Content Anchor. Pause não cria roots. Pause não controla Activity lifecycle core.

---

## Critérios de validação

- Pause usa ContentAnchorBinding.
- Pause libera content/bindings sem destruir Content Anchor owner.
- Input mode troca no pause/resume sem global locator.
- Pause é bloqueado ou permitido durante transition conforme policy explícita.
