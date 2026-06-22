# Route Release Smoke

Status: `F6G APPLIED / PENDING SMOKE`

## Objetivo

Validar que uma Route com `RouteContentProfileAsset` e additional scene owned libera a cena additive ao sair da Route.

## Setup esperado

```text
Route Scene Composition Route = QA Canonical Route
Alternate Route = QA Alternate Route
Expected Route Scene Loaded Count = 2
Expected Route Scene Owned Loaded Count = 2
Expected Route Release Released Count = 1
```

A Route de composição deve declarar uma Primary Scene e uma additional scene válida no Build Profile/Shared Scene List. A additional scene deve estar marcada como `Owned` e não pode ser a active Primary Scene.

## Sequência

O botão `Run Route Release Smoke` executa:

```text
1. Garantir que a Route de composição esteja ativa e com profile carregado.
2. Solicitar a Alternate Route.
3. Validar ContentReleaseResult da saída da Route anterior.
4. Restaurar a Route de composição.
5. Validar RouteSceneCompositionResult novamente.
```

## Resultado esperado

No step `release`:

```text
QA Route Release Smoke step completed.
routeRelease='Succeeded'
routeReleaseReleased='1'
routeReleaseSkipped='1'
routeReleaseFailed='0'
routeReleaseBlockingIssues='0'
```

No step `restore-composition`:

```text
routeSceneComposition='Succeeded'
routeSceneLoaded='2'
routeSceneOwnedLoaded='2'
routeSceneFailed='0'
routeSceneBlockingIssues='0'
```

## Fronteira

Este smoke valida apenas unload de cena additive owned no escopo de Route. Ele não valida Activity release, runtime object destroy, pool return, Surface, RuntimeRoot, Actor, Input, Camera, Save ou Pooling.
