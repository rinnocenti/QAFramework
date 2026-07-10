# C8B4C — Canonical Route/Activity Cinemachine QA Manifest

Status: ready for Unity Editor validation  
Scope: QAFramework only

## Objetivo

Provar que os bindings reais Route/Activity aplicam somente `FrameworkCinemachineCameraOutputSource` e não criam nem usam `FrameworkCameraDirector`.

## Arquivos criados

```text
Assets/ImmersiveFrameworkQA/Editor/CameraAuthoring/QaC8B4CCanonicalRouteActivityCinemachineSmoke.cs
Assets/ImmersiveFrameworkQA/Documentation/C8B4C-CANONICAL-ROUTE-ACTIVITY-CINEMACHINE-QA-MANIFEST.md
```

## Arquivos removidos

```text
Assets/ImmersiveFrameworkQA/Editor/CameraAuthoring/QaC8B4BRouteActivityCinemachineBridgeSmoke.cs
Assets/ImmersiveFrameworkQA/Camera/Scripts/Runtime/QaCameraRouteActivityPanel.cs
Assets/ImmersiveFrameworkQA/Camera/Scripts/Editor/QaCameraRouteActivitySceneBuilder.cs
```

## Casos provados

- Route real aplica câmera, Follow, LookAt e priority.
- Activity real aplica override Cinemachine.
- `UseRoute` não aplica override Activity.
- Required Route e Activity inválidos bloqueiam.
- Optional inválido gera `Skipped`.
- Nenhum `FrameworkCameraDirector` é criado.
- `Camera.enabled` e `GameObject.activeSelf` permanecem inalterados.

## Método de acionamento

O smoke é Editor-only e usa reflexão apenas para construir os contextos internos de lifecycle. O runtime não usa reflexão.

## Superseded

C8B4A bridge aditiva foi superseded. C8B4B provava a mistura indesejada entre binding e director e foi removido. C8B4C mantém somente o caminho Cinemachine canônico.

FIRSTGAME não foi migrado.

## Menu

```text
Immersive Framework/QA/Camera/C8B4C Canonical Route Activity Cinemachine Smoke
```

## Validação manual

Executar C8B4C, C8B2, C8B3, C7 e o proof C6 no FIRSTGAME. A compilação/importação Unity ainda está pendente.

## Commit message sugerida

```text
QA: prove canonical Route Activity Cinemachine bindings
```
