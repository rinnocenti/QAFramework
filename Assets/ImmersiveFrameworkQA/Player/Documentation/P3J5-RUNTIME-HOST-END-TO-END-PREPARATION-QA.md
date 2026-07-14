# P3J.5 Runtime Host End-to-End Player Actor Preparation QA

## Setup

Outside Play Mode run:

```text
Immersive Framework/QA/Player/P3J.5 Apply Runtime Host Preparation Fixture
```

The setup is idempotent and reuses the established P3G.4 real local Player host
fixture plus the P3H.4 Actor selection/default Logical Actor assets.

## Runtime smoke

Enter Play Mode with normal Framework startup and wait for boot success. Then run:

```text
Immersive Framework/QA/Player/P3J.5 Run Runtime Host End-to-End Preparation Smoke
```

Expected:

```text
[P3J5_RUNTIME_HOST_END_TO_END_PREPARATION_SMOKE]
status='Passed'
cases='21'
```

The smoke is one-shot per Play Mode because local Player leave is outside P3J.5.
It proves:

```text
runtime-host preparation module composition
one shared Session identity
real PlayerInputManager manual join
explicit joined-host registration
join remains Actor-less
explicit default Actor selection
explicit RuntimeScopeContext ownership
Logical Actor materialization and activation
framework-generated ActorId
stable PlayerInput binding
runtime-host diagnostics
idempotent prepare
selection mutation guard while prepared
explicit release
stable host/PlayerInput/Slot preservation
selection mutation restored after release
RuntimeContent cleanup
idempotent release-all
joining gate closure
```

## Regression smokes

Run in fresh Play Mode sessions where applicable:

```text
P3G.4 Run Runtime Host Real Join Smoke
P3H.4 Run Runtime Host Actor Selection Smoke
P3J.3 Run Logical Actor Materialization Adapter Smoke
P3J.4 Run Session Actor Preparation Smoke
```
