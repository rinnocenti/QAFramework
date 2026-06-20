# ADR-0002 — Activity Content Binding mínimo e observável

## Status

Accepted

## Contexto

O `com.immersive.framework` já possui bootstrap, Game Application, Route, Scene Lifecycle, Activity Flow, requests runtime de Route/Activity e integração de logging via `com.immersive.logging`.

A próxima necessidade funcional mínima foi tornar uma Activity ativa visível na cena sem importar a arquitetura pesada da Base 2.0 / `NewScripts`.

Na Base 2.0, conteúdo de Activity era tratado por um conjunto maior de conceitos, como discovery, contributors, inventory, setup/release, reset, runtime state e pipeline. Essa arquitetura é útil como referência conceitual de ownership, mas não deve ser copiada como skeleton inicial do novo framework.

O ADR-0001 definiu que o framework deve crescer por necessidade real, evitando criar antecipadamente pipelines, registries, descriptors, contributors e managers preventivos.

## Decisão

Criar um marcador mínimo de cena:

```text
Activity Content Binding
```

Esse componente indica que um GameObject authored diretamente na cena pertence a uma Activity específica.

Quando a Activity ativa muda, o runtime de Activity Content aplica uma regra simples:

```text
binding.Activity == activeActivity -> GameObject ativo
binding.Activity != activeActivity -> GameObject inativo
sem Activity ativa -> todos os bindings ficam inativos
```

O owner dessa aplicação é:

```text
ActivityContentRuntime
```

O owner da identidade ativa continua sendo:

```text
ActivityFlowRuntime
```

O `ActivityContentBinding` não é owner de lifecycle. Ele é apenas um marcador de authoring em cena.

## Fronteiras

`ActivityContentBinding` pode fazer:

- marcar um GameObject de cena como conteúdo de uma Activity;
- permitir que `ActivityContentRuntime` ative/desative esse GameObject;
- expor uma UX simples no Inspector.

`ActivityContentBinding` não deve fazer:

- carregar cena;
- iniciar Activity;
- solicitar Activity;
- executar spawn;
- integrar pooling;
- configurar actor;
- configurar input/camera/save;
- executar reset/release complexo;
- substituir futuro sistema de Actor, Presentation ou Activity Content avançado.

## Observabilidade

Como `ActivityContentBinding` é uma configuração scene-authored, a observabilidade precisa deixar claro o que foi avaliado.

O runtime deve emitir:

- contagem de bindings aplicados;
- quantidade ativada;
- quantidade desativada;
- quantidade inalterada;
- quantidade com Activity ausente;
- diagnóstico por binding com objeto, cena, Activity atribuída, ação e motivo;
- warning explícito para binding sem Activity atribuída.

Exemplo de resumo:

```text
Activity Content applied 2 binding(s) for Activity 'Activity 02'. activated='1' deactivated='1' unchanged='0'.
```

Exemplo de detalhe:

```text
Activity Content Binding diagnostics. activeActivity='Activity 02' observations=[object='Panel_Activity02' scene='StartupScene' assignedActivity='Activity 02' action='Activate' reason='MatchedActiveActivity'; object='Panel_Activity01' scene='StartupScene' assignedActivity='Activity 01' action='Deactivate' reason='DifferentActivity'].
```

Exemplo de warning:

```text
Activity Content Binding warning. warnings=[object='Panel_MissingActivity' scene='StartupScene' reason='MissingActivityReference'].
```

Esses diagnostics devem ser emitidos pela fronteira de logging do framework, não por `UnityEngine.Debug` direto.

## Relação com NewScripts / Base 2.0

Preservar como referência conceitual:

- Activity possui conteúdo;
- owners de lifecycle precisam ser explícitos;
- runtime state e diagnostics devem mostrar decisões relevantes;
- ausência de configuração obrigatória deve ser visível.

Descartar neste momento:

- contributor/discovery/inventory para esse caso mínimo;
- pipelines/stages para ligar/desligar conteúdo simples;
- registries globais;
- profiles de conteúdo antes de existir necessidade concreta;
- fallbacks silenciosos.

## Consequências

Benefícios:

