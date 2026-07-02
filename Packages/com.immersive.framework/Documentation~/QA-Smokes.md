# QA Smokes

The package exposes QA smoke runners through `FrameworkQaCanvas` and internal diagnostics. This page lists the smoke groups that matter for package validation.

## Validation policy

- Run Unity compile/import before relying on smoke results.
- Run the relevant smoke group after changing runtime, authoring, diagnostics or package setup.
- Do not update smoke text only to hide drift.
- Documentation-only changes do not require Unity smokes unless they claim runtime behavior changed.

## Core smoke groups

| Area | Relevant smokes |
| --- | --- |
| Standard package health | Standard Smoke |
| Route/Activity lifecycle | Activity Baseline Smoke, Route Scene Composition Smoke, Route Release Smoke, Composite Lifecycle Release Smoke, Scope Tail Operation Synthetic Smoke. No direct lifecycle operation evidence smoke button exists; validate `lifecycleOperation*`, `lifecycleContent*` and `lifecycleReadiness*` through Standard/Route/Activity logs. |
| Loading | Loading Result Smoke, Loading Readiness Smoke, Loading Progress Smoke, Loading Screen Adapter Smoke. Loading Result Smoke also validates `LoadingSurfaceAdapterEvidence` count, names/statuses, applied/skipped/failed counts, blocking issue count, progress support and explicit no-op/failure evidence. |
| Transition | Transition Smoke, Transition Effect Smoke, Transition Effect Unity Fade Curtain Smoke, Transition Gate Blocker Smoke |
| Pause | Pause Smoke, Pause Runtime Request Smoke, Pause Gate Blocker Smoke |
| Pause input | Pause InputMode Request Boundary Smoke, Pause InputMode Unity PlayerInput Runtime Bridge Smoke, Pause InputAction Runtime Bridge Trigger Smoke |
| InputMode | InputMode Contract Smoke, Unity Input Official Component Evidence Smoke, InputMode Unity PlayerInput application smokes |
| RuntimeContent / ContentAnchor | Runtime prefab materialization, Content Anchor diagnostics, Activity Content Anchor diagnostics, composite lifecycle release/materialization smokes |
| Reset/Object/Cycle | CycleReset and ObjectReset QA smoke runners when those surfaces change |

F46 is a decision-only lifecycle readiness cut. There is no direct F46 smoke button. Use the existing Route/Activity lifecycle smokes and request logs when validating later lifecycle evidence changes.

## QA Canvas expectations

`FrameworkQaCanvas` should expose current validation buttons, not every historical proof. Obsolete intermediate buttons should remain out of the visible primary QA path unless a future diagnostics mode intentionally restores them.

## Manual checklist

1. Open the QA scene/canvas used by the project.
2. Confirm the package imports without compile errors.
3. Run Standard Smoke.
4. Run the smoke group for the touched surface.
5. Confirm logs report explicit success/failure fields.
6. Confirm disabled or retired paths are not presented as the current validation path.

## Documentation-only checklist

For documentation-only cuts:

1. Confirm this package README points to `Documentation~/README.md`.
2. Confirm the documentation index is clear.
3. Confirm setup, QA and troubleshooting pages match the current runtime surface names.
4. Confirm historical ADRs, closeouts, roadmaps and phase notes are not primary package reading.
