# Immersive Framework

Development package for game lifecycle architecture in Unity 6.

Package name:

```text
com.immersive.framework
```

This package owns framework-level game lifecycle concepts such as bootstrap, Game Application, Game Flow, Route, Activity, framework diagnostics, and future integrations with scene lifecycle, input, camera, save and pooling.

Technical infrastructure remains outside this package:

- `com.immersive.foundation`
- `com.immersive.logging`
- `com.immersive.pooling`

## Frozen baseline — Content Materialization Planning

The current materialization baseline is frozen at:

```text
IF-FW-4C — ContentFlow Core + Route Content Set Baseline
IF-FW-4D — Route Content Profile Planning Baseline
```

Validation status:

```text
IF-FW-4C: CLOSED / COMPILE PASS
IF-FW-4D: CLOSED / COMPILE PASS
Smoke: optional for both cuts because they do not change visible scene behavior beyond diagnostics/authoring.
```

This baseline establishes the first common language for content materialization and applies it only to Route primary-scene tracking and Route content planning.

Important frozen decisions:

- content is not `GameObject.SetActive`;
- `ActivityContentBinding` and `RouteContentBinding` are local visibility/lifecycle adapters, not canonical materialization;
- CameraFlow and AudioFlow are intentionally out of scope until materialization scopes are planned top-down;
- Route additive scene execution is deferred;
- Activity content profiles are deferred;
- no fallback path is allowed for required content once execution cuts begin.

The planned order is top-down:

```text
Session
↓
Route
↓
Activity
↓
Local
```

The next implementation step must not be route-additive execution. It should first normalize the scope model across Session, Route, Activity and Local.

Deferred / do not apply as current baseline:

```text
IF-FW-4E — Route Scene Composition Execution
```

That route-additive cut is conceptually useful later, but it was produced too early. It should be replaced by a later route execution cut after the scope-set baseline is documented and implemented.

## Cut history summary

Earlier cuts remain part of project history, but the active architectural front is now materialization.

## IF-FW-4C — ContentFlow Core + Route Content Set Baseline

`IF-FW-4C` introduces the first common content materialization language and applies it to Route primary scene loading without changing route behavior.

This cut adds:

- `Runtime/ContentFlow` with scope, kind, requiredness, handle, set, materializer and contribution marker contracts;
- `RouteContentSet`, owned by Route Lifecycle;
- registration of the active Route primary scene as a required Route-scoped `Scene` content handle;
- diagnostics that append the Route Content Set summary to successful Route lifecycle logs.

This cut does not add camera, audio, actor, pause, presentation, Addressables, additive scene composition or prefab materialization. The existing `ActivityContentBinding` remains a simple local visibility adapter and is not promoted to canonical materialization.

Route is intentionally handled before Activity: the Route materializes the context where Activities later contribute content.


## IF-FW-4D — Route Content Profile Planning Baseline

`IF-FW-4D` adds a Route-owned authoring/planning surface for future route content composition without changing runtime scene loading behavior.

This cut adds:

- `RouteContentProfileAsset`;
- `RouteContentSceneEntry`;
- Route inspector assignment for an optional Content Profile;
- a Route Content Profile inspector for declaring additional scenes;
- `RouteContentMaterializationPlan`;
- diagnostics that append planned additional Route scenes to the `RouteContentSet` message when a profile is assigned.

The current runtime still loads only the Route Primary Scene. Additional scenes declared in the profile are planned-only and may become additive materialization inputs after the scope model is solved top-down.

This cut does not add camera, audio, actor, pause, presentation, Addressables, prefab materialization, Activity content profiles, or additive scene execution.

## QA Baseline

O baseline funcional e arquitetural validado do framework e o modelo de QA manual adotado para este package.

- O jogo/framework continua bootando pelo `Active Game Application` normal do projeto.
- Nao existe `QA_GameApplication` como aplicacao paralela padrao.
- A superficie manual de QA e `Immersive Framework > QA > Framework QA Canvas`, em IMGUI/OnGUI.
- Os assets em `Assets/ImmersiveFrameworkQA/` sao `scenario targets`, nao substitutos do boot do projeto.
- Para repetir smokes na mesma sessao de Play Mode, use `Reset QA Scenario`.
- Para validar uma Route sem Activity inicial, use `QA_NoActivityRoute`.
- Para validar uma Activity sem conteudo correspondente, use `QA_NoContentActivity`.
- Para trocar entre Activities, use `QA_PrimaryContentActivity` e `QA_SecondaryContentActivity`.
- Para trocar entre Routes, use `QA_CanonicalRoute` e `QA_AlternateRoute`.
- Considere PASS quando os logs canonicos aparecerem na ordem esperada e nao houver CS errors, Exceptions, FATAL ou missing targets.

