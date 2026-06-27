# F24D3 — Transition + Loading Cascade Sequence

## Goal

Corrigir a ordem visual da Route transition para uma cascata explícita:

```text
fade-in / curtain closed
loading show/update while route scene/content loads
loading hide
fade-out / curtain open
```

## Cause

F24D2 corrigiu a ordem lógica dos hooks, mas a Transition surface ainda retornava imediatamente após aplicar alpha. Isso fazia loading e transition parecerem simultâneos na renderização.

## Change

- `ITransitionOrchestrator` agora possui `ExecuteAsync` para fases visuais que precisam aguardar settle.
- `GameFlowRuntime` aguarda `transitionBefore` antes de chamar loading/Route lifecycle.
- `GameFlowRuntime` aguarda `transitionAfter` depois do loading/Route lifecycle.
- `UnityFadeCurtainEffectAdapter` implementa `IAsyncTransitionEffectAdapter` e conclui apenas após a fase visual de fade-in/fade-out.

## Boundary

- Loading continua esperando a operação de Route/Scene/Content.
- Transition não carrega cenas.
- Loading não controla Route/Activity/SceneLifecycle.
- SceneLifecycle não recebe delay artificial.
- Sem `Task.Delay`.
- Sem DOTween.
- Sem lifecycle paralelo.

## Expected visual order

```text
TransitionRouteA -> TransitionRouteB:
  curtain fades in
  loading appears
  route scene switches
  loading hides
  curtain fades out
```
