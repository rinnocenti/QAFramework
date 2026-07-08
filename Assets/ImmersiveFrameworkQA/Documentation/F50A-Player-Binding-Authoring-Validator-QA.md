# F50A — Player Binding Authoring Validator QA

## Objective

Validate that the package-owned Player binding authoring validator can inspect an authored QA hierarchy and reject missing or invalid evidence before any real binding lifecycle exists.

## Scene

```text
Assets/ImmersiveFrameworkQA/Player/Scenes/QA_PlayerBindingAuthoringValidator.unity
```

## Hub button

```text
Player Binding Authoring Validator QA
```

## Expected smoke

```text
[F50A_PLAYER_BINDING_AUTHORING_VALIDATOR_QA] status='Succeeded'
```

## Coverage

```text
component references
valid authored hierarchy
missing validation root
missing PlayerSlotDeclaration
missing PlayerSlotOccupancy
missing ActorReadinessBehaviour
missing PlayerEntryBehaviour
missing PlayerViewBehaviour
missing PlayerControlBehaviour
topology issue propagation
diagnostic report generation
passive boundary confirmation
```

## Boundary

This QA must continue to report no view binding, no control binding, no camera activation, no input activation, no movement and no actor spawning.
