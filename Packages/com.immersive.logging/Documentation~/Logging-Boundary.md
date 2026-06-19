# Logging Boundary

`com.immersive.logging` is a specialized package for generic logging primitives.

## Planned Scope

- `LogLevel`
- `LogRecord`
- `ILogSink`
- `ILogFormatter`
- `ILogPolicy`
- instantiable `Logger`

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
- framework-specific diagnostics
- Inspector/profile authoring