## First setup

1. Open `Project Settings > Immersive Framework`.
2. Click `Create and Assign Game Application`.
3. Select the created `Game Application` asset.
4. In the `Startup` section, click `Create and Assign Startup Route`.
5. Select the created `Route` asset.
6. Assign a `Primary Scene` in the Route Inspector.
7. Optionally create and assign a `Route Content Profile` in the Route Inspector. Additional scenes declared there are planning data only in this frozen baseline.
8. Optionally create and assign a `Startup Activity` in the Route Inspector.
9. Enter Play Mode.

Alternative assignment flow:

1. Select a `Game Application` asset.
2. In its Inspector, use `Set as Active Game Application`.

The framework boot should succeed once an Active Game Application, Startup Route, and Startup Route Primary Scene are assigned. If the Primary Scene is not already loaded, it must be available to Unity runtime loading, usually by being included in Build Settings.

If any required setup is missing, the framework fails fast in Play Mode. Project Settings also previews the same required-missing status before entering Play Mode.

Expected happy-path boot log when the Startup Scene is already loaded and no Startup Activity is assigned:

```text
[Immersive Framework] Boot succeeded. Application Runtime started. Game Application 'Game Application' resolved. Startup Route 'Startup Route' resolved. Primary Scene 'StartupScene' declared. Game Flow started with Startup Route 'Startup Route'. Route Lifecycle started Route 'Startup Route'. Scene Lifecycle resolved Primary Scene 'StartupScene' and set it active. alreadyLoaded='True'. loadMode='AlreadyLoaded'. Validation Mode: Standard.
```

Expected happy-path boot log when another scene was open and the framework loaded the Startup Scene:

```text
[Immersive Framework] Boot succeeded. Application Runtime started. Game Application 'Game Application' resolved. Startup Route 'Startup Route' resolved. Primary Scene 'StartupScene' declared. Game Flow started with Startup Route 'Startup Route'. Route Lifecycle started Route 'Startup Route'. Scene Lifecycle resolved Primary Scene 'StartupScene' and set it active. alreadyLoaded='False'. loadMode='Single'. Validation Mode: Standard.
```


Current boot logs include a Route Content Set fragment after the primary scene is resolved. When a Route Content Profile is assigned, the same message also includes the planned additional scenes. Example without additional scenes:

```text
Route Content Set registered 1 handle(s). scope='Route' owner='Startup Route' handles='1' details=[id='route:Assets/Scenes/StartupScene.unity:primary-scene:StartupScene' scope='Route' kind='Scene' requiredness='Required' owner='Startup Route' resource='StartupScene' active='True'].
```

Example with a Route Content Profile assigned:

```text
Route Content Set registered 1 handle(s). scope='Route' owner='Startup Route' handles='1' details=[...]. Route Content Plan profile='StartupRouteContentProfile' additionalScenes='1' plannedOnly='True' details=[id='lighting' scene='StartupLighting' requiredness='Optional' index='0']
```

## Usage guide

The incremental HTML usage guide lives in:

```text
Documentation~/Guides/Usage/index.html
```

This guide records only what already exists in the package and should be updated cut by cut.

## Framework QA Canvas

`FrameworkQaCanvas` is a development-only manual QA surface. It is not a product runtime UI and does not replace automated tests.

The `Reset QA Scenario` button returns the current Play Mode session to a baseline without stopping Play Mode. This preserves the Console log while giving each smoke a predictable starting point.

Baseline reset behavior:

- `Reset Route` is used when assigned. If empty, the current Game Application's Startup Route is used.
- `Reset Activity` is used when assigned. If empty, the resolved reset Route's Startup Activity is used.
- if the reset Route has no Startup Activity and no explicit Reset Activity, the reset clears the active Activity.
- reset requests use `source='FrameworkQaCanvas'` and `qa.reset.*` reasons by default.

QA assets under `Assets/ImmersiveFrameworkQA/` remain scenario targets. They should not become the Active Game Application by default.


## Architecture

Architectural decisions live in:

```text
Documentation~/Architecture/ADR/
```

Start with:

```text
ADR-IF-FW-0001-lifecycle-qa-baseline.md
```

Then review:

```text
ADR-0001-bootstrap-minimo-e-construcao-incremental.md
```

## Editor Play Mode Startup

`Project Settings > Immersive Framework` includes an editor-only startup mode:

- `Framework Startup`: normal framework boot through `Game Application -> Startup Route -> Primary Scene`.
- `Current Scene Only`: skips framework boot in the Unity Editor so the currently open scene can be tested in isolation.

Player builds always use framework startup. `Current Scene Only` is only an authoring workflow switch.

Use `Current Scene Only` when creating or debugging an isolated scene that should not be replaced by the Startup Route scene.



