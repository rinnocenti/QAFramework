# Camera QA Consolidation Closure — 2026-08-10

Status: **Current operational map**

This document supersedes historical Camera menu inventories as the current
operational QA map. The large `QA-SMOKE-CONSOLIDATION-AUDIT.md` remains a
point-in-time R1 audit and is intentionally not rewritten to erase historical
smokes that existed when that audit was made.

## Current Camera QA surfaces

```text
C9M Follow Pipeline
  local CameraRigComposer materialization / Follow proof

Session Camera Override Identity Authoring Regression
  stable scoped authoring identities

Camera Output Session Binding Authoring Regression
  persistent output references / validation

Persistent Camera Presentation Composition Regression
  single persistent output composition

C9R Camera Override Authority
  canonical positive lifecycle / precedence / restoration

ADR-004C Owner Lifetime Integrity
  abnormal component publication lifetime

ADR-004B Negative Integrity
  deterministic negatives + rollback + delegated lifecycle/authoring evidence
```

## Removed historical/redundant Camera proofs

The following old Camera QA surfaces were removed during consolidation because
their responsibilities were historical, private/reflection-oriented or absorbed
by the current canonical surfaces:

```text
QaCameraPlayerAuthoringUxSmoke
QaCameraRuntimeHostIntegrationRegression
QaCut4LocalPlayerCameraPublicationOwnershipAuthoringSmoke
```

Their removal does not reduce current Camera certification coverage.

## Current execution

```text
Immersive Framework > QA > Setup > Camera >
Install Camera Override Authority QA

Play Mode
  -> Camera Override Authority from QA Hub
  -> C9R 11/11
  -> ADR-004C 10/10
  -> ADR-004B 18/18
```

## Certification state

```text
C9R      CERTIFIED POSITIVE LIFECYCLE 11/11
ADR004C  CERTIFIED OWNER LIFETIME     10/10
ADR004B  CERTIFIED NEGATIVE INTEGRITY 18/18
```

No second Camera fixture, manager, context, service or orchestrator is part of
the current architecture.

## Residual QA hygiene

The v10 synthetic Local Player teardown patch addresses redundant
`release-not-found` logging after certification. It is cleanup hygiene, not a new
Camera contract or certification gate. A clean-log rerun may be recorded after
the patch is applied.
