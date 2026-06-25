# Immersive Framework Documentation

Ponto de entrada do pacote `Documentation~`.

## Leitura recomendada

1. [`Planning/Immersive-Framework-Roadmap-Revisado.md`](Planning/Immersive-Framework-Roadmap-Revisado.md) - plano unico e autoritativo.
2. [`COMPLETENESS_TRACKER.md`](COMPLETENESS_TRACKER.md) - estado resumido de fases.
3. [`Governance/ADR_NAMING_CONVENTION.md`](Governance/ADR_NAMING_CONVENTION.md)
4. [`Governance/ADR-TEMPLATE.md`](Governance/ADR-TEMPLATE.md)
5. [`ADRs/`](ADRs/)
6. [`ADRs/F10-activity-content-execution-core/`](ADRs/F10-activity-content-execution-core/)
7. [`Planning/Foundation-Hardening-Backlog.md`](Planning/Foundation-Hardening-Backlog.md)
8. [`Architecture/ADR/`](Architecture/ADR/)

## Estrutura atual

- `Planning/Immersive-Framework-Roadmap-Revisado.md` e a fonte unica para roadmap, boundaries e sequencia F10+.
- `COMPLETENESS_TRACKER.md` registra somente estado fechado/atual.
- `ADRs/` preserva decisoes aceitas de F0-F10 classificadas por fase; F10 esta fechado como Activity Content Execution Core: contratos, resultado agregado, participant contract, collection/ordering model, request factory/phase plan, runtime executor, lifecycle integration, participant source boundary e smokes diagnosticos passaram; sem authoring/discovery real de participants, adapters ou gameplay. Nao manter bucket `Unassigned`.
- `Architecture/ADR/` preserva historico arquitetural.
- `Governance/` contem convencao e template de ADR.
- `Guides/` contem conteudo navegavel.
- `Core/`, `Session/`, `Route/`, `Activity/`, `Local/`, `ContentAnchor/`, `RuntimeContent/` contem documentos tecnicos por dominio.

## Docs tecnicos