- Activity ativa tem efeito visual real na cena;
- UX de authoring é simples;
- evita copiar a estrutura pesada do Base 2.0;
- mantém Activity Flow como owner da Activity ativa;
- mantém Activity Content Runtime como owner da visibilidade de conteúdo;
- melhora rastreabilidade com diagnostics por binding.

Custos aceitos:

- configuração pode ficar espalhada em GameObjects se usada sem disciplina;
- hierarquias com bindings aninhados ainda não têm policy própria;
- conteúdo complexo, spawn, pooling, actors e presentation precisarão de contratos futuros;
- esse binding é uma solução mínima, não o sistema final de composição de gameplay.

## Critério de aceitação

A decisão é respeitada se:

- `ActivityContentBinding` ativa/desativa apenas GameObjects de cena;
- `ActivityFlowRuntime` continua owner da Activity ativa;
- `ActivityContentRuntime` aplica visibilidade;
- logs de resumo, detalhes e warnings passam por `FrameworkLogger`;
- bindings sem Activity atribuída geram warning explícito;
- não há criação de contributor/discovery/inventory/pipeline para esse caso;
- não há Actor/Input/Camera/Save/Pooling acoplado a esse corte.


## Amendment — IF-FW-2R Authoring Guardrails

`ActivityContentBinding` now has a custom Inspector as an authoring guardrail.

The Inspector makes explicit that the binding only controls the active state of one scene-authored GameObject. It does not replace future Actor, Spawn, Pooling, Presentation, Input, Camera or Save systems.

The Inspector shows an authoring error when the binding has no Activity assigned. Runtime behavior remains unchanged: incomplete bindings are skipped and reported through framework diagnostics.

The Inspector also warns when Activity Content Bindings are nested under each other. Nested Activity content policy is intentionally not defined yet, so authored Activity content roots should remain flat for now.


## Atualização IF-FW-2T

O `ActivityContentRuntime` passou a consumir os eventos canônicos de lifecycle emitidos pelo `ActivityFlowRuntime` por meio de `Foundation.Events`. O comportamento público do `ActivityContentBinding` permanece o mesmo: ligar/desligar `GameObject` de cena conforme a Activity ativa. A mudança é apenas de ownership/integração: o conteúdo passa a reagir ao lifecycle canônico em vez de depender de chamada direta como caminho principal.

## Amendment — IF-FW-3A Activity Content Lifecycle Receivers

`ActivityContentRuntime` now dispatches local lifecycle callbacks to components under an `ActivityContentBinding` root.

A component can implement:

```text
IActivityContentLifecycleReceiver
```

to receive:

```text
OnActivityContentEntered(ActivityContentLifecycleContext context)
OnActivityContentExited(ActivityContentLifecycleContext context)
```

This keeps the Activity owner unchanged:

```text
ActivityFlowRuntime owns active Activity identity.
ActivityContentRuntime owns scene-authored content visibility and local content callback dispatch.
ActivityContentBinding remains a marker/root, not a lifecycle owner.
```

Ordering rules:

- enter callbacks are sent after the matching content root is activated;
- exit callbacks are sent before the previous Activity content root is deactivated;
- receivers are searched only below the matching binding root, including inactive children;
- callbacks are local extension points, not a service locator or global event bus.

This amendment does not add Actor, Input, Camera, Save, Pooling, reset, spawn, content profiles, inventory, discovery, pipelines or registries.


## Amendment — IF-FW-3B Route Content Lifecycle Receivers

`RouteLifecycleRuntime` now owns a minimal `RouteContentRuntime` for scene-authored Route content participation.

A scene object can use:

```text
RouteContentBinding
```

and components below that root can implement:

```text
IRouteContentLifecycleReceiver
```

to receive:

```text
OnRouteContentEntered(RouteContentLifecycleContext context)
OnRouteContentExited(RouteContentLifecycleContext context)
```

This keeps ownership narrow:

```text
RouteLifecycleRuntime owns active Route identity.
SceneLifecycleRuntime owns Primary Scene loading.
ActivityFlowRuntime owns active Activity identity.
RouteContentRuntime owns local Route content callback dispatch only.
RouteContentBinding remains a marker/root, not a lifecycle owner.
```

