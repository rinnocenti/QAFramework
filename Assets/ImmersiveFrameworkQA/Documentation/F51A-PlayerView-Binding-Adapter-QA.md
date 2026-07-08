# F51A — PlayerView Binding Adapter QA

## Objective

Validate the first explicit PlayerView binding adapter contract.

## Scope

- Active PlayerView binding to an explicit target.
- Missing readiness failure.
- Not-ready summary failure.
- Inactive PlayerView rejection.
- Missing target failure.
- Explicit no-op clear.
- Clear after bind.
- Passive boundary preservation.

## Smoke

Create/refresh the Hub and Player scenes, then run:

```text
PlayerView Binding Adapter QA
```

Expected log:

```text
[F51A_PLAYERVIEW_BINDING_ADAPTER_QA] status='Succeeded'
```

## Boundary

The successful bind result may report `viewBinding='True'`, but must keep:

```text
controlBinding='False'
cameraActivation='False'
inputActivation='False'
movement='False'
actorSpawning='False'
```

No FIRSTGAME validation is part of this cut.
