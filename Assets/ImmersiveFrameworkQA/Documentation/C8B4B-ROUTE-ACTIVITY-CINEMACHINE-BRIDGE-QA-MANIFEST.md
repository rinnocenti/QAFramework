# C8B4B — Superseded

C8B4B foi removido e substituído por C8B4C.

O smoke C8B4B validava uma mistura indesejada entre `FrameworkCameraDirector` e o output Cinemachine. O corte atual remove essa autoridade legacy e mantém somente o caminho canônico:

```text
FrameworkRouteCameraBinding -> FrameworkCinemachineCameraOutputSource -> FrameworkCinemachineOutputApplier
FrameworkActivityCameraBinding -> FrameworkCinemachineCameraOutputSource -> FrameworkCinemachineOutputApplier
```

Use:

```text
Immersive Framework/QA/Camera/C8B4C Canonical Route Activity Cinemachine Smoke
```
