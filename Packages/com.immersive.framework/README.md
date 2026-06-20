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

`IF-FW-3I-FIX1 - Request Trigger Foundation Events` corrects the request-trigger result boundary. `RouteRequestTrigger` and `ActivityRequestTrigger` can still be invoked by UI Buttons/UnityEvents, but request result notification is now published through `com.immersive.foundation` typed events instead of trigger-owned UnityEvents. Inspector UnityEvent usage remains isolated to explicit bridge components such as `ActivityContentLifecycleEvents` and `RouteContentLifecycleEvents`.

`IF-FW-3H - Content Lifecycle UnityEvent Bridge` adds runtime UnityEvent bridge components for scene-authored Route and Activity content. `ActivityContentLifecycleEvents` and `RouteContentLifecycleEvents` invoke no-argument UnityEvents on local enter/exit, letting authored scene objects trigger simple gameplay reactions without custom scripts, service locators, validators, new QA UI, pipeline stages, Actor, Input, Camera, Save or Pooling.

`IF-FW-3G - Content Lifecycle Dispatch Order` makes local content receiver dispatch deterministic for Route and Activity scopes. Enter callbacks are dispatched parent-to-child, while exit callbacks are dispatched child-to-parent. This lets parent/root behaviours prepare shared state before children enter, and lets children release local work before the parent/root exits. It does not change Route switching, Activity visibility, scene loading, validation, QA UI, Actor, Input, Camera, Save, Pooling or pipeline ownership.

`IF-FW-3F - Flow Request Context Surface` propagates request `source` and `reason` from Game Flow into Route/Activity content lifecycle contexts. `ActivityContentLifecycleContext` and `RouteContentLifecycleContext` now expose `Source` and `Reason`, and the Behaviour base classes expose `LifecycleSource` and `LifecycleReason`. This keeps local gameplay scripts informed about whether a lifecycle transition came from startup, QA, a trigger, or another request path without exposing runtime hosts or service locators. It does not change Route switching, Activity visibility, scene loading, validation, QA UI, Actor, Input, Camera, Save, Pooling or pipeline ownership.

`IF-FW-3E - Content Lifecycle State Surface` adds a minimal runtime state surface to Activity/Route content lifecycle. Callback contexts now expose their lifecycle phase, and `ActivityContentBehaviour` / `RouteContentBehaviour` expose current active-state helpers and last context data for gameplay scripts. This does not change Route switching, Activity visibility, scene loading, validation, QA UI, Actor, Input, Camera, Save, Pooling or pipeline ownership.

`IF-FW-3D - Content Lifecycle Behaviours` adds optional base MonoBehaviours for scene-authored content. `ActivityContentBehaviour` and `RouteContentBehaviour` implement the receiver interfaces and expose protected virtual callbacks, so gameplay scripts can participate in Activity/Route lifecycle without repeating interface boilerplate. This is an ergonomics layer only; it does not change routing, Activity visibility, scene loading, validation, QA UI, Actor, Input, Camera, Save, Pooling or pipeline ownership.

`IF-FW-3C - Content Lifecycle Receiver Safety` protects Activity and Route content receiver dispatch. If one local receiver throws, the framework logs contextual failure and continues dispatching the remaining receivers instead of aborting the Route/Activity flow.

`IF-FW-3B - Route Content Lifecycle Receivers` adds the matching runtime extension point for scene-authored Route content. Components under a `Route Content Binding` root can implement `IRouteContentLifecycleReceiver` to receive `OnRouteContentEntered` and `OnRouteContentExited` callbacks when their Route enters or exits. Route content is a Route-scoped scene boundary; it notifies local objects but does not own GameObject visibility, load scenes, start activities, spawn actors, own input, camera, save, pooling or a new pipeline.

`IF-FW-3A - Activity Content Lifecycle Receivers` adds the first runtime extension point for scene-authored Activity content. Components under an `Activity Content Binding` root can implement `IActivityContentLifecycleReceiver` to receive `OnActivityContentEntered` and `OnActivityContentExited` callbacks when their bound Activity enters or exits. The callback is local to the content root; it does not create a service locator, pipeline, actor system, input, camera, save or pooling integration.

`IF-FW-2Y - Authoring Validation Baseline` adds an editor-only authoring validation surface without changing runtime lifecycle. Project Settings and the relevant Inspectors can now report the current baseline configuration for Active Game Application, Startup Route, Primary Scene, optional Startup Activity, and open-scene Activity Content Bindings. Validation logs use `FrameworkLogger` and do not introduce Actor, Input, Camera, Save, Pooling, UGUI, or a new runtime pipeline.

