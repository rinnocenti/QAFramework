# Player Session QA — R4 reconciliation

R4 supersedes these historical Player Session documents:

- `QA-PLAYER-SURFACE-01-2026-08-08.md`
- `QA-PLAYER-SURFACE-02-2026-08-08.md`
- `QA-PLAYER-SURFACE-CERTIFICATION-2026-08-08.md`
- the Player Session portions of `QA-SMOKE-CONSOLIDATION-AUDIT.md`

They record the pre-R2 Capacity API and are evidence only. They must not be
used as current QA instructions.

## Current contract

`PlayerSessionProfile` is the single authored initial configuration surface:

```text
Supported Slots
Initial Joining
Host Provisioning
Actor Resolution
```

The QA regression set proves profile validation and immutable effective
configuration, canonical Supported Slot order, Joining Closed, first available
join, `RejectedNoAvailableSlot`, uniform Scene Provided/Manager Provisioned
hosting, and distinct Actor Resolution policies.

For Manager Provisioned setup, the serialized `PlayerInputManager` limit must
equal `PlayerSessionProfile.SupportedSlotCount`. A divergent runtime manager is
rejected explicitly by the framework bootstrap; it is not a Player Session
Capacity feature.

## Removed proof vocabulary

Do not add active QA proof, menu labels, or setup instructions for
`PlayerProvisioningProfile`, `PlayerSlotProvisioningOverride`, Initial/Current/
Dynamic Capacity, `SetCapacity`, `SetDynamicCapacity`, or
`RejectedCapacityReached`.
