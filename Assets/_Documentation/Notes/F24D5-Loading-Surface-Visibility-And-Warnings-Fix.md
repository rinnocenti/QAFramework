# F24D5 — Loading Surface Visibility And Warnings Fix

## Goal

Corrigir os warnings CS1998 introduzidos no eixo Awaitable e tornar a Loading Surface QA visível durante a cascata Transition -> Loading -> Route -> Loading -> Transition.

## Changes

- `NoOpTransitionOrchestrator.ExecuteAsync` mantém conclusão síncrona intencional e suprime CS1998 localmente.
- `QaLoadingSurfaceVisibilityHoldAdapter.ShowAsync` e `UpdateAsync` não usam mais métodos `async` sem `await`; o estado visível aguarda um frame via `Awaitable.NextFrameAsync` para permitir renderização antes do route load/hide.
- `QA_LoadingSurface.prefab` agora usa Canvas sorting explícito acima da curtain QA.
- `LoadingPanel` fica ativo e escondido por `CanvasGroup.alpha = 0`, evitando cold start visual no primeiro uso.

## Boundary

- Não altera SceneLifecycleRuntime.
- Não altera RouteLifecycleRuntime.
- Não cria scene loader alternativo.
- Não usa `Task.Delay`.
- Não usa coroutine.
- Não cria lifecycle paralelo.

## Expected visual order

```text
curtain fade-in
loading visible
route load/switch
loading hidden
curtain fade-out
```
