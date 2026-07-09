# F52B — Unity PlayerInput Bridge QA

This smoke validates the explicit bridge between `PlayerControlBindingSnapshot` and a configured Unity `PlayerInput`.

## Hub button

```text
Unity PlayerInput Bridge QA
```

## Expected final line

```text
[F52B_UNITY_PLAYERINPUT_BRIDGE_QA] status='Succeeded'
```

## Validated cases

```text
component references
successful bridge
missing PlayerControl binding target
missing PlayerControl binding
missing Unity PlayerInput bridge target
missing Unity PlayerInput
PlayerSlot mismatch
clear no-op
clear after bridge
passive boundary
```

## Boundary

The smoke expects bridge evidence only:

```text
controlBinding='True'
unityPlayerInputBridge='True'
inputActivation='False'
movement='False'
actorSpawning='False'
```
