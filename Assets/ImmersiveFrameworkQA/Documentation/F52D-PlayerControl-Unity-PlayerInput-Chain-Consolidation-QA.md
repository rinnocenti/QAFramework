# F52D — PlayerControl Unity PlayerInput Chain Consolidation QA

Status: **documentation-only**

## Purpose

F52D records the accepted QA evidence for the PlayerControl Unity PlayerInput chain and marks the chain ready for handoff to a FIRSTGAME usability proof.

## Evidence chain

Use the existing smokes as evidence:

```text
F52A_PLAYERCONTROL_BINDING_ADAPTER_QA
F52B_UNITY_PLAYERINPUT_BRIDGE_QA
F52C_UNITY_PLAYERINPUT_ACTIVATION_QA
```

## Required final state

The accepted final PlayerControl input state after F52C is:

```text
controlBinding='True'
unityPlayerInputBridge='True'
inputActivation='True'
viewBinding='False'
cameraActivation='False'
movement='False'
actorSpawning='False'
```

## QA rule

F52D does not add a new Unity scene or Hub button. It is accepted when:

```text
1. F52A smoke remains PASS.
2. F52B smoke remains PASS.
3. F52C smoke remains PASS.
4. The package documentation is imported without code changes.
```

## Boundary reminder

Do not treat F52D as permission to add movement, gameplay command execution or new framework contracts inside FIRSTGAME. FIRSTGAME may prove usability of the accepted chain only after QA remains clean.
