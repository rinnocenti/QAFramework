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

`IF-FW-2L - Startup Activity Contract` introduces the first Activity authoring asset and lets a Route optionally start one Activity after its Primary Scene is resolved.

This cut adds:

- `ActivityAsset` as a public authoring asset;
- optional `Startup Activity` on `RouteAsset`;
- minimal `ActivityFlowRuntime` owned separately from Route and Scene lifecycle;
- route diagnostics append `Activity Flow started Activity '...'` only when a Startup Activity is assigned.

It still does not add activity content, actors, input, camera, save, pause, pooling, activity transitions, or activity content loading.

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

This cut still intentionally does not introduce Activity content, Actor, Input, Camera, Save, Pooling integration, module graph, route transition policies, loading screen, unload policy, or advanced diagnostics.

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
