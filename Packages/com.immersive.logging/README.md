# Immersive Logging

Specialized package for generic logging primitives.

## v0 Freeze

- `Logging v0` is frozen with pure runtime primitives, basic policies, a plain text formatter, an instantiable `Logger`, and a separate Unity console adapter.
- `Immersive.Logging.Runtime` stays pure and does not reference `UnityEngine`.
- `Immersive.Logging.Unity` is the only current Unity adapter, and it must not contaminate the pure runtime assembly.

## Boundary

- This package is not a direct replacement for the old `DebugUtility` monolith.
- Active API: `LogLevel`, `LogRecord`, `ILogSink`, `ILogFormatter`, `ILogPolicy`, `Logger`, `PlainTextLogFormatter`, `AllowAllLogPolicy`, `RejectAllLogPolicy`, `MinimumLogLevelPolicy`, and `UnityConsoleLogSink`.
- `Logger` is local and instantiable, not global.
- Example composition: `Logger` + `UnityConsoleLogSink` + `PlainTextLogFormatter` + `MinimumLogLevelPolicy`.
- Configuration, profile and Inspector workflows are deferred to later cuts.
- No framework bootstrap.
- No singleton requirement.
- No service locator.
- No hidden global config.
- No framework-specific diagnostics.
- No `Session`, `Route`, `Activity`, `Actor`, `Input`, `Camera`, `Save`, or `Pooling` tags.
- No `ScriptableObject`-based logging configuration in v0.
- No dedupe, cache or throttle layer in v0.
- No logger global.
