# Authoring

This page describes the current authoring model for Unity users.

## Extension authoring boundaries

Bridge, Adapter, Validator/Evidence and Consumer are separate responsibilities.

- Bridge reads Inspector fields, validates authoring locally, calls an explicit runtime boundary and exposes diagnostics.
- Adapter executes one concrete Unity or subsystem side effect and returns local evidence.
- Validator/Evidence checks readiness, inputs or results and reports structured evidence; it does not apply side effects.
- Consumer requests a capability and handles unavailable or failed capability explicitly.

A bridge should not execute multi-stage orchestration, own rollback, discover services implicitly or hide failed side effects behind fallback behavior.

## Adapter authoring pattern

Use Loading as the reference for new Surface/Adapter authoring:

- Define the domain Surface request/result/status before adding a Unity component.
- Make the adapter execute one local side effect, such as changing a CanvasGroup, GameObject active state, image, progress widget or another explicit subsystem.
- Return explicit domain adapter evidence for success, skipped/no-op, unsupported request and local failure.
- Keep lifecycle, route/activity ownership, policy decisions and multi-step orchestration outside the adapter.
- Do not silently replace missing required references. Required missing configuration must fail visibly.
- Do not create a shared `IFrameworkAdapter`, `FrameworkSurface`, universal status enum or generic result container for authoring convenience.

## Game Application asset

Use `GameApplicationAsset` as the root application asset.

Author:

- Startup route.
- `UIGlobal` scene policy.
- `UIGlobal` scene reference when required.

The Game Application is the place to configure app/session visual surface availability, not individual gameplay objects.

## Route authoring

Use `RouteAsset` for route-level navigation.

Route content can use:

- route content bindings
- route content lifecycle receivers
- route content scene entries/profiles
- route-owned ContentAnchor declarations

Route-owned content should not own global Pause, Loading or Transition presentation.

## Activity authoring

Use `ActivityAsset` for activity-level flow inside a route.

Activity content can use:

- activity content profiles
- activity content scene entries
- activity content lifecycle receivers
- activity visual transition mode
- activity-owned ContentAnchor declarations

Activity visual mode can request shared Transition/Loading behavior, but `UIGlobal` still owns the actual shared surfaces.

## UIGlobal authoring

A canonical `UIGlobal` scene may contain:

- `UnityLoadingSurfaceAdapter`
- `UnityFadeCurtainEffectAdapter`
- `UnityPauseResidentSurfaceAdapter`
- Pause input bridge objects, when the project wants shared Pause input wiring

Keep the scene focused on app/session shared presentation. Do not put route-specific gameplay content in `UIGlobal`.

## Pause authoring

Standard Pause presentation:

1. Create a resident Pause UI hierarchy in `UIGlobal`.
2. Add `UnityPauseResidentSurfaceAdapter`.
3. Let logical Pause state show/hide that resident hierarchy.

`UnityPauseResidentSurfaceAdapter` is the supported authoring path for current Pause presentation.

Pause visual/materialization through `PauseVisualSurfaceAuthoring`, RuntimeContent and ContentAnchor is experimental/frozen. It is useful as explicit evidence and QA proof material, but it is not the default production path and should not be used to define new Surface/Adapter contracts yet.

Do not use Pause visual authoring to decide InputMode, `Time.timeScale`, route/activity lifecycle, player spawning, actor spawning or global UI creation.

## Pause input authoring

Use:

- `PauseInputModeUnityPlayerInputRuntimeBridge`
- `PauseInputActionRuntimeBridgeTrigger`

Wire the trigger to the Unity Input System action evidence and to the runtime bridge. The runtime bridge applies the logical Pause request, maps the result to `InputMode`, then applies the explicit Unity `PlayerInput` operation.

This bridge is an authoring/runtime wrapper for the current Pause input path. Do not copy it as the pattern for new adapters; future runtime work must keep multi-stage apply orchestration in a non-MonoBehaviour boundary and keep `PlayerInput` side effects behind an explicit adapter.

Do not author new direct Pause input through the retired adapter path.

## Trigger authoring

Triggers are scene-authored wrappers for explicit submission. They may read Inspector fields, validate local authoring evidence, call one domain boundary and expose last-result diagnostics.

The shared FlowTrigger helper is not a base trigger, not a `MonoBehaviour`, not a request envelope and not a domain policy layer. It only stores local submission state, source/reason/message and issue counts for triggers that opt in.

Domain logic stays in the trigger or the owning domain:

- Route triggers choose route requests.
- Activity triggers choose activity requests.
- Pause triggers choose Pause intent.
- Pause InputAction triggers validate action evidence and delegate to the Pause/InputMode bridge.

Do not use the helper to create a universal trigger, event bus, command bus, lifecycle dispatcher or fallback path.

## RuntimeContent and ContentAnchor authoring

For materialized content:

1. Declare RuntimeContent identity and scope.
2. Declare ContentAnchor ownership and anchor id/kind.
3. Use `UnityContentAnchorMaterializationBridge` for authored opt-in materialization.
4. Use `UnityContentAnchorMaterializationBridgeSet` for batch authoring and diagnostics.
5. Use explicit release/composite release paths for cleanup.

Materialization is explicit. Route/Activity auto-materialization is not a package usage assumption.
