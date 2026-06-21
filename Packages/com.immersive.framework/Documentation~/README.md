# Immersive Framework — Documentation

Este é o arquivo único de navegação do pacote `Documentation~`.

A documentação foi simplificada para evitar múltiplos índices e múltiplos `README.md`. Use este arquivo como ponto de entrada.

---

## 1. Como navegar

Fluxo recomendado:

```text
1. Leia o roadmap.
2. Confira a matriz de rastreabilidade.
3. Leia os ADRs da fase atual.
4. Só então abra corte técnico.
5. Atualize smoke/validator/documentação da fase.
```

Regra central:

```text
Roadmap define ordem.
Matriz confirma cobertura.
ADR registra decisão.
Corte técnico implementa.
Smoke valida.
```

---

## 2. Documentos principais

| Ordem | Documento | Papel |
|---:|---|---|
| 1 | [`Planning/Immersive-Framework-Roadmap-Revisado.md`](Planning/Immersive-Framework-Roadmap-Revisado.md) | Sequência de fases e limites de cada fase. |
| 2 | [`Planning/Capability-Traceability-Matrix.md`](Planning/Capability-Traceability-Matrix.md) | Cobertura das capacidades do `NewScripts`, bloqueadores e riscos. |
| 3 | [`ADR_NAMING_CONVENTION.md`](ADR_NAMING_CONVENTION.md) | Regra de nomenclatura para ADRs alinhada ao plano. |
| 4 | [`ADRs/`](ADRs/) | ADRs novos/propostos, organizados por ordem do plano. |
| 5 | [`Architecture/ADR/`](Architecture/ADR/) | ADRs históricos do package atual. |
| 6 | [`BASELINE_SMOKE.md`](BASELINE_SMOKE.md) | Smoke manual mínimo do baseline ativo. |
| 7 | [`F0_CLOSURE.md`](F0_CLOSURE.md) | Fechamento formal da Fase 0 após smoke. |
| 8 | [`F1_ADR_ACCEPTANCE.md`](F1_ADR_ACCEPTANCE.md) | Aceite dos ADRs da F1 antes de implementação técnica. |
| 9 | [`API_STATUS_CONVENTION.md`](API_STATUS_CONVENTION.md) | Convenção mínima de status de API aplicada no F1B. |
| 10 | [`F1B_CLOSURE.md`](F1B_CLOSURE.md) | Fechamento do F1B após compile-smoke. |
| 11 | [`FRAMEWORK_FACT_MINIMAL_MODEL.md`](FRAMEWORK_FACT_MINIMAL_MODEL.md) | Modelo mínimo de `FrameworkFact` criado no F1C. |
| 12 | [`F1C_CLOSURE.md`](F1C_CLOSURE.md) | Fechamento do F1C após compile-smoke. |
| 13 | [`VALIDATION_MODE_SEMANTICS.md`](VALIDATION_MODE_SEMANTICS.md) | Semântica mínima de `ValidationMode` criada no F1D. |
| 14 | [`F1D_CLOSURE.md`](F1D_CLOSURE.md) | Fechamento do F1D após compile-smoke. |
| 15 | [`TYPED_IDENTITY_PRIMITIVES.md`](TYPED_IDENTITY_PRIMITIVES.md) | Primitivos mínimos de identidade tipada criados no F1E. |
| 16 | [`F1E_CLOSURE.md`](F1E_CLOSURE.md) | Fechamento do F1E após compile-smoke. |
| 17 | [`F1E1_ADR_NAMING_ALIGNMENT.md`](F1E1_ADR_NAMING_ALIGNMENT.md) | Fechamento da higiene de nomenclatura dos ADRs. |
| 18 | [`CONTENT_IDENTITY_AND_HANDLE_REVIEW.md`](CONTENT_IDENTITY_AND_HANDLE_REVIEW.md) | Revisão F1F de content identity e `FrameworkContentHandle`. |
| 19 | [`F1F_CLOSURE.md`](F1F_CLOSURE.md) | Fechamento do F1F após compile-smoke. |
| 20 | [`F1_CLOSURE.md`](F1_CLOSURE.md) | Fechamento formal da Fase 1 antes de abrir F2. |
| 21 | [`F2A_SESSION_SCOPE_ADR_ACCEPTANCE.md`](F2A_SESSION_SCOPE_ADR_ACCEPTANCE.md) | Aceite dos ADRs de Session scope antes da implementação técnica F2. |
| 22 | [`F2A_REMOVED_FILES.txt`](F2A_REMOVED_FILES.txt) | Lista de paths ADR duplicados/obsoletos a remover se o pacote for aplicado por overlay. |
| 23 | [`SESSION_RUNTIME_STATE_BOUNDARY.md`](SESSION_RUNTIME_STATE_BOUNDARY.md) | Corte técnico F2B: fronteira explícita de `SessionRuntimeState`. |
| 24 | [`F2B_CLOSURE.md`](F2B_CLOSURE.md) | Fechamento do F2B após compile-smoke. |
| 25 | [`SESSION_CONTENT_SET_MINIMAL_MODEL.md`](SESSION_CONTENT_SET_MINIMAL_MODEL.md) | Corte técnico F2C: modelo mínimo de `SessionContentSet`. |
| 26 | [`F2C_CLOSURE.md`](F2C_CLOSURE.md) | Fechamento do F2C após compile-smoke. |
| 27 | [`F2_CLOSURE.md`](F2_CLOSURE.md) | Fechamento formal da Fase 2 antes de abrir F3. |
| 26 | [`Guides/`](Guides/) | Guias de uso/visualização. |
| 27 | [`ADR-TEMPLATE.md`](ADR-TEMPLATE.md) | Template para novos ADRs. |

