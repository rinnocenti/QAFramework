# Logging Boundary

`com.immersive.logging` is a specialized package for generic logging primitives.

## Logging v0 Freeze

- Runtime primitives are frozen: `LogLevel`, `LogRecord`, `ILogSink`, `ILogFormatter`, `ILogPolicy`, `Logger`.
- Basic runtime implementations are frozen: `PlainTextLogFormatter`, `AllowAllLogPolicy`, `RejectAllLogPolicy`, `MinimumLogLevelPolicy`.
- `UnityConsoleLogSink` is frozen as the current optional Unity adapter in a separate assembly.

## Active Runtime Primitives

- `LogLevel`
- `LogRecord`
- `ILogSink`
- `ILogFormatter`
- `ILogPolicy`
- `PlainTextLogFormatter`
- `AllowAllLogPolicy`
- `RejectAllLogPolicy`
- `MinimumLogLevelPolicy`
- instantiable `Logger`

`Logger` is local and instantiable, not global.

`Immersive.Logging.Runtime` stays pure (`noEngineReferences: true`) and does not reference `UnityEngine`.

`UnityConsoleLogSink` is an optional Unity adapter in a separate assembly.

Example conceptual composition: `Logger` + `UnityConsoleLogSink` + `PlainTextLogFormatter` + `MinimumLogLevelPolicy`.

Concrete config/profile/Inspector workflows, dedupe/throttle, framework diagnostics and framework policy layers are deferred to later cuts.

## Entry Rule

- Only generic logging concerns enter this package.
- No `Session`, `Route`, `Activity`, `Actor`, `Input`, `Camera`, `Save`, or `Pooling` semantics.
- No framework bootstrap or service locator.
- No mandatory singleton or hidden global config.
- No mandatory `ScriptableObject` configuration.
- No framework-specific diagnostics.
- No Strict/Release policy.
- No degraded mode policy from framework core.

## Explicitly Out of Scope

- `DebugUtility` monolith
- `HardFailFastH1`
- `DegradedModeReporter`
- `RuntimeModeConfig`
- `RuntimeConfigRegistry`
- mandatory `LoggingConfigAsset`
- automatic bootstrap
- mandatory singleton
- service locator
- hidden global config
- tags specific to `Session`, `Route`, `Activity`, `Actor`, `Input`, `Camera`, `Save`, or `Pooling`
- config/profile/Inspector authoring
- dedupe/throttle
- framework-specific diagnostics
- Strict/Release policy
- degraded mode from framework core
