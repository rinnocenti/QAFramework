# IF-FW-F25F1 - Activity Operation Runtime Gate

## Status
Implemented as a guarded bridge after F25F executor preview.

## Purpose

F25F1 starts consuming `ActivityOperationPlan` in the real Activity request path without moving Activity scene execution into the final executor yet.

The cut prevents the invalid behavior documented by F25R:

```text
Seamless Activity operation
+
Activity scene load/release side-effect
=
LoadingSurface appears without fade
```

## Runtime rule

Before an Activity request or Activity clear executes transition, loading hooks, scene composition or scene release, `GameFlowRuntime` previews the operation through `ActivityOperationPlan`.

If the preview is blocked, the Activity operation fails explicitly and performs no Activity lifecycle side-effects.

Blocking examples:

- `Seamless + scene load/release side-effect`
- `Fade + scene load/release side-effect`

`FadeWithLoading + scene side-effect` remains valid and may request `LoadingSurface`.

## Loading gate

`FrameworkRuntimeHost` no longer opens Activity `LoadingSurface` from legacy load/release probes.

For Activity request/clear, Loading is shown only when the preview plan is valid and reports:

```text
RequiresLoadingSurface = true
```

This means `AlreadyLoaded` diagnostics and invalid `Seamless` plans do not open LoadingSurface.

## Scope

Changed:

- `FrameworkRuntimeHost` uses Activity operation preview to decide Activity loading hooks.
- `GameFlowRuntime` blocks invalid Activity request/clear plans before transition/loading/lifecycle execution.
- `RouteLifecycleRuntime` exposes Activity operation preview from `ActivityFlowRuntime`.
- Minor compile cleanup from the F25F preview files.

Not changed:

- Route startup Activity still uses the legacy startup path.
- Activity scene execution/release still runs through the F25C-D4 experimental runtime.
- Activity scene tracking is still the loose tracked-list model.
- Validators and Inspector warnings are not updated yet.
- No Addressables, progress aggregation, coroutine, `Task.Delay`, Camera, Input, Audio, Player, Pause or Save work.

## Expected behavior

A `Seamless` Activity with Activity scene load/release should now fail explicitly instead of showing Loading without fade.

A `FadeWithLoading` Activity with Activity scene load/release should keep the Activity LoadingSurface path available.

A `Fade` Activity with no Activity scene load/release should remain a visual fade without LoadingSurface.

## Follow-up

F25G should unify Route startup Activity under the same Activity operation planning/execution path.