---

## 3. Estrutura do pacote

```text
Documentation~/
├─ README.md
├─ ADR-TEMPLATE.md
├─ ADR_NAMING_CONVENTION.md
├─ BASELINE_SMOKE.md
├─ F0_CLOSURE.md
├─ F1_ADR_ACCEPTANCE.md
├─ API_STATUS_CONVENTION.md
├─ F1B_CLOSURE.md
├─ FRAMEWORK_FACT_MINIMAL_MODEL.md
├─ F1C_CLOSURE.md
├─ VALIDATION_MODE_SEMANTICS.md
├─ F1D_CLOSURE.md
├─ TYPED_IDENTITY_PRIMITIVES.md
├─ F1E_CLOSURE.md
├─ F1E1_ADR_NAMING_ALIGNMENT.md
├─ CONTENT_IDENTITY_AND_HANDLE_REVIEW.md
├─ F1F_CLOSURE.md
├─ F1_CLOSURE.md
├─ F2A_SESSION_SCOPE_ADR_ACCEPTANCE.md
├─ F2A_REMOVED_FILES.txt
├─ SESSION_RUNTIME_STATE_BOUNDARY.md
├─ F2B_CLOSURE.md
├─ F2C_CLOSURE.md
├─ F2_CLOSURE.md
├─ SESSION_CONTENT_SET_MINIMAL_MODEL.md
├─ Planning/
│  ├─ Immersive-Framework-Roadmap-Revisado.md
│  └─ Capability-Traceability-Matrix.md
├─ ADRs/
│  ├─ F0A-baseline-adrs/
│  ├─ F1-api-status-identity-and-diagnostics/
│  ├─ F2-session-scope/
│  ├─ ...
│  └─ F11-consumers-avancados/
├─ Architecture/ADR/
└─ Guides/
```

Observação:

```text
ADRs/ = decisões planejadas/propostas para o roadmap.
Architecture/ADR/ = decisões históricas do package atual.

ADR file names use the plan order first: `<plan-order>-<adr-id>-<slug>.md`. The stable architectural id remains the `ADR-*` segment.
```

---

## 4. Status dos ADRs

| Status | Significado |
|---|---|
| `Proposed` | Pronto para revisão/aceite imediato, mas ainda não aceito. |
| `Draft / Deferred` | Rascunho de fase futura; não é decisão aceita. |
| `Accepted` | Decisão aprovada. |
| `Superseded` | Substituído por ADR posterior. |

