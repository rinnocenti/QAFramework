# Completeness Tracker

Status consolidado do pacote `com.immersive.framework`.

Este arquivo substitui os antigos documentos de fechamento e aceite de fase. Os documentos técnicos específicos permanecem como evidência de cada corte; o status vivo fica aqui.

## Overview

| Phase | Status | Current gate | Consolidated docs |
|---|---|---|---|
| F0 | `CLOSED / PASS` | Baseline closed | `Core/BASELINE_SMOKE.md` |
| F1 | `CLOSED / PASS` | Identity, diagnostics and validation baseline closed | `Core/API_STATUS_CONVENTION.md`, `Core/FRAMEWORK_FACT_MINIMAL_MODEL.md`, `Core/VALIDATION_MODE_SEMANTICS.md`, `Core/TYPED_IDENTITY_PRIMITIVES.md`, `Core/CONTENT_IDENTITY_AND_HANDLE_REVIEW.md` |
| F2 | `CLOSED / PASS` | Session scope closed | `Session/SESSION_RUNTIME_STATE_BOUNDARY.md`, `Session/SESSION_CONTENT_SET_MINIMAL_MODEL.md` |
| F3 | `CLOSED / PASS` | Route baseline closed | `Route/ROUTE_RUNTIME_STATE_TYPED.md`, `Route/ROUTE_EXIT_RESULT_MINIMAL.md`, `Route/ROUTE_CONTENT_RUNTIME_EXECUTION_DECISION.md`, `Route/ROUTE_CONTENT_SET_SEMANTICS.md`, `Route/ROUTE_LOCAL_CALLBACK_SMOKE.md`, `Route/ROUTE_VALIDATOR_EXPANSION.md`, `Route/QA_PANEL_SIMPLIFICATION.md`, `Route/QA_AUTHORING_VALIDATION_HYGIENE.md` |
| F4 | `CLOSED / ACTIVITY BASELINE PASS` | Activity baseline closed | `Activity/ACTIVITY_RUNTIME_STATE_REFINED.md`, `Activity/ACTIVITY_CONTENT_SET_MINIMAL.md`, `Activity/ACTIVITY_CONTENT_LIFECYCLE_RESULT.md`, `Activity/ACTIVITY_READINESS_STATE_MINIMAL.md`, `Activity/ACTIVITY_LOCAL_VISIBILITY_ADAPTER.md`, `Activity/ACTIVITY_BASELINE_SMOKE.md` |
| F5 | `OPEN / LOCAL CONTRIBUTION` | F5B pending compile-smoke | `Local/LOCAL_CONTENT_IDENTITY.md` |

## Consolidation rule

The following file families are status-only and should not expand further:

- phase closure files;
- ADR acceptance files;
- hygiene/checkpoint files whose only purpose is to restate status.

Use this file instead for phase state, next gate, and historical completion summary.

## Preserved technical evidence

Keep these docs as the durable record for implementation details:

- roadmap and traceability matrix;
- ADRs under `ADRs/`;
- architecture references under `Architecture/ADR/`;
- technical cut docs under `Core/`, `Session/`, `Route/`, `Activity/` and `Local/`.

## Current next step

| Next authorized step | Reason |
|---|---|
| `F5C` | `F5B` is applied but still pending compile-smoke validation. |
