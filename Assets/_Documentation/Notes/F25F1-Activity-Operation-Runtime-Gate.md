# IF-FW-F25F1 - Activity Operation Runtime Gate

## Status
Implemented as a guarded bridge after F25F executor preview.

## Purpose

F25F1 starts consuming `ActivityOperationPlan` in the real Activity request path without moving Activity scene execution into the final executor yet.

The cut prevents the invalid behavior documented by F25R:

```text
Activity scene load/release side-effect
opens LoadingSurface directly
without being requested by the Activity operation visual mode
```

F25I1 later corrected the visual-mode scope: `Seamless` and `Fade` are valid with Activity scene side-effects; they simply do not request LoadingSurface.

## Runtime rule

Before an Activity request or Activity clear executes transition, loading hooks, scene composition or scene release, `GameFlowRuntime` previews the operation through `ActivityOperationPlan`.

If the preview is blocked, the Activity operation fails explicitly and performs no Activity lifecycle side-effects.

Blocking remains for declaration/configuration failures. After F25I1, the following are valid presentation choices:

- `Seamless + scene load/release side-effect`: execute without TransitionSurface or LoadingSurface.
- `Fade + scene load/release side-effect`: execute with TransitionSurface and without LoadingSurface.
- `FadeWithLoading + scene side-effect`: execute with TransitionSurface and LoadingSurface when requested by the operation.

## Loading gate

`FrameworkRuntimeHost` no longer opens Activity `LoadingSurface` from legacy load/release probes.

For Activity request/clear, Loading is shown only when the preview plan is valid and reports:

```text
RequiresLoadingSurface = true
```

This means `AlreadyLoaded` diagnostics and `Seamless`/`Fade` Activity scene side-effects do not open LoadingSurface implicitly.

## Scope

Changed:

- `FrameworkRuntimeHost` uses Activity operation preview to decide Activity loading hooks.
- `GameFlowRuntime` blocks invalid Activity request/clear plans before transition/loading/lifecycle execution. After F25I1, visual side-effects alone are not invalid.
- `RouteLifecycleRuntime` exposes Activity operation preview from `ActivityFlowRuntime`.
- Minor compile cleanup from the F25F preview files.

Not changed:

- Route startup Activity still uses the legacy startup path.
- Activity scene execution/release still runs through the F25C-D4 experimental runtime.
- Activity scene tracking is still the loose tracked-list model.
- Validators and Inspector warnings are not updated yet.
- No Addressables, progress aggregation, coroutine, `Task.Delay`, Camera, Input, Audio, Player, Pause or Save work.

## Expected behavior

A `Seamless` Activity with Activity scene load/release should execute without transition and without LoadingSurface.

A `FadeWithLoading` Activity with Activity scene load/release should keep the Activity LoadingSurface path available.

A `Fade` Activity with or without Activity scene load/release should remain a visual fade without LoadingSurface.

## Follow-up

F25G should unify Route startup Activity under the same Activity operation planning/execution path.