Não trate `Draft / Deferred` como autorização para implementar.

---

## 5. Fases do roadmap

| Fase | Tema | Pasta |
|---|---|---|
| F0A | Baseline ADRs | [`ADRs/F0A-baseline-adrs/`](ADRs/F0A-baseline-adrs/) |
| F1 | API status, Identity and Diagnostics | [`ADRs/F1-api-status-identity-and-diagnostics/`](ADRs/F1-api-status-identity-and-diagnostics/) |
| F2 | Session scope | [`ADRs/F2-session-scope/`](ADRs/F2-session-scope/) |
| F3 | Route baseline | [`ADRs/F3-route-baseline/`](ADRs/F3-route-baseline/) |
| F4 | Activity content and readiness | [`ADRs/F4-activity-content-and-readiness/`](ADRs/F4-activity-content-and-readiness/) |
| F5 | Local contribution | [`ADRs/F5-local-contribution/`](ADRs/F5-local-contribution/) |
| F6 | Route scene composition and release | [`ADRs/F6-route-scene-composition-and-release/`](ADRs/F6-route-scene-composition-and-release/) |
| F7 | Surface declaration | [`ADRs/F7-surface-declaration/`](ADRs/F7-surface-declaration/) |
| F8 | Runtime roots and materialization | [`ADRs/F8-runtime-roots-and-materialization/`](ADRs/F8-runtime-roots-and-materialization/) |
| F9 | Surface binding and runtime placement | [`ADRs/F9-surface-binding-and-runtime-placement/`](ADRs/F9-surface-binding-and-runtime-placement/) |
| F10 | Consumers intermediários | [`ADRs/F10-consumers-intermedi-rios/`](ADRs/F10-consumers-intermedi-rios/) |
| F11 | Consumers avançados | [`ADRs/F11-consumers-avan-ados/`](ADRs/F11-consumers-avan-ados/) |
| Unassigned | Unassigned / revisar metadata | [`ADRs/Unassigned-unassigned/`](ADRs/Unassigned-unassigned/) |

---

## 6. ADRs por fase

### F0A — Baseline ADRs

| Ordem no Plano | ADR | Título | Status | Tipo | Escopo | Arquivo |
|---|---|---|---|---|---|---|
| F0A-01 | ADR-BL-001 | Baseline Reconciliation | Accepted | Baseline / Reconciliação | Package atual | [`abrir`](ADRs/F0A-baseline-adrs/F0A-01-ADR-BL-001-baseline-reconciliation.md) |
| F0A-02 | ADR-BL-002 | Core vs Consumers | Accepted | Arquitetura | Core / Consumers | [`abrir`](ADRs/F0A-baseline-adrs/F0A-02-ADR-BL-002-core-vs-consumers.md) |
| F0A-03 | ADR-BL-003 | Public API Status Policy | Accepted | API Policy | Package público | [`abrir`](ADRs/F0A-baseline-adrs/F0A-03-ADR-BL-003-public-api-status-policy.md) |
| F0A-04 | ADR-BL-004 | QA and Diagnostics Boundary | Accepted | Diagnostics / Tooling | Runtime / QA / Editor | [`abrir`](ADRs/F0A-baseline-adrs/F0A-04-ADR-BL-004-qa-and-diagnostics-boundary.md) |
| F0A-05 | ADR-BL-005 | Dependency Policy | Accepted | Package / Dependencies | UPM / asmdef | [`abrir`](ADRs/F0A-baseline-adrs/F0A-05-ADR-BL-005-dependency-policy.md) |

### F1 — API status, Identity and Diagnostics

