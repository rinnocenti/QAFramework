# F50B — Player Binding Authoring Editor Surface QA

## Objective

Validate the Editor-only surface for the F50A Player binding authoring validator.

## Smoke command

Run:

```text
Immersive Framework QA > Player > Run F50B Player Binding Authoring Editor Surface Smoke
```

Expected final log:

```text
[F50B_PLAYER_BINDING_AUTHORING_EDITOR_SURFACE_QA] status='Succeeded'
```

## Coverage

- Active scene validation through the package Editor utility.
- Explicit root validation.
- Selected root validation.
- Missing selected root diagnostics.
- Editor window opens.
- Passive boundary remains intact.

## Out of scope

- View binding.
- Control binding.
- Camera activation.
- Input activation.
- Movement enable/disable.
- Actor spawning.
- FIRSTGAME validation.
