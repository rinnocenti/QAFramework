# Player Surface QA — Joint Certification Runbook

**Date:** 2026-08-08  
**Cuts:** QA-PLAYER-SURFACE-01 + QA-PLAYER-SURFACE-02  

---

## One-shot Unity certification

### A. Edit Mode prepare

1. Exit Play Mode.
2. Menu: **Immersive Framework/QA/Setup/Player/Prepare Player Surface Full Certification**  
   (runs M07 prepare + authored public navigation fixture into QA Hub)

Or separately:

- `Prepare Internal Reconcile Regression` (M07)
- `Prepare Player Surface Public Navigation Fixture`

### B. Automated joint run

Menu: **Immersive Framework/QA/Regressions/Player/Run Player Surface Full Certification (Q1+Q2)**

This enters Play Mode for Q1, exits, re-enters for Q2.

### C. Manual dual run

1. Fresh Play Mode → run Q1 menu → expect `verdict='Q1_PASS'`
2. Exit Play Mode
3. Fresh Play Mode → run Q2 menu → expect `status='Passed'`

### Authored public navigation

Fixture root in Hub: `QA_PlayerSurface_PublicNavigation`

- `ActivityRequestTrigger` Enter/Clear (authored before Play Mode)
- Framework composition binds at Route start
- Authored Activity: `QA_PlayerSurfacePublic_WaitCoveredActivity`
- Route `LocalPlayerProvisioningConsumerAccessBinding`

Q1 uses this path exclusively (no internal Activity port for the happy path).

---

## Expected certified verdict

```text
PLAYER SURFACE QA CERTIFIED
```

when both Q1 and Q2 pass in Unity Play Mode.