| Ordem no Plano | ADR | Título | Status | Tipo | Escopo | Arquivo |
|---|---|---|---|---|---|---|
| F1A-01 | ADR-ID-001 | Typed Identity Policy | Accepted | Identity | Framework-wide | [`abrir`](ADRs/F1-api-status-identity-and-diagnostics/F1A-01-ADR-ID-001-typed-identity-policy.md) |
| F1A-02 | ADR-DIAG-001 | FrameworkFact vs Human Log | Accepted | Diagnostics | Diagnostics | [`abrir`](ADRs/F1-api-status-identity-and-diagnostics/F1A-02-ADR-DIAG-001-frameworkfact-vs-human-log.md) |
| F1A-03 | ADR-CONTENT-001 | Content Identity Domain | Accepted | ContentFlow | Content identity | [`abrir`](ADRs/F1-api-status-identity-and-diagnostics/F1A-03-ADR-CONTENT-001-content-identity-domain.md) |

### F2 — Session scope

| Ordem no Plano | ADR | Título | Status | Tipo | Escopo | Arquivo |
|---|---|---|---|---|---|---|
| F2-01 | ADR-SESSION-001 | Session Scope and Owner | Accepted | Session | Session runtime | [`abrir`](ADRs/F2-session-scope/F2-01-ADR-SESSION-001-session-scope-and-owner.md) |
| F2-02 | ADR-SESSION-002 | SessionContent Ownership Semantics | Accepted | Session / Content | SessionContentSet | [`abrir`](ADRs/F2-session-scope/F2-02-ADR-SESSION-002-sessioncontent-ownership-semantics.md) |
| F2-03 | ADR-SETTINGS-001 | Settings Source Policy | Accepted | Bootstrap / Settings | Project Settings / runtime bootstrap | [`abrir`](ADRs/F2-session-scope/F2-03-ADR-SETTINGS-001-settings-source-policy.md) |


### F2B — SessionRuntimeState explicit boundary

Status atual:

```text
F2B — CLOSED / COMPILE-SMOKE PASS
```

Documento técnico: [`SESSION_RUNTIME_STATE_BOUNDARY.md`](SESSION_RUNTIME_STATE_BOUNDARY.md).

Este corte criou a fronteira explícita `Runtime/SessionLifecycle/SessionRuntimeState.cs` e conectou o estado ao `FrameworkRuntimeHost`, sem criar `SessionContentSet`.

### F2C — SessionContentSet minimal model

Status atual:

```text
F2C — CLOSED / COMPILE-SMOKE PASS
```

Documento técnico: [`SESSION_CONTENT_SET_MINIMAL_MODEL.md`](SESSION_CONTENT_SET_MINIMAL_MODEL.md).

Este corte cria `SessionContentOwnership`, `SessionContentEntry` e `SessionContentSet`, e conecta o set vazio ao `SessionRuntimeState`. Não cria loading, persistent scenes, release policy, Surface, RuntimeMaterialization ou consumers.

Fechamento: [`F2C_CLOSURE.md`](F2C_CLOSURE.md).

### F2D — F2 technical closure checkpoint

Status atual:

```text
F2D — CLOSED / DOCUMENTATION ONLY
F2  — CLOSED / PASS
```

Documento de fechamento: [`F2_CLOSURE.md`](F2_CLOSURE.md).

Este checkpoint fecha `IF-FW-ROAD-2F — Session smoke` a partir dos smokes de F2B e F2C. Não altera runtime e não cria feature nova.

### F3 — Route baseline

| Ordem no Plano | ADR | Título | Status | Tipo | Escopo | Arquivo |
|---|---|---|---|---|---|---|
| F3-01 | ADR-ROUTE-001 | RouteRuntimeState and RouteContentRuntime Status | Draft / Deferred | Route | Route lifecycle | [`abrir`](ADRs/F3-route-baseline/F3-01-ADR-ROUTE-001-routeruntimestate-and-routecontentruntime-status.md) |
| F3-02 | ADR-ROUTE-002 | RouteContentSet Semantics | Draft / Deferred | Route / Content | RouteContentSet | [`abrir`](ADRs/F3-route-baseline/F3-02-ADR-ROUTE-002-routecontentset-semantics.md) |

### F4 — Activity content and readiness

