# FXX-CLOSEOUT-PAUSE-1 — Retired Pause Adapter Cleanup + QA Authoring Alignment

Status: Closed

## Decisão

O componente legado `UnityPauseInputActionAdapter` foi removido da QA scene `Assets/ImmersiveFrameworkQA/UnityBuildSurface/Scenes/QA_UIGlobal.unity`, eliminando a origem do warning `UnityPauseInputActionAdapter is retired.` no authoring de QA.

O canvas de QA em `Packages/com.immersive.framework/Runtime/Diagnostics/FrameworkQaCanvas.cs` passou a expor os smokes do fluxo atual de Pause/InputMode:

- `PauseInputModeUnityPlayerInputRuntimeBridgeQaSmokeRunner`
- `PauseInputActionRuntimeBridgeTriggerQaSmokeRunner`

Isso mantém o authoring de QA alinhado ao bridge atual sem recriar o adapter aposentado.

## Arquivos alterados

- `Assets/ImmersiveFrameworkQA/UnityBuildSurface/Scenes/QA_UIGlobal.unity`
- `Packages/com.immersive.framework/Runtime/Diagnostics/FrameworkQaCanvas.cs`

## Validação

Pendente de validação manual:

1. Unity compile/import
2. Standard Smoke
3. Pause smoke / InputMode smoke, se disponíveis no QA Canvas
4. Verificar console: zero warning de `UnityPauseInputActionAdapter`

## Riscos

- O corte removeu a origem do warning na cena QA, mas não executou Unity para confirmar o efeito em import/Play Mode.
- O bridge atual fica exposto por smoke no QA Canvas; se for desejado um authoring scene-based equivalente no futuro, isso exige um corte separado com wiring explícito de `PlayerInput`.

## Observação

O adapter aposentado permanece como stub inerte no runtime, mas não está mais authorado na cena QA afetada por este corte.