## Activity Content Binding

Add `Activity Content Binding` to a scene GameObject when that object should be active only while a specific Activity is active.

Inspector fields:

- `Activity`: the Activity that owns this scene content.

When Activity Flow starts, switches, or clears an Activity, the framework applies all loaded scene bindings:

- bindings that match the active Activity are activated;
- bindings for other Activities are deactivated;
- bindings without an assigned Activity are skipped.

Keep Activity Content Binding roots simple. Avoid nesting different Activity bindings under each other until nested content policy exists.

### Activity Content Binding authoring guardrails

The `Activity Content Binding` Inspector shows the currently assigned Activity and explains the current scope of the component.

Guardrails:

- missing `Activity` reference is shown as an authoring error;
- parent/child `Activity Content Binding` nesting is shown as a warning;
- the Inspector states that this component only controls GameObject active state.

Nested Activity content is intentionally not supported yet. Keep Activity content roots flat until a dedicated nested content policy exists.

Expected switch log when bindings exist:

```text
[Immersive Framework] Activity Request completed. source='ActivityRequestTrigger' reason='atividade.change'. Activity Flow switched from Activity 'Activity 01' to Activity 'Activity 02'. Activity Content applied 2 binding(s) for Activity 'Activity 02'. activated='1' deactivated='1' unchanged='0'.
```

Expected observability detail log:

```text
[Immersive Framework] Activity Content Binding diagnostics. activeActivity='Activity 02' observations=[object='Panel_Activity02' scene='StartupScene' assignedActivity='Activity 02' action='Activate' reason='MatchedActiveActivity'; object='Panel_Activity01' scene='StartupScene' assignedActivity='Activity 01' action='Deactivate' reason='DifferentActivity'].
```

If a binding has no Activity assigned, the framework emits a warning:

```text
[Immersive Framework] Activity Content Binding warning. warnings=[object='Panel_MissingActivity' scene='StartupScene' reason='MissingActivityReference'].
```

These diagnostics are intentionally runtime diagnostics only. They do not create a registry, scanner asset, content profile or validation window.

## Runtime Activity Request Trigger

Add `Activity Request Trigger` to a GameObject when a scene object or UI Button needs to request another Activity inside the active Route.

Inspector fields:

- `Target Activity`: the Activity to start.
- `Reason`: optional diagnostic text. If empty, the target activity name is used.

For a Unity UI Button, wire the button `OnClick` event to:

```text
ActivityRequestTrigger.RequestActivity
```

Expected success log:

```text
[Immersive Framework] Activity Request completed. source='ActivityRequestTrigger' reason='<Reason>'. Activity Flow switched from Activity '<Previous Activity>' to Activity '<Target Activity>'.
```

If the requested Activity is already active, the request is ignored explicitly instead of silently restarting the same Activity. If `ClearActivity` is called with no active Activity, the request is also ignored explicitly.

## Runtime Route Request Trigger

Add `Route Request Trigger` to a GameObject when a scene object or UI Button needs to request another Route.

Inspector fields:

- `Target Route`: the Route to start.
- `Reason`: optional diagnostic text. If empty, the target route name is used.

For a Unity UI Button, wire the button `OnClick` event to:

```text
RouteRequestTrigger.RequestRoute
```

Expected success log:

```text
[Immersive Framework] Route Request completed. source='RouteRequestTrigger' reason='<Reason>'. Route Lifecycle started Route '<Route Name>'. Scene Lifecycle resolved Primary Scene '<Scene Name>' and set it active. alreadyLoaded='False'. loadMode='Single'.
```

If the requested route is already active, the request is ignored explicitly instead of silently reloading the same route.

## IF-FW-2I — Route switch ownership

Route Lifecycle now owns the active Route identity. Game Flow still accepts route requests, but it no longer keeps the active Route as its own state.

Current route switching flow:

```text
Route Request Trigger
  -> Application Runtime
      -> Game Flow
          -> Route Lifecycle
              -> Scene Lifecycle
```

When a request switches from one Route to another, diagnostics now report the previous Route and the next Route in the Route Lifecycle message.



## IF-FW-2J — Route request diagnostics ownership

Route request completion logs are now emitted by the persistent `FrameworkRuntimeHost`, not by the scene-authored `RouteRequestTrigger`.

Reason: when a route request uses `LoadSceneMode.Single`, the requesting scene object may be destroyed as part of the route switch. Diagnostics should therefore be owned by an application-scope runtime object that survives the scene change.

The trigger remains the public UnityEvent boundary. It validates obvious local authoring issues, then submits the request to the runtime host.

Expected route request success log now includes source and reason:

