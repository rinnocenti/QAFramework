# F24D4 — Awaitable Cascade Loading Hide Boundary

## Goal

Corrigir a sequência visual de Route transition/loading para que o `fade-out` da cortina só comece depois que o loading terminou e ficou visualmente oculto.

Sequência alvo:

```text
transition fade-in completed
loading show completed
route scene/content loading completed
loading hide completed
transition fade-out completed
```

## Problem

F24D3 tornou a transition aguardável, mas o adapter QA de loading ainda segurava o visual localmente enquanto retornava sucesso imediatamente no `Hide`.

Resultado: o runtime chamava `transitionAfter` enquanto o loading ainda estava visível.

## Change

- Adicionado `IAsyncLoadingSurfaceAdapter` com `UnityEngine.Awaitable`.
- `LoadingSurfaceRuntime` ganhou `ShowAsync`, `UpdateAsync` e `HideAsync`.
- `FrameworkRuntimeHost` agora aguarda `HideAsync` antes de liberar o `transitionAfter`.
- `GameFlowRuntime` usa hooks Awaitable para executar loading dentro da janela da transition.
- `QaLoadingSurfaceVisibilityHoldAdapter` deixou de usar coroutine e passou a usar Awaitable.
- Transition async também foi migrada de `Task` para `UnityEngine.Awaitable`.

## Boundary

- Loading não controla `SceneLifecycleRuntime`.
- Transition não carrega cenas.
- GameFlow continua decidindo a ordem do lifecycle.
- Nenhum delay artificial foi adicionado em SceneLifecycle.
- Nenhum DOTween.
- Nenhum `Task.Delay`.
- Nenhuma coroutine no adapter QA de loading.

## Expected visual order

```text
cortina fecha
loading aparece
rota troca
loading some
cortina abre
```
