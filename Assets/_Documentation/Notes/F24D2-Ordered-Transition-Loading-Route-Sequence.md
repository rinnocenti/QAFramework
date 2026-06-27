# F24D2 — Ordered Transition + Loading Route Sequence

## Goal

Corrigir a ordem visual de Route transitions com Loading Surface.

A ordem canônica para Route requests com scene load deve ser:

```text
transition before / curtain fade-in
loading show
route scene load
loading hide
transition after / curtain fade-out
```

## Boundary

Loading continua sendo surface/diagnóstico. Ele não controla `RouteLifecycleRuntime`, `SceneLifecycleRuntime` ou `ActivityFlowRuntime`.

Transition continua sendo contrato visual/orquestração. Ele não controla loading nem scene lifecycle.

## Implementation

`GameFlowRuntime.RequestRouteAsync` ganhou hooks internos opcionais para executar ações imediatamente após `transitionBefore` e antes de `transitionAfter`.

`FrameworkRuntimeHost` usa esses hooks para chamar `LoadingSurfaceRuntime.Show` e `LoadingSurfaceRuntime.Hide` dentro da janela de Route transition.

## Non-goals

- Sem delay artificial.
- Sem `Task.Delay`.
- Sem novo scene loader.
- Sem lifecycle paralelo.
- Sem UIGlobal ainda.
- Sem alteração em Activity/ActivityClear loading, que continuam `SkippedNoSceneLoad`.

## Validation

Validar no QA Transition Game Application:

```text
Route A -> Route B
Route B -> Route A
```

Visual esperado:

```text
curtain in -> loading visible -> route switch -> loading hidden -> curtain out
```

Logs esperados preservam diagnostics de Transition e Loading.
