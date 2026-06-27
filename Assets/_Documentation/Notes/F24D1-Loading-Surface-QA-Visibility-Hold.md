# F24D1 — Loading Surface QA Visibility Hold

## Goal

Permitir validação visual manual da `QA_LoadingSurface` mesmo quando o carregamento real da cena é rápido demais para percepção humana.

## Boundary

Este corte é QA-only.

A alteração não simula loading no framework core e não atrasa:

```text
RouteLifecycleRuntime
SceneLifecycleRuntime
ActivityFlowRuntime
GameFlowRuntime
FrameworkRuntimeHost request flow
```

## Implementation

`QA_LoadingSurface.prefab` passa a usar:

```text
Assets/ImmersiveFrameworkQA/UnityBuildSurface/Scripts/Runtime/QaLoadingSurfaceVisibilityHoldAdapter.cs
```

O adapter implementa `ILoadingSurfaceAdapter` e mantém a superfície visível por um curto período após o request de hide. O método `Hide` retorna imediatamente; apenas o estado visual é ocultado depois por coroutine local do prefab QA.

## Diagnostics

Os diagnostics esperados permanecem iguais ao F24D:

```text
loading='SucceededWithUnitySurface'
loadingVisual='UnitySurface'
loadingBefore='Succeeded'
loadingAfter='Succeeded'
loadingBlockingIssues='0'
loadingAdapterCount='1'
loadingProgressSupported='False'
loadingProgress='Indeterminate'
```

## Future

A solução final de UI global deve migrar para uma `UIGlobal` canonical scene com canvas persistente. Este corte não implementa essa cena.
