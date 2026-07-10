# C8B4B — Route/Activity Cinemachine Bridge QA Manifest

Status: ready for Unity Editor validation  
Scope: QAFramework only

## Objetivo

Provar que `FrameworkRouteCameraBinding` e `FrameworkActivityCameraBinding` reais consomem `FrameworkCinemachineCameraOutputSource` e aplicam output Cinemachine explícito, preservando `FrameworkCameraDirector` e o caminho legacy.

## Escopo

O smoke cria objetos temporários `DontSaveInEditor | DontSaveInBuild`, configura os campos serializados dos componentes reais e dispara os callbacks reais de lifecycle. Ele cobre Route, Activity override, Activity `UseRoute`, output required inválido, output optional inválido e o caminho sem output source.

## Fora de escopo

- Alterar `com.immersive.framework`, FIRSTGAME ou cenas de consumidor.
- Migrar gameplay real ou `CameraComposer`.
- Remover ou reescrever `FrameworkCameraDirector`/Route/Activity legacy.
- Blends avançados, multiplayer, split-screen, spectator ou runtime global.

## Arquivos criados

```text
Assets/ImmersiveFrameworkQA/Editor/CameraAuthoring/QaC8B4BRouteActivityCinemachineBridgeSmoke.cs
Assets/ImmersiveFrameworkQA/Documentation/C8B4B-ROUTE-ACTIVITY-CINEMACHINE-BRIDGE-QA-MANIFEST.md
```

## Arquivos alterados

```text
none
```

## Arquivos removidos

```text
none
```

## Superfície de produto afetada

Nenhuma. `CameraComposer` continua sendo o fluxo principal de produto; Route/Activity permanece integração técnica.

## Fluxo de uso esperado

```text
Route binding real -> output source explícito -> Cinemachine camera/targets/priority
Activity binding real -> override explícito ou policy UseRoute
output inválido required -> Blocked
output inválido optional -> Skipped e legacy preservado
sem output source -> FrameworkCameraDirector legacy continua recebendo os rigs
```

O smoke usa reflexão somente no Editor para construir os contextos internos de lifecycle; isso é necessário porque os construtores/fábricas de `RouteContentLifecycleContext` e `ActivityContentLifecycleContext` não fazem parte da API pública. Não há reflexão no runtime.

## Smoke técnico esperado

Menu:

```text
Immersive Framework/QA/Camera/C8B4B Route Activity Cinemachine Bridge Smoke
```

Logs principais:

```text
[QA][C8B4B RouteActivity Cinemachine Bridge] Smoke started.
[QA][C8B4B RouteActivity Cinemachine Bridge] step='route-binding-output-applied' ...
[QA][C8B4B RouteActivity Cinemachine Bridge] step='activity-binding-output-applied' ...
[QA][C8B4B RouteActivity Cinemachine Bridge] step='activity-use-route-preserved' ...
[QA][C8B4B RouteActivity Cinemachine Bridge] step='required-output-blocked' ...
[QA][C8B4B RouteActivity Cinemachine Bridge] step='optional-output-skipped-legacy-preserved' ...
[QA][C8B4B RouteActivity Cinemachine Bridge] step='legacy-path-preserved' ...
[QA][C8B4B RouteActivity Cinemachine Bridge] PASS. Real Route/Activity bindings consume explicit Cinemachine outputs while preserving the legacy camera director path.
```

## Critérios de aceite técnico

- O novo smoke compila e retorna PASS no Unity Editor.
- Route e Activity usam os bindings reais e aplicam câmera, prioridade, Follow e LookAt explícitos.
- `UseRoute` não aplica override Activity.
- Required inválido retorna `Blocked`/`camera-output-missing` equivalente; optional inválido retorna `Skipped`.
- O smoke não altera `Camera.enabled` nem `GameObject.activeSelf`.
- C8B2, C8B3, C7 e o proof C6/FIRSTGAME continuam PASS após validação manual.

## Critérios de aceite de produto

- `CameraComposer` continua o fluxo principal.
- Route/Activity começa a consumir output técnico explícito sem criar fluxo concorrente para designer.
- Legacy permanece disponível para compatibility/debug.

## Ganho arquitetural

C8B4B fecha a prova entre authoring serializado, bindings reais e o applier Cinemachine, sem transformar `FrameworkCameraDirector` em nova autoridade Cinemachine e sem duplicar o contrato no QA.

## Ganho de usabilidade

O corte reduz o risco de uma ponte funcionar apenas em smoke sintético: agora a validação cobre os componentes que o consumidor realmente adicionaria à cena.

## Declarações de continuidade

- C8B4B prova bindings reais em QA.
- C8B4B não migra FIRSTGAME.
- C8B4B não remove legacy.
- C8B4B não transforma `FrameworkCameraDirector` em nova autoridade Cinemachine.
- C8B4B prepara C8B5.

## Commit message sugerida

```text
QA: prove Route Activity Cinemachine bridge with real bindings
```