`IF-FW-2W-FIX5 - QA Scenario Reset Button` extends `FrameworkQaCanvas` with an in-Play-Mode baseline reset. The reset returns the runtime to a configured baseline Route/Activity without leaving Play Mode, so the Unity Console is preserved between smoke runs. If no explicit reset Route/Activity is configured, the canvas falls back to the current Game Application's Startup Route and that Route's Startup Activity.

`IF-FW-2X - QA Smoke Result Semantics` tightens the manual smoke buttons so `QA Smoke completed` is emitted only when each scenario observes its expected request result. Preparation clears may accept the explicit `IgnoredNoActiveActivity` outcome; state-changing route/activity steps still require success.

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
7. Optionally create and assign a `Startup Activity` in the Route Inspector.
8. Return to `Project Settings > Immersive Framework` and click `Validate Authoring`.
9. Enter Play Mode.

Alternative assignment flow:

1. Select a `Game Application` asset.
2. In its Inspector, use `Set as Active Game Application`.

The framework boot should succeed once an Active Game Application, Startup Route, and Startup Route Primary Scene are assigned. If the Primary Scene is not already loaded, it must be available to Unity runtime loading, usually by being included in Build Settings.

If any required setup is missing, the framework fails fast in Play Mode. Project Settings also previews the same required-missing status before entering Play Mode. Use `Validate Authoring` to produce an explicit editor-only report for the current authoring baseline and open-scene Activity Content Bindings.

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



## Route Content Lifecycle Receivers

A component under a `Route Content Binding` root can react to the Route lifecycle by implementing:

```csharp
using Immersive.Framework.RouteLifecycle;
using UnityEngine;

public sealed class MyRouteContent : MonoBehaviour, IRouteContentLifecycleReceiver
{
    public void OnRouteContentEntered(RouteContentLifecycleContext context)
    {
        // Start local Route-scoped behavior for context.Route.
    }

    public void OnRouteContentExited(RouteContentLifecycleContext context)
    {
        // Stop or release local Route-scoped behavior for context.Route.
    }
}
```

Callback rules:

- exit for the previous Route is sent before the next Primary Scene is loaded, while previous Route scene content still exists;
- enter for the next Route is sent after the next Primary Scene is resolved and active, before the Startup Activity is started;
- receivers are discovered only under the binding root, including inactive children;
- the receiver does not own Route Lifecycle and should not request Route or Activity changes from inside the callback unless the game explicitly wants that behavior.

Use Route content for Route-scoped scene behavior such as local environment systems, music triggers, navigation roots or persistent objects that should live across multiple Activities in the same Route. This is intentionally not an Actor, Input, Camera, Save, Pooling, Addressables or reset system.

## Route Content Binding

Add `Route Content Binding` to a scene GameObject when that object belongs to a Route scope. Route Lifecycle notifies matching route content after the Route Primary Scene is resolved and notifies the previous Route content before the next Route scene is loaded. It does not automatically hide or show the root GameObject; scene loading and authored object state remain the visibility boundary for Route content.

The `Route` reference is optional. When assigned, the binding matches only that Route asset. When empty, the binding matches any active Route whose Primary Scene is the GameObject scene. Keep Route content roots broad and stable. Use `Activity Content Binding` for moment-to-moment gameplay sections inside a Route.


## Activity Content Lifecycle Receivers

A component under an `Activity Content Binding` root can react to the Activity lifecycle by implementing:

```csharp
using Immersive.Framework.ActivityFlow;
using UnityEngine;

public sealed class MyActivityContent : MonoBehaviour, IActivityContentLifecycleReceiver
{
    public void OnActivityContentEntered(ActivityContentLifecycleContext context)
    {
        // Start local content behavior for context.Activity.
    }

    public void OnActivityContentExited(ActivityContentLifecycleContext context)
    {
        // Stop or release local content behavior for context.Activity.
    }
}
```

Callback rules:

- enter is sent after the matching `Activity Content Binding` root is activated;
- exit is sent before the previous Activity binding root is deactivated;
- receivers are discovered only under the binding root, including inactive children;
- the receiver does not own Activity Flow and should not request Route or Activity changes from inside the callback unless the game explicitly wants that behavior.

This is the first implementation extension point for scene-authored Activity content. It is intentionally not an Actor, Input, Camera, Save, Pooling or reset system.

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

The trigger remains invokable from Unity UI events and validates obvious local authoring issues before submitting the request to the runtime host. Result notification is not owned by trigger UnityEvents; use the typed Foundation event subscription surface on the trigger for code-level result handling.

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
