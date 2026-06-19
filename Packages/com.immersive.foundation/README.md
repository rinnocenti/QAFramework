# Immersive Foundation

Internal package skeleton for reusable primitives of the Immersive Framework.

## Boundary

- Foundation v0 is frozen with Validation, Events, and Fsm.
- Future entry rule: only generic technical primitives may enter Foundation.
- No game lifecycle, Unity authoring, ScriptableObject, scene loading, framework runtime, service locator, singleton requirement, global config, bootstrap, or Session/Route/Activity/Actor/Input/Camera/Save/Pooling/Logging dependency may enter Foundation.
- RuntimeMode / Strict-Release policy is deferred to Framework Core / Settings / Diagnostics.
- SceneComposition is deferred to Framework Core / Scene Lifecycle / Scene Loading.
- Pooling is deferred to com.immersive.pooling.
- DebugUtility / Logging is deferred to com.immersive.logging.
- SceneKeyAsset, SceneRouteId, SceneTransitionProfile, and SceneTransitionEvents stay out until public naming and Inspector UX are redesigned.
- Any module-specific resolver and any runtime config registry are excluded.
- Foundation must remain small, reusable, and free of Immersive Framework-specific semantics.
- Behavior-governing blocks belong to Framework Core.
- Specialized technical blocks become their own packages.
- Validation is the first active participant.
- Events is the next active participant and uses a local, instantiable bus.
- Fsm is the next active participant and stays generic, without Unity lifecycle or automatic EventBus integration.
- RuntimeMode remains outside Foundation v0.
- Strict/Release policy and FrameworkValidationMode belong to Framework Core, not Foundation.
- Config, registry, resolvers, and degraded diagnostics remain explicitly forbidden in Foundation.
- No global bus, singleton, service locator, reflection util, or filtered bus in this cut.
- No lifecycle ownership.
- No service locator.
- No composition root.
- No fallback rails.
- No legacy migration in this cut.
