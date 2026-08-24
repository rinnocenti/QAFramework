# Player Physical Lifetime Recertification QA

Status: **PENDING UNITY COMPILE / PLAY MODE CERTIFICATION**  
Date: **2026-08-14**

## Contract under proof

```text
Session owns the admitted physical Player after successful admission.
Activity owns only its contextual representation.
```

The certification separates the physical identity from the contextual Activity
occurrence. An ordinary Activity change must retain the same Host/Actor entity and
pose while producing new contextual readiness/gameplay evidence.

## QA reconciliation matrix

| Surface | Disposition | Current proof |
|---|---|---|
| `QaAdr19SessionParticipationAuthorityRegression` | KEEP | Session Join and Slot authority remain valid. |
| `QaAdr19SessionLifetimeMatrixRegression` | API-MIGRATE | Historical Activity-exit/termination coverage remains useful but is not the physical-transition certificate. |
| `QaP3M5BRouteTransitionAndNegativeMatrixSmoke` | API-MIGRATE | Scene-Provided adoption is Session-owned; A→B/A re-entry assert same Actor/RuntimeContent identity, entity references and pose while contextual admission changes. |
| `QaPlayerProvisioningPublicSurfaceRegression` | API-MIGRATE | Manager-Provisioned exit/re-entry now captures and preserves the exact Actor entity and pose. |
| `QaPlayerProvisioningPublicSurfaceNegativeRegression` | API-MIGRATE | Exit no longer expects physical Actor destruction. |
| `QaSessionPlayerLeavePublicManagerRegression` | KEEP / FULL-QA | ADR020-H retains public occurrence-safe Leave proof and is now a Full Player QA phase. |
| `QaAdr21ActivityPlayerInitialPlacementRegression` | SUPERSEDED | Historical Activity-owned Initial Placement 9/9 remains documented. It is not a current Model B owner. Current owners: `QaAdr21RoutePlayerSpatialEntryRegression` and `QaAdr21ActivityPlayerRelocationRegression`. |
| Old generated `Player/Scripts/Runtime/*.cs` entries | RETIRE | They are absent source files retained only by the generated `.csproj`; Unity must regenerate project files rather than recreating retired QA fixtures. |

## Covered by this cut

- Manager-Provisioned Activity exit and re-entry preserve the same physical Actor
  reference, entity identity and pose.
- Manager-Provisioned re-entry still requires a newer contextual Activity occurrence.
- Scene-Provided successful adoption is asserted as `FrameworkOwned` / Session-owned.
- Scene-Provided Route A → B and Route A re-entry assert retained physical
  `ActorId`, `RuntimeContentIdentity`, Host/Actor references, entity identities and pose.
- The supplying Route A Activity scene can unload without destroying the adopted
  Scene-Provided Player; its former `SceneLocal` admission may not remain active.
- Full Player QA now includes the public ADR020-H Manager Leave phase.

## Deliberate non-claims

- No Unity compile, import or Play Mode smoke has run for this record.
- ADR020-H still certifies Manager-Provisioned Leave only. A Scene-Provided Leave
  recertification cannot be authored solely through the current public
  `SceneLocalPlayerAdmissionAuthoring` surface: it exposes admission/release but not
  the public exact-occurrence Session Leave command. Do not bypass that boundary with
  reflection or internal runtime-module access.
- Historical ADR-021 Activity-owned Initial Placement 9/9 remains documentary only.
  Current Model B Edit Mode owners are Route Spatial Entry and Activity Relocation.

## Unity execution order

1. In Edit Mode, run `Immersive Framework > QA > Player > P3M5B Apply Route Transition and Negative Matrix Fixture`.
2. Enter a fresh normal Play Mode session and run `Immersive Framework > QA > Player > Scene Provided > Run Integration`.
3. Return to Edit Mode and run `Immersive Framework > QA > Player > Run ADR-021 Route Spatial Entry QA`, then `Run ADR-021 Activity Relocation QA`.
4. In Edit Mode, run `Immersive Framework > QA > Player > Run Full Player QA`. It now runs Scene-Provided, Manager-Provisioned, public surface, ADR020-H Leave and participation phases in fresh Play Mode sessions.

Collect each final diagnostic line. A `Passed` result is required from Unity before
claiming certification.
