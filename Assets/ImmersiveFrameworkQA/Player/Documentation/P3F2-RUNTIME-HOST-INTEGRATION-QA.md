# P3F.2 — Runtime Host Integration QA

## Purpose

Prove that framework boot creates exactly one Session Player participation runtime on the persistent `FrameworkRuntimeHost` object.

## Run

1. Use Framework Play Mode startup, not Current Scene Only.
2. Enter Play Mode and wait for `Boot succeeded`.
3. Run:

```text
Immersive Framework > QA > Player > P3F.2 Run Runtime Host Integration Smoke
```

## Expected

```text
[P3F2_RUNTIME_HOST_INTEGRATION_SMOKE] status='Passed' cases='12'
```

The smoke is read-only. It does not open joining, reserve Slots or change capacity.

## Coverage

```text
runtime host resolved
exactly one host-scoped module
same GameObject/lifetime as host
GameApplication resolved
Session context initialized
configured roster copied
initial capacity matches roster
joining starts closed
all Slots start Available
configured order preserved
repeated snapshot access is stable
context remains plain C#
```