Ordering rules:

- previous Route content receives exit before the next Primary Scene load starts, while old scene content may still exist;
- next Route content receives enter after the next Primary Scene is resolved and active;
- Startup Activity begins after Route content enter;
- receivers are searched only below the matching Route Content Binding root, including inactive children;
- callbacks are local extension points, not a service locator or global event bus.

This amendment does not add Actor, Input, Camera, Save, Pooling, Addressables, reset, spawn, content profiles, inventory, discovery, pipelines or registries.


### IF-FW-3B-FIX1 — Route Content non-destructive lifecycle

Route Content Binding was narrowed to a non-destructive lifecycle boundary. It notifies local `IRouteContentLifecycleReceiver` components on route enter/exit, but does not automatically toggle the root GameObject active state. This avoids route-scoped content disappearing permanently when multiple route assets share the same Primary Scene or when authored content uses a different route asset than the active QA route. An empty Route reference matches by Primary Scene; an assigned Route reference matches that specific Route asset.

## Amendment — IF-FW-3C Content Lifecycle Receiver Safety

Activity and Route content receiver dispatch is protected at the framework boundary.

If a local receiver throws during one of these callbacks:

```text
OnActivityContentEntered
OnActivityContentExited
OnRouteContentEntered
OnRouteContentExited
```

the content runtime logs a contextual error and continues dispatching the remaining receivers under the same binding root.

This keeps local gameplay component failures from aborting Route or Activity flow. The framework does not hide the failure; it reports the receiver type, phase, binding object, scene and exception summary through `FrameworkLogger`.

This amendment does not add negative QA automation, validators, Inspectors, Actor, Input, Camera, Save, Pooling, Addressables, reset, spawn, content profiles, inventory, discovery, pipelines or registries.


## Amendment — IF-FW-3D Content Lifecycle Behaviours

The framework now exposes optional base MonoBehaviours for common scene-authored content scripts:

```text
ActivityContentBehaviour
RouteContentBehaviour
```

They implement the lower-level receiver interfaces:

```text
IActivityContentLifecycleReceiver
IRouteContentLifecycleReceiver
```

and expose protected virtual callbacks for derived gameplay scripts:

```text
protected virtual void OnActivityContentEntered(ActivityContentLifecycleContext context)
protected virtual void OnActivityContentExited(ActivityContentLifecycleContext context)
protected virtual void OnRouteContentEntered(RouteContentLifecycleContext context)
protected virtual void OnRouteContentExited(RouteContentLifecycleContext context)
```

The receiver interfaces remain the canonical dispatch contract. The Behaviour base classes are an ergonomics layer for normal MonoBehaviour scripts, so user-authored components do not need to repeat explicit interface implementation when all they need is a local lifecycle hook.

This amendment does not alter Route switching, Activity switching, scene loading, Activity content visibility, Route content visibility, logging policy, validation, QA UI, Actor, Input, Camera, Save, Pooling, Addressables, reset, spawn, content profiles, inventory, discovery, pipelines or registries.



## Amendment — IF-FW-3E Content Lifecycle State Surface

Decision:

- Activity and Route content lifecycle contexts expose an explicit lifecycle phase.
- `ActivityContentBehaviour` keeps a minimal local state surface:
  - `IsActivityContentActive`;
  - `HasActivityContentContext`;
  - `LastActivityContentContext`;
  - `ActiveActivity`;
  - `ActivityContentBinding`.
- `RouteContentBehaviour` keeps the equivalent Route state surface:
  - `IsRouteContentActive`;
  - `HasRouteContentContext`;
  - `LastRouteContentContext`;
  - `ActiveRoute`;
  - `RouteContentBinding`.

Rationale:

- Gameplay components should not need to maintain duplicate booleans just to know whether their content scope is active.
- The state is local to the authored component and does not introduce a global lifecycle registry or service locator.
- Exit callbacks set the local active flag to `false` before the overridable exit method runs. The exiting Activity/Route remains available through the callback context.

Boundaries:

