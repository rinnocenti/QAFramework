# F24C - Transition Unity Surface Wiring

## Goal

Conectar uma surface visual minima ao contrato de Transition.

## Decision

Reutiliza `TransitionEffects` existentes e `UnityFadeCurtainEffectAdapter`.

## Runtime ownership

`FrameworkRuntimeHost` e `GameFlowRuntime` continuam owners.
A surface nao controla `Route`, `Activity`, `SceneLifecycle` ou `Pause`.

## Scene switch constraint

Route switching usa `LoadSceneMode.Single`, entao a surface visual precisa ser app/session-scoped.
O prefab de surface e instanciado sob o `FrameworkRuntimeHost` persistente.

## Validation

Use as fixtures:

- `Assets/ImmersiveFrameworkQA/UnityBuildSurface/Scenes/TransitionRouteA.unity`
- `Assets/ImmersiveFrameworkQA/UnityBuildSurface/Scenes/TransitionRouteB.unity`
- `Assets/ImmersiveFrameworkQA/UnityBuildSurface/GameApplications/QA_TransitionGameApplication.asset`
- `Assets/ImmersiveFrameworkQA/UnityBuildSurface/Prefabs/QA_TransitionCurtainSurface.prefab`

Logs de Route e Activity mantem os campos F24B e adicionam diagnostics de visual/effect.
O alvo esperado e `transition='SucceededWithUnitySurface'` quando a surface esta configurada.
