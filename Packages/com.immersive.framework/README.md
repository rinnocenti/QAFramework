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

## Current cut

`IF-FW-2W-FIX5 - QA Scenario Reset Button` extends `FrameworkQaCanvas` with an in-Play-Mode baseline reset. The reset returns the runtime to a configured baseline Route/Activity without leaving Play Mode, so the Unity Console is preserved between smoke runs. If no explicit reset Route/Activity is configured, the canvas falls back to the current Game Application's Startup Route and that Route's Startup Activity.

`IF-FW-2W - QA Scenario Presets` extends `FrameworkQaCanvas` with canonical manual smoke buttons and semantic scenario targets:

- `Run Activity Smoke`, `Run Route Smoke`, `Run Clear Activity Smoke`, `Run No-Activity Route Smoke`, `Run No-Content Activity Smoke` and `Run Negative Smoke`;
- semantic scenario targets such as `Canonical Route`, `Alternate Route`, `No-Activity Route`, `Primary Activity`, `Secondary Activity` and `No-Content Activity`;
- dedicated QA assets live under `Assets/ImmersiveFrameworkQA/` and are used as scenario targets instead of replacing the project boot;
- ordered smoke logs for repeatable manual QA;
- the canvas stays IMGUI-based and continues to avoid UGUI, automated tests and new event-bus infrastructure.

`IF-FW-2S - Activity Lifecycle Events` adds canonical Activity enter/exit lifecycle events on top of the existing Activity Flow runtime:

- `ActivityFlowRuntime` emits `ActivityEnteredEvent` and `ActivityExitedEvent` through `Foundation.Events`;
- start, switch and clear transitions are surfaced as lifecycle events;
- `ActivityContentBinding` and the existing Activity diagnostics remain unchanged.

`IF-FW-2R - Activity Content Binding Authoring Guardrails` improves the Inspector for scene-authored Activity content without changing runtime behavior.

This cut adds:

- a custom Inspector for `ActivityContentBinding`;
- an authoring error when the binding has no Activity assigned;
- a clear explanation that the binding only controls GameObject active state;
- warnings for nested Activity Content Binding hierarchies, because nested content policy does not exist yet.

`IF-FW-2Q - Activity Content Binding Observability` improves diagnostics for scene-authored Activity content without changing behavior.

This cut adds:

- per-binding Activity Content diagnostics with object name, scene, assigned Activity, action and reason;
- warning diagnostics for bindings missing an Activity reference;
- summary counts for activated, deactivated, unchanged and missing-activity bindings;
- documentation/ADR updates that define `ActivityContentBinding` as a minimal scene-authored marker, not a replacement for future Actor, Spawn, Pooling or Presentation systems.

`IF-FW-2P - Activity Content Binding` added the first scene-authored Activity content boundary:

- `ActivityContentBinding` as a MonoBehaviour for GameObjects that should be active for one Activity;
- `ActivityContentRuntime` as the owner that applies bindings when Activity changes or clears;
- Activity diagnostics that report content binding application when bindings exist.

It still does not add actors, input, camera, save, pause, pooling, route-level activity transition policy, spawning, or content loading. Activity content is only visibility control for scene-authored GameObjects.

`IF-FW-2O - Runtime Activity Clear Request` added an explicit runtime request for clearing the active Activity without switching Route.

`IF-FW-2M - Activity Flow Active State` made `ActivityFlowRuntime` the owner of the active Activity identity and records Activity transitions in route diagnostics.

`IF-FW-2L - Startup Activity Contract` introduced the first Activity authoring asset and lets a Route optionally start one Activity after its Primary Scene is resolved.

`IF-FW-2K - Framework Logging Integration` added a framework-owned logging boundary on top of `com.immersive.logging`:

- `Bootstrap`, `FrameworkRuntimeHost` and `RouteRequestTrigger` no longer call `Debug.Log` directly;
- the framework keeps the same diagnostic text, but emits it through `FrameworkLogger`;
- `FrameworkRuntimeHost` remains the owner of the final route request log;
- there is still no global logger, service locator, or diagnostics UI.

