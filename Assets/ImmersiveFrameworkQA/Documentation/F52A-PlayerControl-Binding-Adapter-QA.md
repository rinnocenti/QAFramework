# F52A — PlayerControl Binding Adapter QA

Status: QA smoke for `PlayerControlBindingAdapter`, replanned from the F51D safe point.

## Scene

```text
Assets/ImmersiveFrameworkQA/Player/Scenes/QA_PlayerControlBindingAdapter.unity
```

## Hub entry

```text
PlayerControl Binding Adapter QA
```

## Expected log

```text
[F52A_PLAYERCONTROL_BINDING_ADAPTER_QA] status='Succeeded'
```

## Coverage

- component references
- successful PlayerControl binding
- missing readiness summary
- not-ready control binding summary
- inactive PlayerControl rejected
- missing binding target
- clear no-op
- clear after bind
- passive boundary

## Boundary

Successful F52A binding may report:

```text
controlBinding='True'
```

It must still report:

```text
viewBinding='False'
cameraActivation='False'
inputActivation='False'
movement='False'
actorSpawning='False'
```


## Roadmap guardrail

F52A proves only PlayerControl binding evidence. It must not activate Unity Input System, movement, gameplay command execution or actor spawning.
