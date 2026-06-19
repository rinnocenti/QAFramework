# Immersive Foundation

Internal package skeleton for reusable primitives of the Immersive Framework.

## Boundary

- Validation is the first active participant.
- Events is the next active participant and uses a local, instantiable bus.
- Fsm is the next active participant and stays generic, without Unity lifecycle or automatic EventBus integration.
- No global bus, singleton, service locator, reflection util, or filtered bus in this cut.
- No lifecycle ownership.
- No service locator.
- No composition root.
- No fallback rails.
- No legacy migration in this cut.