- Does not change Activity content visibility policy.
- Does not change Route content non-destructive policy.
- Does not add Actor, Input, Camera, Save, Pooling, Addressables, spawning, inventory or lifecycle pipelines.



## Amendment — IF-FW-3F Flow Request Context Surface

Decision:

- Game Flow request metadata is propagated into local content lifecycle contexts.
- `ActivityContentLifecycleContext` exposes:
  - `Source`;
  - `Reason`.
- `RouteContentLifecycleContext` exposes the same metadata.
- `ActivityContentBehaviour` and `RouteContentBehaviour` expose convenience properties:
  - `LifecycleSource`;
  - `LifecycleReason`.

Rationale:

- Gameplay components reacting to local content lifecycle often need to know why the transition happened.
- The information already exists at the request boundary as `source` and `reason`; it should not be lost before reaching local content.
- Passing this data through immutable contexts avoids service locator access to `FrameworkRuntimeHost` or direct dependency on QA/runtime request components.

Defaults:

- Startup route boot uses `source='GameApplication'` and `reason='startup'`.
- Missing source is normalized to `Unknown`.
- Missing reason is normalized to `None`.

Boundaries:

- Does not change request acceptance/rejection semantics.
- Does not change Route content non-destructive policy.
- Does not change Activity content visibility policy.
- Does not add Actor, Input, Camera, Save, Pooling, Addressables, spawning, inventory, validators, Inspectors, service locators or lifecycle pipelines.


## Amendment — IF-FW-3G Content Lifecycle Dispatch Order

Decision:

- Activity content lifecycle receiver dispatch is deterministic by phase:
  - enter dispatch runs parent-to-child within the `ActivityContentBinding` hierarchy;
  - exit dispatch runs child-to-parent within the same hierarchy.
- Route content lifecycle receiver dispatch follows the same phase order:
  - enter dispatch runs parent-to-child within the `RouteContentBinding` hierarchy;
  - exit dispatch runs child-to-parent within the same hierarchy.

Rationale:

- Parent/root behaviours commonly prepare shared local state for child behaviours when content enters.
- Child behaviours commonly need to release local work before a parent/root behaviour tears down shared state when content exits.
- The rule is small, deterministic, and local to the binding root. It avoids introducing priority assets, registries, inspectors, validators or pipeline stages before the framework has a real need for them.

Boundaries:

- Does not add configurable priorities.
- Does not add validators, Inspectors, QA UI, registries or lifecycle pipelines.
- Does not change Activity content visibility policy.
- Does not change Route content non-destructive policy.
- Does not add Actor, Input, Camera, Save, Pooling, Addressables, spawning or inventory.

## Amendment — IF-FW-3H Content Lifecycle UnityEvent Bridge

Decision:

- Add no-argument UnityEvent bridge components for local content lifecycle:
  - `ActivityContentLifecycleEvents` under Activity content roots;
  - `RouteContentLifecycleEvents` under Route content roots.
- The bridge components inherit from the existing Behaviour base classes and invoke authored UnityEvents on enter/exit.
- The canonical lifecycle contract remains the receiver interface plus Behaviour base class. UnityEvent bridges are an authored convenience layer for simple scene reactions.

Rationale:

- The framework needs a minimal path from Game Flow / Activity Flow to scene-authored gameplay without forcing a custom script for every small reaction.
- UnityEvent bridges let simple content start/stop local effects, objectives, panels, triggers or scene-authored scripts when Route/Activity content enters or exits.
- This keeps the framework moving toward a playable local content loop instead of expanding validators, inspectors, registries or bootstrap setup.

Boundaries:

- Does not pass lifecycle context through UnityEvent arguments in this cut.
- Does not add validators, Inspectors, QA UI, registries or lifecycle pipelines.
- Does not change Activity content visibility policy.
- Does not change Route content non-destructive policy.
- Does not add Actor, Input, Camera, Save, Pooling, Addressables, spawning or inventory.

## Amendment — IF-FW-3I-FIX1 Request Trigger Foundation Events

`IF-FW-3I-FIX1` corrects the request-trigger result boundary.

Rules:

