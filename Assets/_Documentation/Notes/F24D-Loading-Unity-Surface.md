# F24D - Loading Unity Surface

## Goal
Adicionar uma surface visual minima de loading para operacoes reais de Route/Scene loading.

## Boundary
Loading nao controla Route, Activity ou SceneLifecycle.

## Current implementation
Prefab app/session-scoped configurado no GameApplicationAsset e instanciado sob FrameworkRuntimeHost.

## Progress
Se progresso real nao estiver disponivel, registrar indeterminate sem progresso fake.

## Future
UIGlobal canonical scene deve substituir prefab app-scoped no futuro.