| Ordem no Plano | ADR | Título | Status | Tipo | Escopo | Arquivo |
|---|---|---|---|---|---|---|
| F4-01 | ADR-ACTIVITY-001 | ActivityContentSet and Readiness Baseline | Draft / Deferred | Activity | ActivityFlow | [`abrir`](ADRs/F4-activity-content-and-readiness/F4-01-ADR-ACTIVITY-001-activitycontentset-and-readiness-baseline.md) |

### F5 — Local contribution

| Ordem no Plano | ADR | Título | Status | Tipo | Escopo | Arquivo |
|---|---|---|---|---|---|---|
| F5-01 | ADR-LOCAL-001 | Local Identity | Draft / Deferred | Local | LocalContentIdentity | [`abrir`](ADRs/F5-local-contribution/F5-01-ADR-LOCAL-001-local-identity.md) |
| F5-02 | ADR-LOCAL-002 | Local Contribution Discovery and Requiredness | Draft / Deferred | Local | LocalContributionSet | [`abrir`](ADRs/F5-local-contribution/F5-02-ADR-LOCAL-002-local-contribution-discovery-and-requiredness.md) |

### F6 — Route scene composition and release

| Ordem no Plano | ADR | Título | Status | Tipo | Escopo | Arquivo |
|---|---|---|---|---|---|---|
| F6-01 | ADR-RELEASE-001 | Content Release Plan by Scope | Draft / Deferred | Release / Ownership | Session/Route/Activity content | [`abrir`](ADRs/F6-route-scene-composition-and-release/F6-01-ADR-RELEASE-001-content-release-plan-by-scope.md) |
| F6-02 | ADR-SCENE-001 | Route Scene Composition Plan and Result | Draft / Deferred | Scene / Route | Route scene composition | [`abrir`](ADRs/F6-route-scene-composition-and-release/F6-02-ADR-SCENE-001-route-scene-composition-plan-and-result.md) |

### F7 — Surface declaration

| Ordem no Plano | ADR | Título | Status | Tipo | Escopo | Arquivo |
|---|---|---|---|---|---|---|
| F7-01 | ADR-SURFACE-001 | Surface as Space Contract | Draft / Deferred | Surface | Surface declaration | [`abrir`](ADRs/F7-surface-declaration/F7-01-ADR-SURFACE-001-surface-as-space-contract.md) |

### F8 — Runtime roots and materialization

| Ordem no Plano | ADR | Título | Status | Tipo | Escopo | Arquivo |
|---|---|---|---|---|---|---|
| F8-01 | ADR-RUNTIME-001 | Runtime Ownership and Roots | Draft / Deferred | RuntimeSpawned | Runtime roots | [`abrir`](ADRs/F8-runtime-roots-and-materialization/F8-01-ADR-RUNTIME-001-runtime-ownership-and-roots.md) |
| F8-02 | ADR-RUNTIME-002 | Materialization Request Result Handle | Draft / Deferred | RuntimeSpawned / Content | Materialization | [`abrir`](ADRs/F8-runtime-roots-and-materialization/F8-02-ADR-RUNTIME-002-materialization-request-result-handle.md) |

### F9 — Surface binding and runtime placement

| Ordem no Plano | ADR | Título | Status | Tipo | Escopo | Arquivo |
|---|---|---|---|---|---|---|
| F9-01 | ADR-SURFACE-002 | Surface Binding and Content Placement | Draft / Deferred | Surface / Runtime | SurfaceBinding | [`abrir`](ADRs/F9-surface-binding-and-runtime-placement/F9-01-ADR-SURFACE-002-surface-binding-and-content-placement.md) |

### F10 — Consumers intermediários

