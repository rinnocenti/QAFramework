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
| 3 | [`ADRs/`](ADRs/) | ADRs novos/propostos, organizados por ordem do plano. |
| 4 | [`Architecture/ADR/`](Architecture/ADR/) | ADRs históricos do package atual. |
| 5 | [`Guides/`](Guides/) | Guias de uso/visualização. |
| 6 | [`ADR-TEMPLATE.md`](ADR-TEMPLATE.md) | Template para novos ADRs. |

---

## 3. Estrutura do pacote

```text
Documentation~/
├─ README.md
├─ ADR-TEMPLATE.md
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

| ADR | Título | Status | Tipo | Escopo | Arquivo |
|---|---|---|---|---|---|
| ADR-BL-001 | Baseline Reconciliation | Accepted | Baseline / Reconciliação | Package atual | [`abrir`](ADRs/F0A-baseline-adrs/01-ADR-BL-001-baseline-reconciliation.md) |
| ADR-BL-002 | Core vs Consumers | Accepted | Arquitetura | Core / Consumers | [`abrir`](ADRs/F0A-baseline-adrs/02-ADR-BL-002-core-vs-consumers.md) |
| ADR-BL-003 | Public API Status Policy | Accepted | API Policy | Package público | [`abrir`](ADRs/F0A-baseline-adrs/03-ADR-BL-003-public-api-status-policy.md) |
| ADR-BL-004 | QA and Diagnostics Boundary | Accepted | Diagnostics / Tooling | Runtime / QA / Editor | [`abrir`](ADRs/F0A-baseline-adrs/04-ADR-BL-004-qa-and-diagnostics-boundary.md) |
| ADR-BL-005 | Dependency Policy | Accepted | Package / Dependencies | UPM / asmdef | [`abrir`](ADRs/F0A-baseline-adrs/05-ADR-BL-005-dependency-policy.md) |

### F1 — API status, Identity and Diagnostics

| ADR | Título | Status | Tipo | Escopo | Arquivo |
|---|---|---|---|---|---|
| ADR-CONTENT-001 | Content Identity Domain | Draft / Deferred | ContentFlow | Content identity | [`abrir`](ADRs/F1-api-status-identity-and-diagnostics/01-ADR-CONTENT-001-content-identity-domain.md) |
| ADR-DIAG-001 | FrameworkFact vs Human Log | Draft / Deferred | Diagnostics | Diagnostics | [`abrir`](ADRs/F1-api-status-identity-and-diagnostics/02-ADR-DIAG-001-frameworkfact-vs-human-log.md) |
| ADR-ID-001 | Typed Identity Policy | Draft / Deferred | Identity | Framework-wide | [`abrir`](ADRs/F1-api-status-identity-and-diagnostics/03-ADR-ID-001-typed-identity-policy.md) |

### F2 — Session scope

| ADR | Título | Status | Tipo | Escopo | Arquivo |
|---|---|---|---|---|---|
| ADR-SESSION-001 | Session Scope and Owner | Draft / Deferred | Session | Session runtime | [`abrir`](ADRs/F2-session-scope/01-ADR-SESSION-001-session-scope-and-owner.md) |
| ADR-SESSION-002 | SessionContent Ownership Semantics | Draft / Deferred | Session / Content | SessionContentSet | [`abrir`](ADRs/F2-session-scope/02-ADR-SESSION-002-sessioncontent-ownership-semantics.md) |

### F3 — Route baseline

| ADR | Título | Status | Tipo | Escopo | Arquivo |
|---|---|---|---|---|---|
| ADR-ROUTE-001 | RouteRuntimeState and RouteContentRuntime Status | Draft / Deferred | Route | Route lifecycle | [`abrir`](ADRs/F3-route-baseline/01-ADR-ROUTE-001-routeruntimestate-and-routecontentruntime-status.md) |
| ADR-ROUTE-002 | RouteContentSet Semantics | Draft / Deferred | Route / Content | RouteContentSet | [`abrir`](ADRs/F3-route-baseline/02-ADR-ROUTE-002-routecontentset-semantics.md) |

### F4 — Activity content and readiness

| ADR | Título | Status | Tipo | Escopo | Arquivo |
|---|---|---|---|---|---|
| ADR-ACTIVITY-001 | ActivityContentSet and Readiness Baseline | Draft / Deferred | Activity | ActivityFlow | [`abrir`](ADRs/F4-activity-content-and-readiness/01-ADR-ACTIVITY-001-activitycontentset-and-readiness-baseline.md) |

### F5 — Local contribution

| ADR | Título | Status | Tipo | Escopo | Arquivo |
|---|---|---|---|---|---|
| ADR-LOCAL-001 | Local Identity | Draft / Deferred | Local | LocalContentIdentity | [`abrir`](ADRs/F5-local-contribution/01-ADR-LOCAL-001-local-identity.md) |
| ADR-LOCAL-002 | Local Contribution Discovery and Requiredness | Draft / Deferred | Local | LocalContributionSet | [`abrir`](ADRs/F5-local-contribution/02-ADR-LOCAL-002-local-contribution-discovery-and-requiredness.md) |

### F6 — Route scene composition and release

| ADR | Título | Status | Tipo | Escopo | Arquivo |
|---|---|---|---|---|---|
| ADR-RELEASE-001 | Content Release Plan by Scope | Draft / Deferred | Release / Ownership | Session/Route/Activity content | [`abrir`](ADRs/F6-route-scene-composition-and-release/01-ADR-RELEASE-001-content-release-plan-by-scope.md) |
| ADR-SCENE-001 | Route Scene Composition Plan and Result | Draft / Deferred | Scene / Route | Route scene composition | [`abrir`](ADRs/F6-route-scene-composition-and-release/02-ADR-SCENE-001-route-scene-composition-plan-and-result.md) |

### F7 — Surface declaration

| ADR | Título | Status | Tipo | Escopo | Arquivo |
|---|---|---|---|---|---|
| ADR-SURFACE-001 | Surface as Space Contract | Draft / Deferred | Surface | Surface declaration | [`abrir`](ADRs/F7-surface-declaration/01-ADR-SURFACE-001-surface-as-space-contract.md) |

### F8 — Runtime roots and materialization

| ADR | Título | Status | Tipo | Escopo | Arquivo |
|---|---|---|---|---|---|
| ADR-RUNTIME-001 | Runtime Ownership and Roots | Draft / Deferred | RuntimeSpawned | Runtime roots | [`abrir`](ADRs/F8-runtime-roots-and-materialization/01-ADR-RUNTIME-001-runtime-ownership-and-roots.md) |
| ADR-RUNTIME-002 | Materialization Request Result Handle | Draft / Deferred | RuntimeSpawned / Content | Materialization | [`abrir`](ADRs/F8-runtime-roots-and-materialization/02-ADR-RUNTIME-002-materialization-request-result-handle.md) |

### F9 — Surface binding and runtime placement

| ADR | Título | Status | Tipo | Escopo | Arquivo |
|---|---|---|---|---|---|
| ADR-SURFACE-002 | Surface Binding and Content Placement | Draft / Deferred | Surface / Runtime | SurfaceBinding | [`abrir`](ADRs/F9-surface-binding-and-runtime-placement/01-ADR-SURFACE-002-surface-binding-and-content-placement.md) |

### F10 — Consumers intermediários

| ADR | Título | Status | Tipo | Escopo | Arquivo |
|---|---|---|---|---|---|
| ADR-INPUT-001 | Input Ownership | Draft / Deferred | Consumer / Input | Input | [`abrir`](ADRs/F10-consumers-intermedi-rios/01-ADR-INPUT-001-input-ownership.md) |
| ADR-PAUSE-001 | Pause as Surface Input Activity Consumer | Draft / Deferred | Consumer / Pause | Pause | [`abrir`](ADRs/F10-consumers-intermedi-rios/02-ADR-PAUSE-001-pause-as-surface-input-activity-consumer.md) |
| ADR-SAVE-001 | Snapshot Envelope and Schema | Draft / Deferred | Consumer / Save | Snapshot | [`abrir`](ADRs/F10-consumers-intermedi-rios/03-ADR-SAVE-001-snapshot-envelope-and-schema.md) |

### F11 — Consumers avançados

| ADR | Título | Status | Tipo | Escopo | Arquivo |
|---|---|---|---|---|---|
| ADR-ACTOR-001 | Actor Runtime Boundary | Draft / Deferred | Consumer / Actor | Actor runtime | [`abrir`](ADRs/F11-consumers-avan-ados/01-ADR-ACTOR-001-actor-runtime-boundary.md) |
| ADR-AUDIO-001 | Audio as Lifecycle Consumer | Draft / Deferred | Consumer / Audio | Audio | [`abrir`](ADRs/F11-consumers-avan-ados/02-ADR-AUDIO-001-audio-as-lifecycle-consumer.md) |
| ADR-CAMERA-001 | Camera as Surface Consumer | Draft / Deferred | Consumer / Camera | Camera | [`abrir`](ADRs/F11-consumers-avan-ados/03-ADR-CAMERA-001-camera-as-surface-consumer.md) |
| ADR-POOL-001 | Pooling Package Boundary | Draft / Deferred | Consumer / Pooling | Pooling | [`abrir`](ADRs/F11-consumers-avan-ados/04-ADR-POOL-001-pooling-package-boundary.md) |

### Unassigned — Unassigned / revisar metadata

| ADR | Título | Status | Tipo | Escopo | Arquivo |
|---|---|---|---|---|---|
| ADR-0001 | Bootstrap mínimo e construção incremental do Immersive Framework |  |  |  | [`abrir`](ADRs/Unassigned-unassigned/02-ADR-0001-bootstrap-m-nimo-e-constru-o-incremental-do-immersive-framework.md) |
| ADR-0002 | Activity Content Binding mínimo e observável |  |  |  | [`abrir`](ADRs/Unassigned-unassigned/03-ADR-0002-activity-content-binding-m-nimo-e-observ-vel.md) |

---

## 7. Checklist antes de abrir um corte técnico

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

## 8. Regra contra avanço prematuro

```text
Não avançar para feature enquanto o baseline ativo ainda for ambíguo.
Não avançar para consumer enquanto owner, identity, content set e release ainda forem ambíguos.
Não copiar shape do NewScripts; preservar capacidades e redesenhar boundaries.
```

---

## 9. Foco atual

F0A está aceito como fase de decisão/documentação.

Próximo corte técnico:

```text
F0B — Baseline hygiene
```

Alvo do F0B:

```text
Aplicar somente a higiene mínima para código, package metadata, README, inspectors e guia não contradizerem os ADRs aceitos.
```

Não iniciar F1 enquanto F0B não compilar e o smoke baseline não passar.
