# IF-FW-F25I — Activity Operation Validator Guards

## Status

Superseded by `IF-FW-F25I1` for visual-mode scope.

## Correction

The original F25I validator was too strict because it treated Activity content scene declarations as requiring `FadeWithLoading`.

That is no longer the canonical rule.

Correct rule:

```text
Activity scene load/release side-effects are allowed in Seamless, Fade and FadeWithLoading.
The selected ActivityVisualTransitionMode controls presentation only.
```

Meaning:

```text
Seamless       = no TransitionSurface, no LoadingSurface
Fade           = TransitionSurface, no LoadingSurface
FadeWithLoading = TransitionSurface + LoadingSurface when scene side-effects exist
```

The framework must not silently upgrade `Seamless` or `Fade` into `FadeWithLoading`.

## Preserved validator errors

F25I/F25I1 still keep structural profile errors:

- required Activity content scene entry without scene reference;
- cached scene name without scene path;
- duplicate content id inside one Activity Content Profile.

## Superseded rule

The following rule was rejected:

```text
Activity Content Profile with scenes requires FadeWithLoading.
```

Reason:

```text
It prevents the intended use case of loading a sequence of Activities without a curtain/loading screen.
```