| Ordem no Plano | ADR | Título | Status | Tipo | Escopo | Arquivo |
|---|---|---|---|---|---|---|
| F10-01 | ADR-INPUT-001 | Input Ownership | Draft / Deferred | Consumer / Input | Input | [`abrir`](ADRs/F10-consumers-intermedi-rios/F10-01-ADR-INPUT-001-input-ownership.md) |
| F10-02 | ADR-PAUSE-001 | Pause as Surface Input Activity Consumer | Draft / Deferred | Consumer / Pause | Pause | [`abrir`](ADRs/F10-consumers-intermedi-rios/F10-02-ADR-PAUSE-001-pause-as-surface-input-activity-consumer.md) |
| F10-03 | ADR-SAVE-001 | Snapshot Envelope and Schema | Draft / Deferred | Consumer / Save | Snapshot | [`abrir`](ADRs/F10-consumers-intermedi-rios/F10-03-ADR-SAVE-001-snapshot-envelope-and-schema.md) |

### F11 — Consumers avançados

| Ordem no Plano | ADR | Título | Status | Tipo | Escopo | Arquivo |
|---|---|---|---|---|---|---|
| F11-01 | ADR-ACTOR-001 | Actor Runtime Boundary | Draft / Deferred | Consumer / Actor | Actor runtime | [`abrir`](ADRs/F11-consumers-avan-ados/F11-01-ADR-ACTOR-001-actor-runtime-boundary.md) |
| F11-02 | ADR-AUDIO-001 | Audio as Lifecycle Consumer | Draft / Deferred | Consumer / Audio | Audio | [`abrir`](ADRs/F11-consumers-avan-ados/F11-02-ADR-AUDIO-001-audio-as-lifecycle-consumer.md) |
| F11-03 | ADR-CAMERA-001 | Camera as Surface Consumer | Draft / Deferred | Consumer / Camera | Camera | [`abrir`](ADRs/F11-consumers-avan-ados/F11-03-ADR-CAMERA-001-camera-as-surface-consumer.md) |
| F11-04 | ADR-POOL-001 | Pooling Package Boundary | Draft / Deferred | Consumer / Pooling | Pooling | [`abrir`](ADRs/F11-consumers-avan-ados/F11-04-ADR-POOL-001-pooling-package-boundary.md) |

### Unassigned — Unassigned / revisar metadata

| Ordem no Plano | ADR | Título | Status | Tipo | Escopo | Arquivo |
|---|---|---|---|---|---|---|
| — | ADR-0001 | Bootstrap mínimo e construção incremental do Immersive Framework |  |  |  | [`abrir`](ADRs/Unassigned-unassigned/02-ADR-0001-bootstrap-m-nimo-e-constru-o-incremental-do-immersive-framework.md) |
| — | ADR-0002 | Activity Content Binding mínimo e observável |  |  |  | [`abrir`](ADRs/Unassigned-unassigned/03-ADR-0002-activity-content-binding-m-nimo-e-observ-vel.md) |

---

## 7. Status F0 fechado

| Item | Status | Observação |
|---|---|---|
| `F0A` | `CLOSED / ADRS ACCEPTED` | ADRs de baseline aceitos. |
| `F0B` | `CLOSED / HYGIENE APPLIED / SMOKE PASS` | Higiene aplicada e smoke aprovado. |
| `F0C` | `CLOSED / FORMAL CLOSURE` | Fechamento registrado em `F0_CLOSURE.md`. |
| `F0` | `CLOSED / PASS` | Nenhum bloqueador F0 permanece aberto. |

Nota de status:

```text
ADR = Accepted.
Corte/fase = Closed.
```

Resumo aplicado:

```text
CameraFlow saiu do core ativo.
Cinemachine saiu das dependências obrigatórias de com.immersive.framework.
RouteContentRuntime ficou Deferred até F3.
RouteContentProfileAsset ficou Planning-only.
FrameworkQaCanvas ficou Development Tooling.
BASELINE_SMOKE.md registra o smoke mínimo.
F0_CLOSURE.md registra a matriz ADR → resultado.
```


## 8. Status F1 fechado

