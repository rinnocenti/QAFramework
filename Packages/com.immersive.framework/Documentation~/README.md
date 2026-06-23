# Immersive Framework Documentation

Ponto de entrada do pacote `Documentation~`.

Estrutura atual:

- `COMPLETENESS_TRACKER.md` para status consolidado.
- `Planning/` para roadmap e rastreabilidade.
- `ADRs/` para decisões propostas e aceitas.
- `Architecture/ADR/` para histórico arquitetural.
- `Governance/` para convenção de ADR e template.
- `Guides/` para conteúdo navegável.
- `Core/`, `Session/`, `Route/`, `Activity/`, `Local/`, `ContentAnchor/`, `RuntimeContent/` para documentos técnicos por domínio.

Leitura recomendada:

1. [`COMPLETENESS_TRACKER.md`](COMPLETENESS_TRACKER.md)
2. [`Planning/Immersive-Framework-Roadmap-Revisado.md`](Planning/Immersive-Framework-Roadmap-Revisado.md)
3. [`Planning/Capability-Traceability-Matrix.md`](Planning/Capability-Traceability-Matrix.md)
4. [`Governance/ADR_NAMING_CONVENTION.md`](Governance/ADR_NAMING_CONVENTION.md)
5. [`Governance/ADR-TEMPLATE.md`](Governance/ADR-TEMPLATE.md)
6. [`ADRs/`](ADRs/)
7. [`Planning/F6-Route-Scene-Composition-Audit.md`](Planning/F6-Route-Scene-Composition-Audit.md)
8. [`Planning/F7-Content-Anchor-Declaration-Audit.md`](Planning/F7-Content-Anchor-Declaration-Audit.md)
9. [`Planning/F8-Runtime-Roots-Materialization-Audit.md`](Planning/F8-Runtime-Roots-Materialization-Audit.md)
10. [`Architecture/ADR/`](Architecture/ADR/)

Docs técnicos:

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
- [`Local/LOCAL_CONTENT_IDENTITY.md`](Local/LOCAL_CONTENT_IDENTITY.md)
- [`Planning/F6-Route-Scene-Composition-Audit.md`](Planning/F6-Route-Scene-Composition-Audit.md)
- [`Planning/F7-Content-Anchor-Declaration-Audit.md`](Planning/F7-Content-Anchor-Declaration-Audit.md)
- [`Planning/F8-Runtime-Roots-Materialization-Audit.md`](Planning/F8-Runtime-Roots-Materialization-Audit.md)
- [`RuntimeContent/RUNTIME_OWNERSHIP_PRIMITIVES.md`](RuntimeContent/RUNTIME_OWNERSHIP_PRIMITIVES.md)
- [`RuntimeContent/RUNTIME_CONTENT_HANDLE.md`](RuntimeContent/RUNTIME_CONTENT_HANDLE.md)
- [`ContentAnchor/CONTENT_ANCHOR_IDENTITY_PRIMITIVES.md`](ContentAnchor/CONTENT_ANCHOR_IDENTITY_PRIMITIVES.md)
- [`ContentAnchor/CONTENT_ANCHOR_DECLARATION_MODEL.md`](ContentAnchor/CONTENT_ANCHOR_DECLARATION_MODEL.md)
- [`ContentAnchor/ROUTE_CONTENT_ANCHOR_AUTHORING.md`](ContentAnchor/ROUTE_CONTENT_ANCHOR_AUTHORING.md)
- [`ContentAnchor/CONTENT_ANCHOR_SET.md`](ContentAnchor/CONTENT_ANCHOR_SET.md)
- [`ContentAnchor/ROUTE_CONTENT_ANCHOR_DISCOVERY.md`](ContentAnchor/ROUTE_CONTENT_ANCHOR_DISCOVERY.md)
- [`ContentAnchor/CONTENT_ANCHOR_DIAGNOSTICS_SMOKE.md`](ContentAnchor/CONTENT_ANCHOR_DIAGNOSTICS_SMOKE.md)
- [`ContentAnchor/CONTENT_ANCHOR_AUTHORING_VALIDATION.md`](ContentAnchor/CONTENT_ANCHOR_AUTHORING_VALIDATION.md)

`COMPLETENESS_TRACKER.md` consolida o estado que antes estava espalhado por arquivos de fechamento e aceite.

- `Documentation~/ContentAnchor/ROUTE_CONTENT_ANCHOR_DISCOVERY.md` — F7F diagnostic discovery of Route Content Anchors into a local ContentAnchorSet.

- `ContentAnchor/CONTENT_ANCHOR_DIAGNOSTICS_SMOKE.md` — dedicated QA smoke for Route Content Anchor diagnostics.
- `ContentAnchor/CONTENT_ANCHOR_AUTHORING_VALIDATION.md` — loaded authoring validation for Route Content Anchors.

- `Planning/F8-Runtime-Roots-Materialization-Audit.md` — F8 boundary for runtime roots, materialization, handles and release.
- `RuntimeContent/RUNTIME_OWNERSHIP_PRIMITIVES.md` — F8B/F8C passive runtime ownership primitives and handle state.
- `RuntimeContent/RUNTIME_CONTENT_HANDLE.md` — F8C passive RuntimeContentHandle state transitions.