- `RouteRequestTrigger` and `ActivityRequestTrigger` remain scene-authored request boundaries only.
- The runtime owner of request execution remains `FrameworkRuntimeHost -> GameFlowRuntime -> RouteLifecycleRuntime/ActivityFlowRuntime`.
- Triggers may be invoked from Unity UI/Button UnityEvents, but they do not expose result UnityEvents as their canonical result surface.
- Request result notification is published through `com.immersive.foundation` typed events.
- Route triggers publish `RouteRequestTriggerEvent` through `SubscribeRequestEvents(...)`.
- Activity triggers publish `ActivityRequestTriggerEvent` through `SubscribeRequestEvents(...)`.
- Event payload includes phase, outcome, source, reason, message and target asset references where applicable.
- Inspector-facing UnityEvent bridges remain explicit bridge components only, such as `ActivityContentLifecycleEvents` and `RouteContentLifecycleEvents`.
- This cut does not add Actor, Input, Camera, Save, Pooling, Addressables, validators, custom Inspectors, QA UI or a new pipeline.


## Amendment — IF-FW-3J Request Trigger Event State Surface

`IF-FW-3J` keeps the `IF-FW-3I-FIX1` event boundary and adds read-only local request state to authored Game Flow triggers.

Decision:

- `RouteRequestTrigger` and `ActivityRequestTrigger` expose `IsRequestInFlight`, last event phase, last outcome, last reason, last message and convenience outcome flags.
- `ActivityRequestTrigger` also exposes whether the last request was a clear-activity request.
- State is updated before publishing `RouteRequestTriggerEvent` or `ActivityRequestTriggerEvent` through `com.immersive.foundation`.
- The canonical result notification remains the typed Foundation event.
- The trigger does not expose result UnityEvents.
- Inspector-facing UnityEvent result bridges, if needed later, must be explicit bridge components that subscribe to these typed events rather than replacing them.
- This cut does not add Actor, Input, Camera, Save, Pooling, Addressables, validators, custom Inspectors, QA UI or a new pipeline.


## Amendment — IF-FW-3K Request Trigger UnityEvent Bridges

Request trigger results remain canonical Foundation events. The framework may expose UnityEvent integration only through explicit bridge components.

Decision:

- `RouteRequestTrigger` and `ActivityRequestTrigger` publish typed Foundation events.
- `RouteRequestTriggerUnityEventBridge` and `ActivityRequestTriggerUnityEventBridge` subscribe to those events and expose Inspector callbacks.
- UnityEvent result callbacks must not be embedded directly in the request triggers.

Rationale:

- Keeps package boundaries clear.
- Preserves typed event integration for code.
- Still supports authored Inspector workflows without creating a parallel event system.
## Amendment — IF-FW-4A-R4 CameraFlow Removal

The exploratory `CameraFlow` implementation from `IF-FW-4A-R1` through `IF-FW-4A-R3` is removed from the package.

Reason:

- The removed implementation used real Unity `Camera` components inside Route/Activity content.
- `ActivityContentBinding` owns GameObject activation and can disable or destroy those camera objects before camera authority can be meaningfully resolved.
- A physical camera under content scope masks responsibility: content lifecycle decides object availability, while CameraFlow appears to decide camera selection.
- The framework must not keep this as a fallback, legacy path, or parallel lane.

Decision:

- Remove `Runtime/CameraFlow` from the active package.
- Do not keep `FrameworkCameraBinding`, `FrameworkCameraAuthority`, `FrameworkCameraRequest`, `FrameworkCameraScope`, `FrameworkCameraActivatedEvent`, or `FrameworkCameraDeactivatedEvent` as active runtime types.
- Reintroduce camera later as a clean CameraFlow cut based on a persistent output rig and semantic virtual-camera requests.
- The future canonical shape should be: persistent Unity output camera with Cinemachine Brain, route/activity/pause/presentation virtual camera requests, and a semantic priority model: `Pause > Presentation > Activity > Route > Default`.
- Audio listener ownership remains outside camera and belongs to a future AudioFlow that guarantees one active listener per session.

This removal does not add Cinemachine, AudioFlow, Actor, Input, Save, Pooling, Addressables, validators, custom Inspectors, QA UI or a new lifecycle pipeline.