| Item | Status | Observação |
|---|---|---|
| `F1A` | `CLOSED / ACCEPTED` | ADRs de Identity, Diagnostics e Content Identity aceitos. |
| `F1B` | `CLOSED / COMPILE-SMOKE PASS` | Convenção mínima de status de API aplicada e validada por smoke. |
| `F1C` | `CLOSED / COMPILE-SMOKE PASS` | Modelo mínimo de `FrameworkFact` criado e validado. |
| `F1D` | `CLOSED / COMPILE-SMOKE PASS` | Semântica mínima de `ValidationMode` criada e validada. |
| `F1E` | `CLOSED / COMPILE-SMOKE PASS` | Primitivos mínimos de identidade tipada criados e validados. |
| `F1E1` | `CLOSED / DOCUMENTATION ONLY` | ADRs renomeados para seguir ordem do plano; não altera runtime. |
| `F1F` | `CLOSED / COMPILE-SMOKE PASS` | Revisão de content identity e `FrameworkContentHandle` aplicada e validada. |
| `F1` | `CLOSED / PASS` | Checkpoint formal registrado em `F1_CLOSURE.md`. |

F1 está fechada. F1E1 apenas corrigiu a navegação documental dos ADRs e F1F validou a identidade composta de content handles no smoke de fechamento.


## 9. Status F2 fechado

| Item | Status | Observação |
|---|---|---|
| `F2A` | `CLOSED / ADRS ACCEPTED` | ADRs de Session scope, SessionContent ownership e Settings source aceitos. |
| `F2B` | `CLOSED / COMPILE-SMOKE PASS` | `SessionRuntimeState` explícito implementado e validado. |
| `F2C` | `CLOSED / COMPILE-SMOKE PASS` | `SessionContentSet` mínimo e ownership semantics implementados e validados. |
| `F2D` | `CLOSED / DOCUMENTATION ONLY` | Checkpoint formal de fechamento de F2. |
| `F2` | `CLOSED / PASS` | Todos os itens do roadmap F2 foram cobertos. |

F2 está fechado. O próximo avanço deve começar pela Fase 3, seguindo ADRs e roadmap.


## 10. Checklist antes de abrir um corte técnico

```text
1. Qual fase do roadmap este corte pertence?
2. Qual capacidade da matriz ele cobre?
3. Os bloqueadores da matriz já existem?
4. O ADR necessário está aceito ou explicitamente aprovado para o corte?
5. O corte cria consumer antes de core? Se sim, parar.
6. O corte usa string/path/GameObject name como chave funcional? Se sim, redesenhar.
7. O validator ou smoke da fase será atualizado?
```

---

## 11. Regra contra avanço prematuro

```text
Não avançar para feature enquanto o baseline ativo ainda for ambíguo.
Não avançar para consumer enquanto owner, identity, content set e release ainda forem ambíguos.
Não copiar shape do NewScripts; preservar capacidades e redesenhar boundaries.
```

---

## 12. Foco atual

F0 está fechado. F1 está fechado. F2 está fechado. A próxima etapa autorizada é abrir F3 com revisão/aceite dos ADRs de Route baseline.

```text
F0A — CLOSED / ADRS ACCEPTED.
F0B — CLOSED / HYGIENE APPLIED / SMOKE PASS.
F0C — CLOSED / FORMAL CLOSURE.
F0  — CLOSED / PASS.
F1A — CLOSED / ACCEPTED.
F1B — CLOSED / COMPILE-SMOKE PASS.
F1C — CLOSED / COMPILE-SMOKE PASS.
F1D — CLOSED / COMPILE-SMOKE PASS.
F1E — CLOSED / COMPILE-SMOKE PASS.
F1E1 — CLOSED / DOCUMENTATION ONLY.
F1F — CLOSED / COMPILE-SMOKE PASS.
F1  — CLOSED / PASS.
F2A — CLOSED / ADRS ACCEPTED.
F2B — CLOSED / COMPILE-SMOKE PASS.
F2C — CLOSED / COMPILE-SMOKE PASS.
F2D — CLOSED / DOCUMENTATION ONLY.
F2  — CLOSED / PASS.
```

Próximo passo autorizado:

```text
F3A — Route baseline ADR review and acceptance.
```

Não iniciar Surface, RuntimeMaterialization ou consumers antes do fechamento técnico de F3/F6/F8 conforme roadmap.
