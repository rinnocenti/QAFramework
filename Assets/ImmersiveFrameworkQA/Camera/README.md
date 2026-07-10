# Immersive Framework QA — Camera

`Camera Product Surface QA` continua sendo o fluxo principal de CameraComposer.

## Canonical Route/Activity QA

Execute:

```text
Immersive Framework/QA/Camera/C8B4C Canonical Route Activity Cinemachine Smoke
```

O smoke usa os bindings reais e valida somente:

- `FrameworkCinemachineCameraOutputSource` explícito;
- `FrameworkCinemachineOutputApplier`;
- prioridade, Follow e LookAt;
- `UseRoute` sem override Activity;
- bloqueio required e skip optional;
- nenhum `FrameworkCameraDirector`, `SetActive` ou `Camera.enabled`.

Os antigos scripts e cenas Route/Activity baseados em rig/director foram removidos do fluxo QA. C8B4B está superseded por C8B4C.

## Validação relacionada

```text
Immersive Framework/QA/Camera/C8B2 Cinemachine Output Applier Smoke
Immersive Framework/QA/Camera/C8B3 Route Activity Cinemachine Output Binding Smoke
Immersive Framework/QA/Camera/C7 Camera Product Surface Regression Smoke
```