`IF-FW-2J — Route Request Diagnostics Ownership` remains part of the history below.

`IF-FW-2J — Route Request Diagnostics Ownership` moves route request completion diagnostics to the persistent Application Runtime host and records request source/reason:

- `RouteRequestTrigger` can still be added to a GameObject and wired to UnityEvents/UI Buttons;
- the trigger now only submits the request and performs local preflight checks;
- `FrameworkRuntimeHost` owns the final success/warning/error log for route requests;
- request diagnostics include `source` and `reason`;
- `GameFlowRuntime` accepts or rejects the request;
- `RouteLifecycleRuntime` starts/switches the requested Route;
- `SceneLifecycleRuntime` loads/activates the target Route primary scene.

This is intentionally not a broad service locator. User-authored objects still do not see pipelines, stages, commands, facts or snapshots. The public authoring concept is only: request this Route.

Earlier cuts:

- `IF-FW-2R — Activity Content Binding Authoring Guardrails`: adds Inspector guardrails for missing Activity references and nested binding hierarchy.
- `IF-FW-2Q — Activity Content Binding Observability`: improves Activity Content diagnostics without changing content behavior.
- `IF-FW-2P — Activity Content Binding`: adds scene-authored Activity content visibility binding.
- `IF-FW-2O — Runtime Activity Clear Request`: added explicit runtime clear for active Activity.
- `IF-FW-2M — Activity Flow Active State`: made Activity Flow own the active Activity identity and report start/switch/clear/keep diagnostics.
- `IF-FW-2L — Startup Activity Contract`: introduced `ActivityAsset`, optional `Startup Activity` on Route, and minimal `ActivityFlowRuntime`.
- `IF-FW-2I — Route Switch Ownership`: moved active Route ownership to Route Lifecycle and made switch diagnostics report previous/next Route.
- `IF-FW-2H — Runtime Route Request Trigger`: added the first scene-authored route request boundary.
- `IF-FW-2G — Route Lifecycle Runtime Owner`: separates Route startup from Game Flow through `RouteLifecycleRuntime`.
- `IF-FW-2F — Application Runtime Host`: introduced the persistent `Immersive Framework Runtime` object and `FrameworkRuntimeHost`.
- `IF-FW-2E — Startup Primary Scene Single Load`: makes the Startup Route primary scene replace the currently loaded scene when the framework has to load it.
- `IF-FW-2D — Editor Play Mode Startup`: added an editor-only switch to run the current scene without framework boot.
- `IF-FW-2C — Scene Lifecycle Primary Scene Load`: introduced the first Scene Lifecycle owner and primary scene loading.
- `IF-FW-2B — Route Primary Scene Contract`: introduced `Primary Scene` on Route and required it in boot validation.
- `IF-FW-2A — Startup Route Contract`: introduced `Route`, `Startup Route`, boot validation for startup route, and minimal `GameFlowRuntime`.
- `IF-FW-1E — Active Application Assignment UX`: lets the `Game Application` Inspector show and set project assignment.
- `IF-FW-1D — Project Settings UX`: improves the Immersive Framework Project Settings page.
- `IF-FW-1C — Game Application Inspector UX`: adds a custom Inspector for the Game Application asset.
- `IF-FW-1B — Shared Boot Validation`: shares runtime/editor boot validation rules.
- `IF-FW-1A — Minimal Bootstrap`: introduced Game Application, Project Settings, Validation Mode, internal bootstrap, and minimal boot diagnostics.

This cut still intentionally does not introduce Actor, Input, Camera, Save, Pooling integration, module graph, route transition policies, loading screen, unload policy, or advanced diagnostics.

## First setup

1. Open `Project Settings > Immersive Framework`.
2. Click `Create and Assign Game Application`.
3. Select the created `Game Application` asset.
4. In the `Startup` section, click `Create and Assign Startup Route`.
5. Select the created `Route` asset.
6. Assign a `Primary Scene` in the Route Inspector.
7. Optionally create and assign a `Startup Activity` in the Route Inspector.
8. Enter Play Mode.

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