- [`Core/BASELINE_SMOKE.md`](Core/BASELINE_SMOKE.md)
- [`Core/API_STATUS_CONVENTION.md`](Core/API_STATUS_CONVENTION.md)
- [`Core/FRAMEWORK_FACT_MINIMAL_MODEL.md`](Core/FRAMEWORK_FACT_MINIMAL_MODEL.md)
- [`Core/VALIDATION_MODE_SEMANTICS.md`](Core/VALIDATION_MODE_SEMANTICS.md)
- [`Core/TYPED_IDENTITY_PRIMITIVES.md`](Core/TYPED_IDENTITY_PRIMITIVES.md)
- [`Core/CONTENT_IDENTITY_AND_HANDLE_REVIEW.md`](Core/CONTENT_IDENTITY_AND_HANDLE_REVIEW.md)
- [`Session/SESSION_RUNTIME_STATE_BOUNDARY.md`](Session/SESSION_RUNTIME_STATE_BOUNDARY.md)
- [`Session/SESSION_CONTENT_SET_MINIMAL_MODEL.md`](Session/SESSION_CONTENT_SET_MINIMAL_MODEL.md)
- [`Route/ROUTE_RUNTIME_STATE_TYPED.md`](Route/ROUTE_RUNTIME_STATE_TYPED.md)
- [`Route/ROUTE_EXIT_RESULT_MINIMAL.md`](Route/ROUTE_EXIT_RESULT_MINIMAL.md)
- [`Route/ROUTE_CONTENT_RUNTIME_EXECUTION_DECISION.md`](Route/ROUTE_CONTENT_RUNTIME_EXECUTION_DECISION.md)
- [`Route/ROUTE_CONTENT_SET_SEMANTICS.md`](Route/ROUTE_CONTENT_SET_SEMANTICS.md)
- [`Route/ROUTE_LOCAL_CALLBACK_SMOKE.md`](Route/ROUTE_LOCAL_CALLBACK_SMOKE.md)
- [`Route/ROUTE_VALIDATOR_EXPANSION.md`](Route/ROUTE_VALIDATOR_EXPANSION.md)
- [`Route/ROUTE_CONTENT_PROFILE_USAGE.md`](Route/ROUTE_CONTENT_PROFILE_USAGE.md)
- [`Route/ROUTE_SCENE_COMPOSITION_SMOKE.md`](Route/ROUTE_SCENE_COMPOSITION_SMOKE.md)
- [`Route/ROUTE_RELEASE_SMOKE.md`](Route/ROUTE_RELEASE_SMOKE.md)
- [`Route/QA_PANEL_SIMPLIFICATION.md`](Route/QA_PANEL_SIMPLIFICATION.md)
- [`Route/QA_AUTHORING_VALIDATION_HYGIENE.md`](Route/QA_AUTHORING_VALIDATION_HYGIENE.md)
- [`Activity/ACTIVITY_RUNTIME_STATE_REFINED.md`](Activity/ACTIVITY_RUNTIME_STATE_REFINED.md)
- [`Activity/ACTIVITY_CONTENT_SET_MINIMAL.md`](Activity/ACTIVITY_CONTENT_SET_MINIMAL.md)
- [`Activity/ACTIVITY_CONTENT_LIFECYCLE_RESULT.md`](Activity/ACTIVITY_CONTENT_LIFECYCLE_RESULT.md)
- [`Activity/ACTIVITY_READINESS_STATE_MINIMAL.md`](Activity/ACTIVITY_READINESS_STATE_MINIMAL.md)
- [`Activity/ACTIVITY_LOCAL_VISIBILITY_ADAPTER.md`](Activity/ACTIVITY_LOCAL_VISIBILITY_ADAPTER.md)
- [`Activity/ACTIVITY_BASELINE_SMOKE.md`](Activity/ACTIVITY_BASELINE_SMOKE.md)
- [`Activity/ACTIVITY_CONTENT_EXECUTION_CONTRACTS.md`](Activity/ACTIVITY_CONTENT_EXECUTION_CONTRACTS.md)
- [`Activity/ACTIVITY_CONTENT_EXECUTION_PARTICIPANT_CONTRACT.md`](Activity/ACTIVITY_CONTENT_EXECUTION_PARTICIPANT_CONTRACT.md)
- [`Activity/ACTIVITY_CONTENT_EXECUTION_PARTICIPANT_COLLECTION.md`](Activity/ACTIVITY_CONTENT_EXECUTION_PARTICIPANT_COLLECTION.md)
- [`Activity/ACTIVITY_CONTENT_EXECUTION_PHASE_PLAN.md`](Activity/ACTIVITY_CONTENT_EXECUTION_PHASE_PLAN.md)
- [`Activity/ACTIVITY_CONTENT_EXECUTION_RUNTIME.md`](Activity/ACTIVITY_CONTENT_EXECUTION_RUNTIME.md)
- [`Activity/ACTIVITY_CONTENT_EXECUTION_RUNTIME_SMOKE.md`](Activity/ACTIVITY_CONTENT_EXECUTION_RUNTIME_SMOKE.md)
- [`Activity/ACTIVITY_CONTENT_EXECUTION_LIFECYCLE_INTEGRATION.md`](Activity/ACTIVITY_CONTENT_EXECUTION_LIFECYCLE_INTEGRATION.md)
- [`Activity/ACTIVITY_CONTENT_EXECUTION_LIFECYCLE_TRANSITION_SMOKE.md`](Activity/ACTIVITY_CONTENT_EXECUTION_LIFECYCLE_TRANSITION_SMOKE.md)
- [`Activity/ACTIVITY_CONTENT_EXECUTION_PARTICIPANT_SOURCE.md`](Activity/ACTIVITY_CONTENT_EXECUTION_PARTICIPANT_SOURCE.md)
- [`Activity/ACTIVITY_CONTENT_EXECUTION_PARTICIPANT_SOURCE_SMOKE.md`](Activity/ACTIVITY_CONTENT_EXECUTION_PARTICIPANT_SOURCE_SMOKE.md)
- [`Activity/ACTIVITY_CONTENT_EXECUTION_CORE_CLOSURE.md`](Activity/ACTIVITY_CONTENT_EXECUTION_CORE_CLOSURE.md)
- [`Local/LOCAL_CONTENT_IDENTITY.md`](Local/LOCAL_CONTENT_IDENTITY.md)
- [`Planning/F6-Route-Scene-Composition-Audit.md`](Planning/F6-Route-Scene-Composition-Audit.md)
- [`Planning/F7-Content-Anchor-Declaration-Audit.md`](Planning/F7-Content-Anchor-Declaration-Audit.md)
- [`Planning/F8-Runtime-Roots-Materialization-Audit.md`](Planning/F8-Runtime-Roots-Materialization-Audit.md)
- [`Planning/Foundation-Hardening-Backlog.md`](Planning/Foundation-Hardening-Backlog.md)
- [`RuntimeContent/RUNTIME_OWNERSHIP_PRIMITIVES.md`](RuntimeContent/RUNTIME_OWNERSHIP_PRIMITIVES.md)
- [`RuntimeContent/RUNTIME_CONTENT_HANDLE.md`](RuntimeContent/RUNTIME_CONTENT_HANDLE.md)
- [`RuntimeContent/RUNTIME_SCOPE_ROOT_REGISTRY.md`](RuntimeContent/RUNTIME_SCOPE_ROOT_REGISTRY.md)
- [`RuntimeContent/RUNTIME_CONTENT_RUNTIME.md`](RuntimeContent/RUNTIME_CONTENT_RUNTIME.md)
- [`RuntimeContent/RUNTIME_ROOT_LIFECYCLE_INTEGRATION.md`](RuntimeContent/RUNTIME_ROOT_LIFECYCLE_INTEGRATION.md)
- [`RuntimeContent/RUNTIME_MATERIALIZATION_REQUEST_RESULT.md`](RuntimeContent/RUNTIME_MATERIALIZATION_REQUEST_RESULT.md)
- [`RuntimeContent/RUNTIME_RELEASE_POLICY_LOGICAL_EXECUTION.md`](RuntimeContent/RUNTIME_RELEASE_POLICY_LOGICAL_EXECUTION.md)
- [`RuntimeContent/RUNTIME_TRANSITION_GUARD_SCOPED_CANCELLATION.md`](RuntimeContent/RUNTIME_TRANSITION_GUARD_SCOPED_CANCELLATION.md)
- [`ContentAnchor/CONTENT_ANCHOR_IDENTITY_PRIMITIVES.md`](ContentAnchor/CONTENT_ANCHOR_IDENTITY_PRIMITIVES.md)
- [`ContentAnchor/CONTENT_ANCHOR_DECLARATION_MODEL.md`](ContentAnchor/CONTENT_ANCHOR_DECLARATION_MODEL.md)
- [`ContentAnchor/ROUTE_CONTENT_ANCHOR_AUTHORING.md`](ContentAnchor/ROUTE_CONTENT_ANCHOR_AUTHORING.md)
- [`ContentAnchor/ACTIVITY_CONTENT_ANCHOR_AUTHORING.md`](ContentAnchor/ACTIVITY_CONTENT_ANCHOR_AUTHORING.md)
- [`ContentAnchor/CONTENT_ANCHOR_SET.md`](ContentAnchor/CONTENT_ANCHOR_SET.md)
- [`ContentAnchor/ROUTE_CONTENT_ANCHOR_DISCOVERY.md`](ContentAnchor/ROUTE_CONTENT_ANCHOR_DISCOVERY.md)
- [`ContentAnchor/ACTIVITY_CONTENT_ANCHOR_DISCOVERY.md`](ContentAnchor/ACTIVITY_CONTENT_ANCHOR_DISCOVERY.md)
- [`ContentAnchor/CONTENT_ANCHOR_BINDING_CONTRACTS.md`](ContentAnchor/CONTENT_ANCHOR_BINDING_CONTRACTS.md)
- [`ContentAnchor/CONTENT_ANCHOR_BINDING_RUNTIME.md`](ContentAnchor/CONTENT_ANCHOR_BINDING_RUNTIME.md)
- [`ContentAnchor/CONTENT_ANCHOR_DIAGNOSTICS_SMOKE.md`](ContentAnchor/CONTENT_ANCHOR_DIAGNOSTICS_SMOKE.md)
- [`ContentAnchor/CONTENT_ANCHOR_AUTHORING_VALIDATION.md`](ContentAnchor/CONTENT_ANCHOR_AUTHORING_VALIDATION.md)