```text
[Immersive Framework] Route Request completed. source='RouteRequestTrigger' reason='Second Route'. Route Lifecycle switched from Route 'Startup Route' to Route 'Second Route'. Scene Lifecycle resolved Primary Scene 'SecondScene' and set it active. alreadyLoaded='False'. loadMode='Single'.
```


### Runtime Activity Clear Request

`ActivityRequestTrigger.ClearActivity` clears the current active Activity without changing Route or Scene. The request still passes through `FrameworkRuntimeHost`, `GameFlowRuntime`, `RouteLifecycleRuntime`, and `ActivityFlowRuntime`; scene objects do not own Activity lifecycle.

Expected clear log:

```text
[Immersive Framework] Activity Request completed. source='ActivityRequestTrigger' reason='<Reason>'. Activity Flow cleared Activity '<Previous Activity>' by request.
```

### IF-FW-2T — Activity Content consumes Activity lifecycle events

ActivityContentRuntime now subscribes to the Activity lifecycle events emitted by ActivityFlowRuntime. Activity content visibility still behaves the same, but the content update path is now aligned with the canonical ActivityEntered/ActivityExited events from Foundation.Events.


### IF-FW-2U — Route Lifecycle Events

RouteLifecycleRuntime now emits canonical Route lifecycle events through `com.immersive.foundation`:

```text
RouteExitedEvent
RouteEnteredEvent
```

The Route lifecycle owner remains `RouteLifecycleRuntime`. Game Flow still accepts route requests, Scene Lifecycle still owns scene loading, and Activity Flow still owns Activity state. No consumer has been added yet; this cut only establishes the event boundary for future systems such as Activity, camera, input, save, actors, and pooling scopes.


## IF-FW-2V — Framework QA Canvas

Este corte adiciona um componente de desenvolvimento para padronizar smokes manuais:

```text
Immersive Framework > QA > Framework QA Canvas
```

O QA Canvas usa IMGUI para evitar dependência nova de UGUI no runtime do framework. Ele permite configurar listas de `Route` e `Activity` e disparar:

```text
Request Route
Request Activity
Clear Active Activity
```

O componente pode persistir entre cenas para continuar disponível após `LoadSceneMode.Single`. Ele não é parte do runtime de produto nem substitui testes automatizados; é uma superfície de QA manual para gerar logs consistentes durante o desenvolvimento.

## IF-FW-2W — QA Scenario Presets

O `Framework QA Canvas` passou a expor presets manuais padronizados para reduzir variação humana nos logs.

A pasta `Assets/ImmersiveFrameworkQA/` concentra os assets de teste manual do framework. Eles não sao assets de producao e existem somente para gerar logs padronizados de smoke.
O `Active Game Application` normal do projeto continua sendo o asset de produto do projeto; os assets QA nao devem virar boot padrao.
A `StartupScene` do projeto permanece como cena normal de produto/dev, nao como cena QA paralela.
Os nomes `QA_CanonicalRoute`, `QA_AlternateRoute`, `QA_PrimaryContentActivity` e `QA_SecondaryContentActivity` representam papéis de teste, nao nomes de gameplay.
O catálogo também pode incluir `QA_NoActivityRoute` e `QA_NoContentActivity` para testes sem Startup Activity ou sem conteúdo correspondente.

Em `Immersive Framework > QA > Framework QA Canvas`, o Inspector mostra alvos semânticos de cenário:

- `Canonical Route`
- `Alternate Route`
- `No-Activity Route`
- `Primary Activity`
- `Secondary Activity`
- `No-Content Activity`

O componente também expõe os botões:

- `Run Activity Smoke`
- `Run Route Smoke`
- `Run Clear Activity Smoke`
- `Run No-Activity Route Smoke`
- `Run No-Content Activity Smoke`
- `Run Negative Smoke`

Esses smokes executam sequências canônicas em ordem, mantendo `source='FrameworkQaCanvas'` nos requests e emitindo logs de início/fim pelo `FrameworkLogger`. O canvas continua IMGUI/OnGUI, continua manual e não adiciona UGUI, testes automatizados, event bus novo ou mudança de lifecycle.

Smokes principais:

- `Run Activity Smoke`: Secondary Activity -> Primary Activity -> Clear -> Primary Activity.
- `Run Route Smoke`: Alternate Route -> Canonical Route.
- `Run Clear Activity Smoke`: Clear -> Primary Activity -> Clear.
- `Run No-Activity Route Smoke`: solicita uma Route valida sem Startup Activity.
- `Run No-Content Activity Smoke`: solicita uma Activity valida sem binding/conteudo esperado.
- `Run Negative Smoke`: Clear -> Clear novamente, sem assumir boot QA.

O QA Canvas preserva referencias serializadas antigas com `FormerlySerializedAs`, mas os campos publicos de authoring devem ser lidos como papeis de cenario, nao como nomes de assets de gameplay.
