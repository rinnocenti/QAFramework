# F10-01 — ADR-TRANSITION-001 — Scene Transition and Loading Policy

Status: Draft / Planned
Fase: F10
Ordem no Plano: F10-01
Tipo: Lifecycle / Transition
Escopo: Route/Activity transition

---

## Contexto

No `NewScripts`, transition/loading combinava cut, curtain, loading, readiness e bloqueio de input. O roadmap antigo cobriu scene composition/release em F6, mas deixou sem fase a política de transição/loading.

F10 recupera essa camada sem colocar presentation visual dentro de SceneLifecycle.

---

## Decisão

Definir contratos mínimos:

```text
TransitionRequest
TransitionPolicy
TransitionProgress
TransitionResult
TransitionInputLockPolicy
```

A política de transição orquestra Route/Activity changes, loading progress e readiness. Fade/loading screen/curtain visual são adapters/consumers futuros, não core de SceneLifecycle.

---

## Regras

- F6 continua sendo scene composition/release técnico.
- F10 coordena transition policy e progress.
- F10 pode bloquear/liberar input por policy, mas o full Input consumer entra em F12.
- F10 não cria UI de loading final.
- F10 não controla Camera, Audio, Actor ou Pause.

---

## Critérios de validação

- TransitionRequest produz TransitionResult explícito.
- Loading progress pode agregar scene load, materialization e readiness.
- Input lock policy é observável sem depender do Input System concreto.
- Transition rejeita execução concorrente incompatível.
