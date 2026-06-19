# Foundation Boundary

`com.immersive.foundation` is an internal Unity package skeleton for reusable primitives.

## Rules

- `Validation/Preconditions` is the first active participant.
- `Events` is the next active participant and stays local and instantiable.
- `Fsm` is the next active participant and remains a generic primitive without Unity lifecycle, MonoBehaviour, or automatic EventBus integration.
- A global bus, singleton bus, service locator, reflection utility, and filtered bus are out of scope for this cut.
- Keep it free of lifecycle ownership.
- Keep it free of service locator patterns.
- Keep it free of composition-root responsibilities.
- Keep it free of fallback compatibility rails.
- Keep it free of legacy migration code in this cut.
