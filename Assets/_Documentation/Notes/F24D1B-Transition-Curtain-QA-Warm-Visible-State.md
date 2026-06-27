# F24D1B — Transition Curtain QA Warm Visible State

## Goal

Corrigir a falha visual da cortina na primeira troca de Route dentro das fixtures QA de Unity Build Surface.

## Problem

O `QA_TransitionCurtainSurface.prefab` mantinha o `CurtainPanel` inativo e o adapter ativava/desativava esse mesmo root. No primeiro uso, a chamada do adapter retornava sucesso e os diagnostics ficavam corretos, mas a UI podia não ter sido renderizada antes do primeiro switch.

## Decision

Manter o painel QA sempre ativo e esconder a cortina por `CanvasGroup.alpha = 0`, sem desativar o root visual.

Também foi elevado o sorting order do Canvas para garantir que a cortina QA fique acima das outras surfaces no cenário de teste.

## Boundary

- Não altera `FrameworkRuntimeHost`.
- Não altera `GameFlowRuntime`.
- Não altera `SceneLifecycleRuntime`.
- Não usa delay.
- Não usa `Task.Delay`.
- Não cria lifecycle paralelo.
- Não transforma a cortina em loading screen.

## Validation

Validar manualmente:

1. Entrar com `QA Transition Game Application`.
2. Executar `TransitionRouteA -> TransitionRouteB`.
3. Confirmar que a cortina é visível já na primeira troca.
4. Confirmar que os logs continuam com `transition='SucceededWithUnitySurface'` e `transitionEffectAdapterCount='1'`.
