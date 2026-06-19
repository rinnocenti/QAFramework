# Immersive Logging

Specialized package for generic logging primitives.

## Boundary

- This package is not a direct replacement for the old `DebugUtility` monolith.
- Planned participants: `LogLevel`, `LogRecord`, `ILogSink`, `ILogFormatter`, `ILogPolicy`, and an instantiable `Logger`.
- No framework bootstrap.
- No singleton requirement.
- No service locator.
- No hidden global config.
- No framework-specific diagnostics.
- No `Session`, `Route`, `Activity`, `Actor`, `Input`, `Camera`, `Save`, or `Pooling` tags.
- Inspector/profile authoring is out of scope for this skeleton.
