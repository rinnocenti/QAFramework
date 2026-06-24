# Capability Traceability Matrix — Revisada
> Fonte: `newscripts-capability-catalog.md` + roadmap revisado + comparação `NewScripts` vs package atual.
> Objetivo: garantir que capacidades do `NewScripts` sejam preservadas como conceito sem copiar seus anti-padrões.

---
## 1. Regra de leitura
Esta matriz não autoriza implementação direta. Ela serve para rastrear:

```text
capacidade do NewScripts
→ fase alvo
→ corte técnico
→ status no package
→ bloqueadores
→ risco
→ correção recomendada
```

Regras:
- `Coberto` não significa implementado; significa que existe lugar no roadmap.
- `Deferred` significa que a capacidade é válida, mas ainda bloqueada por dependências.
- Consumers não devem entrar antes de owner, identity, content set, release e Content Anchor/runtime quando aplicável.
- Paths, nomes de GameObject e strings podem existir como diagnóstico, não como chave funcional.

---
## 2. Legenda

### Status no package
| Status | Significado |
|---|---|
| Presente | Já existe de forma clara no package atual. |
| Presente / Parcial | Existe, mas ainda precisa reforço ou semântica. |
| Parcial / Ambíguo | Existe código ou conceito, mas o fluxo/status é incerto. |
| Parcial / Ausente | Há precursor, mas falta o modelo real. |
| Ausente | Não existe no package atual. |
| Ambíguo / Experimental | Existe, mas contradiz docs ou ainda não deve ser core. |

### Status no roadmap
| Status | Significado |
|---|---|
| Coberto | A capacidade tem fase/corte claro. |
| Coberto, mas com correção | Existe fase/corte, mas deve ser reposicionado ou limitado. |
| Coberto com ajuste / Deferred | A capacidade é válida, mas só deve entrar depois de bloqueadores. |
| Deferred / Requer detalhamento | Ainda precisa de ADR/corte futuro. |

---
## 3. Correções principais aplicadas

| Capacidade/Área | Correção |
|---|---|
| `IF-FW-F7J` | Alinha a matriz após fechamento de F7: marca F5/F7 smokes como PASS, remove overlay root do escopo F7 e consolida LocalSlot/LocalAnchor como ContentAnchorSlot/ContentAnchorPoint. |
| `ActivityContentProfile` | Não tratar como F4 obrigatório; F4 deve estabilizar `ActivityContentSet` e readiness mínimo. Profile avançado entra depois de scene composition/release. |
| `SessionCompositionContext` | Não criar composition genérico cedo demais. Primeiro Session scope/content sem service locator. |
| `PersistentScenesPolicy` | Não puxar scene ownership para F2. Reavaliar com Route scene composition/release ou futura Session persistent content. |
| `CapabilityInventory` | Em F5 apenas descriptor/contribution simples. Runtime refs/inventory vivo só depois de RuntimeHandle/lifetime. |
| `Content Anchor` | Dividida em declaração (`F7`) e binding/runtime placement (`F9`). |
| `Content Anchor` naming | F7A accepted `Content Anchor`; rejected hook-style names and duplicated anchor naming; use `ContentAnchorPoint` for a specific point. F7B introduced passive identity primitives; F7C introduced passive declaration models; F7D introduced passive Route-scoped authoring; F7E introduced the passive scoped set model; F7F/F7G validated Route discovery diagnostics; F7H closed authoring validation. F7J aligns this matrix after F7 closure. |
| `RuntimeSpawned` | Entra só depois de identity/content/release; consumers não podem criar roots ad hoc. |
| Campos `—` | Tratados como `Deferred` ou `Requer detalhamento`; não podem ficar sem status no backlog. |
| `Overlay root separado de content root` | Removido de F7. É consumer/layout/runtime-placement concern e deve ser reavaliado em F9/F10 depois de RuntimeRoot/materialization e binding. |
| `Local slots` / `Local anchors` | Não criar tipos paralelos. O vocabulário canônico é `ContentAnchorSlot` e `ContentAnchorPoint`; F7C cobre os wrappers tipados. |
| `IF-FW-F9PLUS-REALIGNMENT` | Realinha F9+ após análise de lacunas do NewScripts: Transition/loading, ActivityContentProfile execution, Activity reset, Participation, Live Capability Inventory, Local Lifecycle Participants, Save progression/migration e productization backlog. |
| `Transition / Loading` | Deixa de ser tratado como presentation pura. F6 permanece scene composition/release técnico; F10 passa a cobrir transition policy, loading progress e input lock; F13 pode adicionar presentation adapters. |
| `ActivityContentProfile execution` | Sai de deferred genérico e ganha F10 como fase concreta depois de RuntimeContent/Content Anchor foundations. |
| `Participation / Capability runtime` | Sai de itens espalhados e entra em F11 antes de Input/Save/Pause/Actor/Camera consumirem participação ou capability references. |
| `Input / Snapshot / Pause` | Fase antiga F10 foi renumerada para F12 e passa a depender de F10/F11. |
| `Camera / Audio / Actor / Pooling` | Fase antiga F11 foi renumerada para F13. |
| `Gameplay capabilities` | Fase antiga F12 foi renumerada para F14. |
| `Productization / Tooling` | Novo F15/FX rastreia Settings Source, assembly/build/stripping, versioning/migration, pre-build validation, editor visualizer, Asset Provider/Addressables/DLC e domain reload resilience. |

---
## 4. Matriz consolidada por grupo

### Session
| Capacidade | Decisão | Fase original | Fase revisada | Corte | Status package | Status roadmap | Prioridade | Bloqueadores | Risco | Observação |
|---|---|---|---|---|---|---|---|---|---|---|
| Boot global antes da cena | Preservar | F0B | F0B | IF-FW-ROAD-0B7 | Presente / Parcial | Coberto | Core | Baseline ADRs + API/identity policy | Baixo / controlado | Sem ajuste. |
| Config de runtime obrigatória | Preservar fail-fast | F1 | F1 | IF-FW-ROAD-1D | Parcial / Ausente | Coberto | Core | RuntimeContentHandle + ContributionSet + release policy | Baixo / controlado | Sem ajuste. |
| Registry imutável de config | Preservar como contexto explícito | F1 | F1 | IF-FW-ROAD-1D | Parcial / Ausente | Coberto | Core | Baseline ADRs + API/identity policy | Baixo / controlado | Sem ajuste. |
| Pipeline de composição em fases | Redesenhar sem DependencyManager público | F2 | Futura F2/F3 | IF-FW-ROAD-2C | Parcial / Ausente | Coberto com ajuste / Deferred | Core | Baseline ADRs + API/identity policy | Baixo / controlado | Não obrigatório como corte imediato. Preservar o padrão, mas evitar composition genérico cedo demais. |
| Session runtime state | Preservar; tipar IDs | F2 | F2 | IF-FW-ROAD-2B | Presente / F2B fechado | Coberto | Core | RuntimeContentHandle + ContributionSet + release policy | Baixo / controlado | Sem ajuste. |
| Session composition context | Redesenhar; composição interna | F2 | Deferred | IF-FW-ROAD-2C | Parcial / Ausente | Coberto com ajuste / Deferred | Core | Baseline ADRs + API/identity policy | Risco de entrar cedo demais | Conceito válido, mas perigoso cedo. Primeiro formalizar Session scope/content sem service locator. |
| Persistent scenes policy | Preservar conceito | F2 | F6 ou futura Session Persistent Content | IF-FW-ROAD-2C | Parcial / Ausente | Coberto, mas com correção | Core | Baseline ADRs + API/identity policy | Risco de entrar cedo demais | Não puxar scene ownership para F2; depende de scene composition/release. |
| Session content set | Preservar como `SessionContentSet` | F2 | F2 | IF-FW-ROAD-2C | Presente / F2C fechado | Coberto | Core | Baseline ADRs + API/identity policy | Baixo / controlado | Mantém F2, mas como set mínimo, possivelmente vazio/diagnóstico. |
| Startup route signal | Preservar sinal; remover evento estático global | F2 | F2 | IF-FW-ROAD-2F | Parcial / Ausente | Coberto | Core | Baseline ADRs + API/identity policy | Baixo / controlado | Sem ajuste. |
| Runtime policy (Strict/Release) | Preservar | F1 | F1 | IF-FW-ROAD-1D | Parcial / Ausente | Coberto | Core | RuntimeContentHandle + ContributionSet + release policy | Baixo / controlado | Sem ajuste. |
| Player participation | Preservar typed; adiar implementação | F10+ | F10+/F11 | — | Parcial / Ausente | Coberto com ajuste / Deferred | Core | Baseline ADRs + API/identity policy | Risco de ficar sem corte rastreável | Adiar. Entra com Input/Actor boundary, não no core inicial. |
| Subsystem composition descriptors | Preservar padrão descriptor | F11 | F11+ | IF-FW-ROAD-11C, 11D | Parcial / Ausente | Coberto com ajuste / Deferred | Core | Baseline ADRs + API/identity policy | Baixo / controlado | Como padrão de consumer/adapters, não Session core. |
| Session diagnostics/facts | Preservar como `FrameworkFact` mínimo | F1 | F1 | IF-FW-ROAD-1C | Parcial / Ausente | Coberto | Core | Baseline ADRs + API/identity policy | Baixo / controlado | Sem ajuste. |

### Route
| Capacidade | Decisão | Fase original | Fase revisada | Corte | Status package | Status roadmap | Prioridade | Bloqueadores | Risco | Observação |
|---|---|---|---|---|---|---|---|---|---|---|
| Route asset declarativo | Preservar; dividir campos de subsistemas | F3 | F3 | IF-FW-ROAD-3A | Presente / Parcial | Coberto | Core | Route baseline + Content identity | Baixo / controlado | Sem ajuste. |
| Route identity tipada | Redesenhar como typed ID | F1 | F1 | IF-FW-ROAD-1B | Parcial / Ausente | Coberto | Core | Route baseline + Content identity | Baixo / controlado | Sem ajuste. |
| Route runtime state | Preservar como `RouteRuntimeState` | F3 | F3 | IF-FW-ROAD-3A | Implementado em F3B / Smoke PASS | Coberto | Core | RuntimeContentHandle + ContributionSet + release policy | Baixo / controlado | Fechado. |
| Route exit plan | Preservar como `RouteExitResult` | F3 | F3 | IF-FW-ROAD-3B | Implementado em F3C / Smoke PASS | Coberto | Core | Route baseline + Content identity | Baixo / controlado | Resultado mínimo explícito; sem release execution. |
| Route content set semantics | Separar registro de ownership explícito | F3 | F3 | IF-FW-ROAD-3D | Implementado em F3E / Smoke PASS | Coberto | Core | Route baseline + Content identity | Baixo / controlado | `RouteContentEntry` + `RouteContentOwnership`; Primary Scene baseline é Owned; smoke PASS. |
| Scene composition plan/result | Preservar plan/result explícitos | F6 | F6 | IF-FW-ROAD-6A, 6B, 6C | F6B/F6C implementados / Smoke PASS | F6A-F6C fechados | Core | Route baseline + F5 LocalContribution + F6 ADRs | Baixo / controlado | Plan/result inertes criados; execução de RouteContentProfileAsset fica para F6E. |
| Primary scene loading | Preservar; já existe no package | F3 | F3 | IF-FW-ROAD-3C | Presente / Integrado em F3D / Smoke PASS | Coberto | Core | Route baseline + Content identity | Baixo / controlado | RouteContentRuntime entra após load da Primary Scene e antes da Startup Activity. |
| Additive scene loading | Preservar com plan/result | F6 | F6 | IF-FW-ROAD-6D | F6 closed / additive route scene composition pass | Coberto por F6D/F6E; profile execution validado | Core | RouteSceneCompositionPlan/Result aceitos | Baixo / controlado | Primitivo interno em SceneLifecycle; Route consome additional scenes via profile. |
| Active scene policy | Preservar como parte do plan | F6 | F6 | IF-FW-ROAD-6A | Implementado no plan F6B | F6B fechado | Core | RouteSceneCompositionPlan | Baixo / controlado | Política inicial: PrimarySceneActive. |
| Route content profile execution | Preservar execução; separar de planning | F6 | F6 | IF-FW-ROAD-6E | F6E closed / profile smoke pass | Coberto | Core | Plan/result + additive primitive | Baixo / controlado | RouteContentProfileAsset executa additional scenes via composition; release ocorre em F6G. |
| Route → Activity handoff | Preservar boundary; reduzir payload | F3 | F3 | IF-FW-ROAD-3C | Integrado em F3D / Smoke PASS | Coberto | Core | Route baseline + Content identity | Baixo / controlado | RouteContentRuntime roda antes da Startup Activity. |
| Route startup activity policy | Preservar como policy simples | F3 | F3 | IF-FW-ROAD-3C | Preservado em F3D / Smoke PASS | Coberto | Core | Route baseline + Content identity | Baixo / controlado | Startup Activity continua depois do enter local da Route. |
| Route local callback smoke | Validar callbacks reais de Route Content | F3 | F3 | IF-FW-ROAD-3E | Implementado em F3F / Callback-smoke PASS | Coberto | QA / Dev Tooling | RouteContentRuntime ativo | Baixo / controlado | `Run Route Callback Smoke` validado com receivers reais sob `RouteContentBinding`; sem falso positivo com zero receivers. |
| QA panel simplification | Reduzir ruído do painel QA | F3 | F3 | IF-FW-ROAD-3E support | Aplicado em F3F1 / Standard Smoke PASS | Coberto | QA / Dev Tooling | F3F aplicado | Baixo | Painel default mostra core smokes e callback smoke; controles manuais/edge ficam avançados. |
| Route validator expansion | Detectar configuração incorreta de Route Content | F3 | F3 | IF-FW-ROAD-3F | Implementado em F3G/F3G1 / Smoke PASS | Coberto | Editor / QA | F3F fechado | Baixo / controlado | Valida `RouteContentBinding` em cenas carregadas via QA: Route ausente, Route de cena errada e receivers ausentes. Inspector fica reduzido a tooltip; F3 fechada. |
| Route contribution set | Redesenhar como `RouteContributionSet` | F5+ | F5/F7/F10+ | F7 via Content Anchor | Parcial / Ausente | Coberto com ajuste / Deferred | Core | Route baseline + Content identity | Baixo / controlado | Contribution genérico em F5; Content Anchor em F7; consumers reais depois. |
| Route Content Anchor set | Redesenhar como `ContentAnchorSet` passivo + Route discovery diagnóstico | F7 | F7 | IF-FW-ROAD-7G | Fechado em F7E/F7F / PASS | Coberto | Core | Runtime binding só na F9 | Baixo / controlado | Não criar `RouteContentAnchorSet` específico no baseline; `ContentAnchorSet` único é escopado pelos dados e consumido pela Route. |
| Content release plan | Preservar como `ContentReleasePlan` | F6 | F6 | IF-FW-ROAD-6F | F6 closed / release smoke pass | F6G fechado por Route Release Smoke | Core | Scene composition result + ownership explícito | Baixo / controlado | Release físico limitado a owned additive scene unload; LocalContributionHandle não é release handle. |

### Activity
| Capacidade | Decisão | Fase original | Fase revisada | Corte | Status package | Status roadmap | Prioridade | Bloqueadores | Risco | Observação |
|---|---|---|---|---|---|---|---|---|---|---|
| Activity asset declarativo | Preservar; reduzir fields cross-cutting | F4 | F4 | IF-FW-ROAD-4A | Presente / Parcial | Coberto | Core | Route handoff + ActivityContentSet | Baixo / controlado | Sem ajuste. |
| Activity content profile | Preservar como perfil separado | F4 | Deferred após F4/F6 | IF-FW-ROAD-4B | Parcial / Ausente | Coberto com ajuste / Deferred | Core | Route handoff + ActivityContentSet | Risco de entrar cedo demais | F4B não ativa profile loading; profile avançado vem depois de scene composition/release. |
| Activity runtime state | Redesenhar com typed IDs | F4 | F4A | IF-FW-ROAD-4A | Presente / Parcial | Coberto | Core | ActivityContentSet + readiness mínimo | Baixo / controlado | F4A fechado; `ActivityRuntimeState` usa status `None`/`Active`, identidade `Activity:*` e `Transitioning` reservado. |
| Activity entry context | Preservar boundary; reduzir payload | F4 | F4 | IF-FW-ROAD-4B | Parcial / Ausente | Coberto | Core | Route handoff + ActivityContentSet | Baixo / controlado | Sem ajuste. |
| Activity content set | Preservar como `ActivityContentSet` | F4 | F4B | IF-FW-ROAD-4B | Presente mínimo | Coberto | Core | Route handoff + ActivityContentSet | Baixo / controlado | F4B fechado; snapshot mínimo de conteúdo scene-authored local; profile/loading/release continuam fora. |
| Activity content loading | Preservar com plan/result/handles | F4–F6 | F6+ | IF-FW-ROAD-4B, 6D | Parcial / Ausente | Coberto com ajuste / Deferred | Core | Route handoff + ActivityContentSet | Baixo / controlado | Carregamento real de cenas da Activity depende de plan/result/release. |
| Activity content lifecycle result | Criar como `ActivityContentLifecycleResult` | F4 | F4C | IF-FW-ROAD-4C | Presente mínimo | Coberto | Core | Route handoff + ActivityContentSet | Baixo / controlado | F4C fechado; resultado agregado de callbacks locais enter/exit; não é release/loading. |
| Readiness gate | Preservar como state/result | F4 | F4D/F4F | IF-FW-ROAD-4D, IF-FW-ROAD-4F | Presente mínimo / Smoke dedicado fechado | Coberto | Core | Route handoff + ActivityContentSet | Baixo / controlado | F4D adiciona `ActivityReadinessState`; F4F fecha smoke dedicado de Activity baseline com readiness/content/clear/restore. |
| Activity baseline smoke | Validar switch/content/readiness/clear | F4 | F4F | IF-FW-ROAD-4F | Implementado / Smoke PASS | Coberto | QA / Dev Tooling | F4A-F4E | Baixo / controlado | `Run Activity Baseline Smoke` validado; cobre Secondary com content handle, Primary sem handle local, clear para None e restore. |
| Activity exit plan | Preservar como `ActivityExitPlan` | F4 | F6/F8 | IF-FW-ROAD-4B | Parcial / Ausente | Coberto, mas com correção | Core | Route handoff + ActivityContentSet | Baixo / controlado | Exit simples em F4; plano de release completo após release/runtime policy. |
| Activity reset plan | Preservar; entrar depois de LocalContributionSet | F5+ | Futura após F5/F10 | — | Parcial / Ausente | Coberto com ajuste / Deferred | Core | Route handoff + ActivityContentSet | Risco de ficar sem corte rastreável | Depende de LocalContributionSet e snapshot/reset participants. |
| Participant binding | Preservar boundary; redesenhar ownership | F10–F11 | F10–F11 | IF-FW-ROAD-10B, 11F | Parcial / Ausente | Coberto | Core | Route handoff + ActivityContentSet | Baixo / controlado | Sem ajuste. |
| Pause content/Content Anchor contribution | Redesenhar como Content Anchor consumer | F9–F10 | F9–F10 | IF-FW-ROAD-9D, 10G | Parcial / Ausente | Coberto | Core | LocalContributionSet para discovery; Runtime binding só na F9 | Baixo / controlado | Sem ajuste. |
| Activity snapshot | Preservar contrato; entrar após snapshot model | F10 | F10 | IF-FW-ROAD-10D, 10E | Parcial / Ausente | Coberto | Core | Route handoff + ActivityContentSet | Baixo / controlado | Sem ajuste. |
| Activity diagnostics/facts | Preservar como `FrameworkFact` | F1 | F1 | IF-FW-ROAD-1C | Parcial / Ausente | Coberto | Core | Route handoff + ActivityContentSet | Baixo / controlado | Sem ajuste. |

### Local
| Capacidade | Decisão | Fase original | Fase revisada | Corte | Status package | Status roadmap | Prioridade | Bloqueadores | Risco | Observação |
|---|---|---|---|---|---|---|---|---|---|---|
| Scene-authored local binding | Preservar como `ActivityLocalVisibilityAdapter` / `RouteContentBinding` | F5 | F5 | IF-FW-ROAD-5C | Implementado em F5C / Smoke PASS | Coberto | Core | ActivityContentSet/RouteContentSet + typed identity | Baixo / controlado | Não há marker paralelo; F5C usa os bindings/adapters reais com `Local Content Id` explícito. |
| Local content identity | Redesenhar como `LocalContentIdentity` | F5 | F5 | IF-FW-ROAD-5A, 5B | CLOSED / Standard compile-smoke pass | Coberto | Core | ActivityContentSet/RouteContentSet + typed identity | Risco de identity/discovery frágil | F5B cria `LocalContentIdentity`, `LocalContentId` e `LocalContentScopeKind`; sem marker/discovery. |
| Scoped contribution discovery | Preservar discovery scoped; remover scan global | F5 | F5 | IF-FW-ROAD-5D | Discovery carregado fechado / QA compile-smoke pass | Coberto parcial | Core | ActivityContentSet/RouteContentSet + typed identity | Risco de identity/discovery frágil | F5D produz LocalContributionDiscovery/Set/Handle a partir dos bindings/adapters carregados com LocalContentId explícito; integração formal por ContentSet fica para corte posterior. |
| Contribution set | Criar `LocalContributionSet` | F5 | F5 | IF-FW-ROAD-5D/5E | F5E closed / QA compile-smoke pass | Coberto parcial | Core | ActivityContentSet/RouteContentSet + typed identity | Risco de identity/discovery frágil | F5E consolida LocalContributionSet como snapshot consultável por scope/source/identity; F5H valida o set por smoke dedicado. |
| Requiredness policy | Centralizar em policy | F5 | F5 | IF-FW-ROAD-5F/F5G/F5H | F5H closed / PASS | Coberto | Core | ActivityContentSet/RouteContentSet + typed identity | Baixo / controlado | F5F registra Required/Optional nos handles; F5G adiciona validator policy para expected required/optional sem criar authoring declarativo; F5H validou o smoke dedicado sem materialização. |
| Capability inventory | Preservar como `LocalCapabilityDescriptor` | F5 | F5 parcial / Runtime refs depois | Pós-F5E | Parcial / Ausente | Coberto, mas com correção | Core | ActivityContentSet/RouteContentSet + typed identity | Risco de entrar cedo demais | F5E não cria capability descriptor; inventory vivo/runtime reference fica depois de RuntimeHandle. |
| Runtime capability reference | Preservar com validade por lifecycle | F8+ | F8+ | após RuntimeHandle | Parcial / Ausente | Coberto com ajuste / Deferred | Core | RuntimeContentHandle + ContributionSet + release policy | Risco de identity/discovery frágil | Correto: depende de RuntimeContentHandle/lifetime validity. |
| Local reset participant | Preservar contrato | F8+ | F10+ | após LocalContributionSet | Parcial / Ausente | Coberto com ajuste / Deferred | Core | ActivityContentSet/RouteContentSet + typed identity | Risco de identity/discovery frágil | Depende de LocalContributionSet e capability participant model. |
| Local snapshot participant | Preservar contrato com typed payload | F10 | F10 | IF-FW-ROAD-10D | Parcial / Ausente | Coberto | Core | ActivityContentSet/RouteContentSet + typed identity | Risco de identity/discovery frágil | Sem ajuste. |
| Local release participant | Preservar contrato | F8 | F8/F10 | IF-FW-ROAD-8G | Parcial / Ausente | Coberto, mas com correção | Core | ActivityContentSet/RouteContentSet + typed identity | Risco de identity/discovery frágil | Release policy vem F8; participant local específico depois. |
| Exit freeze | Preservar como parte de `ActivityExitPlan` | F8 | F8/F10 | IF-FW-ROAD-8G | Parcial / Ausente | Coberto, mas com correção | Core | ActivityContentSet/RouteContentSet + typed identity | Risco de identity/discovery frágil | Só faz sentido quando há runtime references/release participants. |
| Local slots | Consolidar como `ContentAnchorSlot`; não criar `LocalSlot` separado | F7 | F7 | IF-FW-ROAD-7D | Fechado em F7C / PASS | Coberto com ajuste | Core | ContentAnchor identity/declaration | Baixo / controlado | Coberto por `ContentAnchorSlot`; evita vocabulário paralelo e componente local prematuro. |
| Local anchors | Consolidar como `ContentAnchorPoint`; não criar `LocalAnchor` separado | F7 | F7 | IF-FW-ROAD-7E | Fechado em F7C / PASS | Coberto com ajuste | Core | ContentAnchor identity/declaration | Baixo / controlado | Coberto por `ContentAnchorPoint`; evita duplicar conceito de posicionamento/localização. |
| Stale/foreign reference check | Preservar como validação de `LocalContributionSet` | F5 | F5 | IF-FW-ROAD-5G/F5H | F5H closed / PASS | Coberto | Core | ActivityContentSet/RouteContentSet + typed identity | Baixo / controlado | Validator e smoke dedicado validaram identity/set sem usar fallback por nome/path. |

### RuntimeSpawned
| Capacidade | Decisão | Fase original | Fase revisada | Corte | Status package | Status roadmap | Prioridade | Bloqueadores | Risco | Observação |
|---|---|---|---|---|---|---|---|---|---|---|
| Runtime ownership primitives | Criar scope/owner/state/identity tipados | F8 | F8 | IF-FW-ROAD-8B | Implementado em F8B | Coberto / Aplicado | Foundation avançada | Content identity + release semantics | Baixo / controlado | Base de identidade para runtime-created content; sem materialização. |
| Runtime content handle | Criar `RuntimeContentHandle` | F8 | F8 | IF-FW-ROAD-8C | Implementado em F8C | Coberto / Aplicado | Foundation avançada | Runtime ownership primitives | Baixo / controlado | Handle passivo com estado e diagnostics de release; sem side effect físico. |
| Runtime scope root | Redesenhar como `RuntimeScopeRoot` com registry | F8 | F8 | IF-FW-ROAD-8D | Implementado em F8D | Coberto / Aplicado | Foundation avançada | RuntimeContentHandle + owner identity | Baixo / controlado | Root lógico por owner; não cria hierarchy GameObject. |
| Runtime root registry | Criar `RuntimeRootRegistry` | F8 | F8 | IF-FW-ROAD-8D | Implementado em F8D | Coberto / Aplicado | Foundation avançada | RuntimeContentHandle + owner identity | Baixo / controlado | Registry interno, explicit roots only; sem `GameObject.Find` e sem fallback root. |
| Runtime content owner runtime/context | Criar `RuntimeContentRuntime` + `RuntimeScopeContext` | F8 | F8 | IF-FW-ROAD-8E | Implementado em F8E | Coberto / Aplicado | Foundation avançada | RuntimeRootRegistry | Baixo / controlado | Owner interno coordena registry/context/handles; contexto explícito por owner antes de request/result. |
| Lifecycle integration for runtime roots | Conectar criação/remoção explícita de roots/contextos ao lifecycle | F8 | F8 | IF-FW-ROAD-8F | Implementado em F8F | Coberto / Aplicado | Foundation avançada | RuntimeContentRuntime + RuntimeScopeContext | Baixo / controlado | Session/Route/Activity criam/removem roots lógicos sem executar materialização física ainda. |
| Materialization request/result | Preservar como `RuntimeMaterializationRequest/Result` | F8 | F8 | IF-FW-ROAD-8G | Implementado em F8G | Coberto / Aplicado | Foundation avançada | RuntimeContentRuntime + lifecycle context | Baixo / controlado | Request/result/resource/status definidos sem adapter físico, sem UnityEngine e sem Content Anchor binding. |
| Transition guard / scoped cancellation | Guardar transições e cancelamento por scope | F8 | F8 | IF-FW-ROAD-8H | Implementado em F8H | Coberto / Aplicado | Foundation avançada | Materialization request/result | Baixo / controlado | RuntimeScopeTransitionState, RuntimeScopeCancellationToken e guard interno impedem request stale/cancelado antes de qualquer adapter físico. |
| Materialization adapter boundary | Preservar como adapter físico fora do core | F8 | F8 | IF-FW-ROAD-8I | Implementado em F8I | Coberto / Aplicado | Foundation avançada | Request/result + transition guard | Baixo / controlado | `IRuntimeMaterializationAdapter` define boundary; prefab/cena/pool continuam fora do core. |
| Runtime release policy | Criar `RuntimeReleasePolicy` por escopo | F8 | F8 | IF-FW-ROAD-8J | Aplicado / compile-smoke pass | Coberto | Foundation avançada | Handle state + transition guard + release request/result | Baixo / controlado | Release lógico por handle/escopo aplicado; Destroy, pool return, Addressables e scene unload ficam em adapters/trilhos específicos fora do core. |
| Runtime content closure smoke | Validar request/guard/apply/release/root removal sem materialização física | F8 | F8 | IF-FW-ROAD-8K | Aplicado / pendente smoke | Coberto | Foundation avançada | Materialization request/result + release policy | Baixo / controlado | `Run Runtime Content Smoke` valida `ApplyMaterializationResult`, release lógico, unregister, remoção de root e rejeição de request stale. |
| Pooled materializer | Redesenhar como `PooledContentMaterializer` | F11 | F11 | IF-FW-ROAD-11H | Ausente | Coberto | Foundation avançada | RuntimeContentHandle + release policy | Risco de consumer capturar core | Sem ajuste. |
| Runtime spawned contribution | Preservar como `RuntimeSpawnedContribution` | F11 | F11 | IF-FW-ROAD-11F | Ausente | Coberto | Foundation avançada | RuntimeContentHandle + ContributionSet + release policy | Risco de consumer capturar core | Sem ajuste. |
| Spawn origin / slot | Integrar com `ContentAnchorSlot` / `ContentAnchorPoint` | F9 | F9 | IF-FW-ROAD-9D | Ausente | Coberto | Foundation avançada | Runtime materialization + ContentAnchor binding | Risco de ownership/lifetime incompleto | Não usar `LocalAnchor` como conceito separado. |
| Runtime identity | Preservar typed; generalizar para todo spawned | F8 | F8 | IF-FW-ROAD-8B/8C | Implementado em F8B/F8C | Coberto / Aplicado | Foundation avançada | Content identity policy | Baixo / controlado | RuntimeContentId/Identity + RuntimeContentHandle cobrem a identidade base. |
| Pool rent/return cycle | Preservar como infra técnica separada | F11 | F11 | IF-FW-ROAD-11G, 11H | Ausente | Coberto | Foundation avançada | RuntimeContentHandle + release policy | Risco de ownership/lifetime incompleto | Sem ajuste. |
| Physical destroy policy | Deferir para adapters físicos | Pós-F8 | F9/F11+ | Deferred | Ausente / Deferred | Coberto com ajuste / Deferred | Adapter/Consumer | Runtime release policy + adapter físico específico | Risco de capturar core | `Destroy` explícito não pertence ao core F8; deve aparecer em adapter Unity/pooling/consumer próprio. |

### Content Anchor
| Capacidade | Decisão | Fase original | Fase revisada | Corte | Status package | Status roadmap | Prioridade | Bloqueadores | Risco | Observação |
|---|---|---|---|---|---|---|---|---|---|---|
| Content Anchor identity primitives | Criar `ContentAnchorId`, `ContentAnchorScope`, `ContentAnchorKind` e `ContentAnchorRequiredness` | F7 | F7 | IF-FW-ROAD-7B | Fechado em F7B / PASS | Coberto | Foundation avançada | LocalContributionSet para discovery; Runtime binding só na F9 | Risco de ownership/lifetime incompleto | Sem ajuste. |
| Content Anchor root | Criar `ContentAnchorRoot` com role tipado | F7 | F7 | IF-FW-ROAD-7C | Fechado em F7C / PASS | Coberto | Foundation avançada | LocalContributionSet para discovery; Runtime binding só na F9 | Risco de ownership/lifetime incompleto | Sem ajuste. |
| Content Anchor slot | Criar `ContentAnchorSlot` typed | F7 | F7 | IF-FW-ROAD-7D | Fechado em F7C / PASS | Coberto | Foundation avançada | LocalContributionSet para discovery; Runtime binding só na F9 | Risco de ownership/lifetime incompleto | Sem ajuste. |
| Content Anchor point | Criar `ContentAnchorPoint` typed | F7 | F7 | IF-FW-ROAD-7E | Fechado em F7C / PASS | Coberto | Foundation avançada | LocalContributionSet para discovery; Runtime binding só na F9 | Risco de ownership/lifetime incompleto | Ponto semântico não implica montagem. |
| Route Content Anchor authoring | Criar `RouteContentAnchor` como primeiro componente público | F7 | F7 | IF-FW-ROAD-7F | Fechado em F7D / PASS | Coberto | Foundation avançada | RouteContentSet + loaded scene discovery; Runtime binding só na F9 | Risco de ownership/lifetime incompleto | Preferir componente público por escopo em vez de endpoint genérico se isso melhorar UX de Inspector. |
| Content Anchor set por scope | Criar `ContentAnchorSet` passivo; discovery por scope em cortes separados | F7 | F7 | IF-FW-ROAD-7G | Fechado em F7E/F7F / PASS | Coberto | Foundation avançada | Runtime binding só na F9 | Baixo / controlado | Set único passivo; Route discovery implementado em F7F. Activity discovery permanece futuro. |
| Content Anchor binding request/result | Criar `ContentAnchorBindingRequest/Result` | F9 | F9 | IF-FW-ROAD-9A, 9B | Ausente | Coberto | Foundation avançada | LocalContributionSet para discovery; Runtime binding só na F9 | Risco de ownership/lifetime incompleto | Sem ajuste. |
| Content Anchor content handle | Criar `ContentAnchorContentHandle` | F9 | F9 | IF-FW-ROAD-9C | Ausente | Coberto | Foundation avançada | LocalContributionSet para discovery; Runtime binding só na F9 | Risco de ownership/lifetime incompleto | Sem ajuste. |
| Runtime Content Anchor binding | Criar `RuntimeContentAnchorBinding` e smoke dedicado | F9 | F9 | IF-FW-ROAD-9D/9E/9G | F9J CLOSED / logical binding pass | Coberto | Foundation avançada | Binding lógico resolve ContentAnchorSet + RuntimeContentHandle; F9C/F9D/F9E/F9F/F9I validam bind idempotente, unbind, release lógico, host ownership, Route/Activity cleanup e Activity anchor binding; placement físico ainda futuro | Risco controlado; cleanup automático lógico validado; placement físico ainda futuro | Fechado em F9J; regressão opcional se binding/lifecycle mudar. |
| Duplicate detection | Preservar em Content Anchor validators | F7 | F7 | IF-FW-ROAD-7H | Fechado em F7H / PASS | Coberto | Foundation avançada | ContentAnchorSet + Route discovery | Baixo / controlado | Validado por Loaded Authoring e Content Anchor Diagnostics Smoke: duplicidade de identity/id zerada. |
| Content Anchor lifecycle policy | Preservar policy explícita de cleanup/release lógico | F9 | F9 | IF-FW-ROAD-9H | F9J CLOSED / logical binding pass | Coberto | Foundation avançada | RuntimeContentAnchorBinding + RuntimeContentHandle + release lógico + FrameworkRuntimeHost ownership | Risco controlado; cleanup automático validado | F9D adiciona cleanup/snapshots; F9E expõe owner/scope cleanup por API interna do host; F9F executa e valida cleanup lógico automático em Route/Activity exit; F9J fecha o bloco lógico; sem placement físico. |
| Overlay root separado de content root | Reposicionar para consumers/layout após binding | F7 | F9/F10 | Deferred | Ausente / Deferred | Coberto com ajuste / Deferred | Consumer intermediário | RuntimeRoot/materialization + Content Anchor binding + Pause/UI consumer | Risco de entrar cedo demais | Não é baseline de declaração F7; depende de runtime placement e consumers de UI/Pause. |

### Input
| Capacidade | Decisão | Fase original | Fase revisada | Corte | Status package | Status roadmap | Prioridade | Bloqueadores | Risco | Observação |
|---|---|---|---|---|---|---|---|---|---|---|
| Input mode contract | Redesenhar como `InputModeContract` typed | F10 | F10 | IF-FW-ROAD-10A, 10B | Ausente | Coberto | Consumer intermediário | Input ownership ADR + Activity state | Baixo / controlado | Sem ajuste. |
| Activity input consumer | Redesenhar como consumer de input | F10 | F10 | IF-FW-ROAD-10B | Ausente | Coberto | Consumer intermediário | Input ownership ADR + Activity state | Baixo / controlado | Sem ajuste. |
| Input mode owner | Criar `InputModeOwner` explícito | F10 | F10 | IF-FW-ROAD-10A | Ausente | Coberto | Consumer intermediário | Input ownership ADR + Activity state | Baixo / controlado | Sem ajuste. |
| Player slot / command binding | Preservar boundary; redesenhar ownership | F10–F11 | F10–F11 | IF-FW-ROAD-10B, 11F | Ausente | Coberto | Consumer intermediário | Input ownership ADR + Activity state | Baixo / controlado | Sem ajuste. |

### Save / Snapshot
| Capacidade | Decisão | Fase original | Fase revisada | Corte | Status package | Status roadmap | Prioridade | Bloqueadores | Risco | Observação |
|---|---|---|---|---|---|---|---|---|---|---|
| Snapshot envelope | Redesenhar como `SnapshotEnvelope` typed | F10 | F10 | IF-FW-ROAD-10C, 10D | Ausente | Coberto | Consumer intermediário | LocalContributionSet + SnapshotEnvelope | Baixo / controlado | Sem ajuste. |
| Snapshot participant contract | Preservar como `ISnapshotParticipant` | F10 | F10 | IF-FW-ROAD-10D | Ausente | Coberto | Consumer intermediário | LocalContributionSet + SnapshotEnvelope | Baixo / controlado | Sem ajuste. |
| Snapshot set | Criar `SnapshotSet` | F10 | F10 | IF-FW-ROAD-10E | Ausente | Coberto | Consumer intermediário | LocalContributionSet + SnapshotEnvelope | Baixo / controlado | Sem ajuste. |
| Save backend port | Preservar como port/adapter | F10 | F10 | IF-FW-ROAD-10C | Ausente | Coberto | Consumer intermediário | LocalContributionSet + SnapshotEnvelope | Baixo / controlado | Sem ajuste. |
| Schema versioning | Criar como parte do `SnapshotEnvelope` | F10 | F10 | IF-FW-ROAD-10C | Ausente | Coberto | Consumer intermediário | LocalContributionSet + SnapshotEnvelope | Baixo / controlado | Sem ajuste. |

### Pause
| Capacidade | Decisão | Fase original | Fase revisada | Corte | Status package | Status roadmap | Prioridade | Bloqueadores | Risco | Observação |
|---|---|---|---|---|---|---|---|---|---|---|
| Pause content anchor consumer | Redesenhar como `PauseContentAnchorConsumer` | F10 | F10 | IF-FW-ROAD-10F, 10G | Ausente | Coberto | Consumer intermediário | ContentAnchorSet + Runtime/Binding conforme escopo | Baixo / controlado | Sem ajuste. |
| Pause state contract | Criar `PauseStateContract` | F10 | F10 | IF-FW-ROAD-10F | Ausente | Coberto | Consumer intermediário | ContentAnchorSet + Runtime/Binding conforme escopo | Baixo / controlado | Sem ajuste. |
| Pause content materialization | Usa `RuntimeContentAnchorBinding` | F9–F10 | F9–F10 | IF-FW-ROAD-9D, 10G | Ausente | Coberto | Consumer intermediário | ContentAnchorSet + Runtime/Binding conforme escopo | Baixo / controlado | Sem ajuste. |
| Pause/unpause lifecycle | Preservar boundary: pause recebe evento, não controla Activity | F10 | F10 | IF-FW-ROAD-10F | Ausente | Coberto | Consumer intermediário | ContentAnchorSet + Runtime/Binding conforme escopo | Baixo / controlado | Sem ajuste. |

### Camera
| Capacidade | Decisão | Fase original | Fase revisada | Corte | Status package | Status roadmap | Prioridade | Bloqueadores | Risco | Observação |
|---|---|---|---|---|---|---|---|---|---|---|
| Camera consumer | Redesenhar como `CameraConsumer` | F11 | F11 | IF-FW-ROAD-11A, 11B | Ambíguo / Experimental | Coberto | Consumer avançado | ContentAnchorSet + Runtime/Binding conforme escopo | Risco de consumer capturar core | Sem ajuste. |
| Camera anchor binding | Redesenhar com `ContentAnchor` typed | F11 | F11 | IF-FW-ROAD-11B | Ambíguo / Experimental | Coberto | Consumer avançado | ContentAnchorSet + Runtime/Binding conforme escopo | Risco de consumer capturar core | Sem ajuste. |
| Camera scope (Session/Route/Activity) | Preservar modelo de escopo | F11 | F11 | IF-FW-ROAD-11B | Ambíguo / Experimental | Coberto | Consumer avançado | ContentAnchorSet + Runtime/Binding conforme escopo | Risco de consumer capturar core | Sem ajuste. |
| Camera binding result/handle | Criar `CameraBindingResult` com handle | F11 | F11 | IF-FW-ROAD-11B | Ambíguo / Experimental | Coberto | Consumer avançado | ContentAnchorSet + Runtime/Binding conforme escopo | Risco de consumer capturar core | Sem ajuste. |
| Virtual camera binding | Redesenhar como consumer de Content Anchor; não no core | F11 | F11 | IF-FW-ROAD-11B | Ambíguo / Experimental | Coberto | Consumer avançado | ContentAnchorSet + Runtime/Binding conforme escopo | Risco de consumer capturar core | Sem ajuste. |

### Audio
| Capacidade | Decisão | Fase original | Fase revisada | Corte | Status package | Status roadmap | Prioridade | Bloqueadores | Risco | Observação |
|---|---|---|---|---|---|---|---|---|---|---|
| BGM lifecycle consumer | Preservar como consumer de lifecycle | F11 | F11 | IF-FW-ROAD-11C, 11D | Ausente | Coberto | Consumer avançado | Baseline reconciliado | Risco de consumer capturar core | Sem ajuste. |
| SFX dispatch | Preservar como port/adapter | F11 | F11 | IF-FW-ROAD-11D | Ausente | Coberto | Consumer avançado | Baseline reconciliado | Risco de consumer capturar core | Sem ajuste. |
| Audio listener | Preservar em `SessionContentSet` | F2 | F11 ou SessionContent futuro | IF-FW-ROAD-2C | Ausente | Coberto, mas com correção | Consumer avançado | Baseline reconciliado | Risco de consumer capturar core | Não colocar audio listener em F2 técnico ainda; tratar como consumer/persistent content futuro. |

### Actor
| Capacidade | Decisão | Fase original | Fase revisada | Corte | Status package | Status roadmap | Prioridade | Bloqueadores | Risco | Observação |
|---|---|---|---|---|---|---|---|---|---|---|
| Actor materialization request/result | Redesenhar como `ActorMaterializationRequest/Result` | F11 | F11 | IF-FW-ROAD-11E, 11F | Ausente | Coberto | Consumer avançado | RuntimeContentHandle + ContributionSet + release policy | Risco de consumer capturar core | Sem ajuste. |
| Actor contribution | Criar `ActorContribution` | F11 | F11 | IF-FW-ROAD-11F | Ausente | Coberto | Consumer avançado | RuntimeContentHandle + ContributionSet + release policy | Risco de consumer capturar core | Sem ajuste. |
| Actor runtime identity | Preservar typed; especializar `RuntimeContentIdentity` | F11 | F11 | IF-FW-ROAD-11F | Ausente | Coberto | Consumer avançado | RuntimeContentHandle + ContributionSet + release policy | Risco de consumer capturar core | Sem ajuste. |
| Actor reset/release | Preservar via `LocalResetParticipant` / `LocalReleaseParticipant` | F11 | F11 | IF-FW-ROAD-11F | Ausente | Coberto | Consumer avançado | RuntimeContentHandle + ContributionSet + release policy | Risco de consumer capturar core | Sem ajuste. |
| Player actor binding | Preservar boundary; redesenhar sem pipeline monolítica | F11 | F11 | IF-FW-ROAD-11F | Ausente | Coberto | Consumer avançado | RuntimeContentHandle + ContributionSet + release policy | Risco de consumer capturar core | Sem ajuste. |

### Pooling
| Capacidade | Decisão | Fase original | Fase revisada | Corte | Status package | Status roadmap | Prioridade | Bloqueadores | Risco | Observação |
|---|---|---|---|---|---|---|---|---|---|---|
| Pool rent | Preservar como infra técnica em package separado | F11 | F11 | IF-FW-ROAD-11G | Ausente | Coberto | Consumer avançado | RuntimeContentHandle + ContributionSet + release policy | Risco de consumer capturar core | Sem ajuste. |
| Pool return | Preservar; integrar com `RuntimeReleasePolicy` | F11 | F11 | IF-FW-ROAD-11H | Ausente | Coberto | Consumer avançado | RuntimeContentHandle + ContributionSet + release policy | Risco de consumer capturar core | Sem ajuste. |
| Pool ownership | Desacoplar de gameplay; package independente | F11 | F11 | IF-FW-ROAD-11G | Ausente | Coberto | Consumer avançado | RuntimeContentHandle + ContributionSet + release policy | Risco de consumer capturar core | Sem ajuste. |

### Projectile / Damage / Attributes
| Capacidade | Decisão | Fase original | Fase revisada | Corte | Status package | Status roadmap | Prioridade | Bloqueadores | Risco | Observação |
|---|---|---|---|---|---|---|---|---|---|---|
| Projectile as RuntimeSpawned | Redesenhar como `RuntimeSpawned` com pool return | F12 | F12 | IF-FW-ROAD-12A | Ausente | Coberto | Gameplay avançado | RuntimeContentHandle + ContributionSet + release policy | Risco de consumer capturar core | Sem ajuste. |
| Impact/collision handling | Preservar boundary: impact não controla projectile | F12 | F12 | IF-FW-ROAD-12B | Ausente | Coberto | Gameplay avançado | RuntimeContentHandle + ContributionSet + release policy | Risco de consumer capturar core | Sem ajuste. |
| Damage as actor capability | Preservar como capability; sem acesso a Actor internals | F12 | F12 | IF-FW-ROAD-12C | Ausente | Coberto | Gameplay avançado | RuntimeContentHandle + ContributionSet + release policy | Risco de consumer capturar core | Sem ajuste. |
| Attributes as snapshot-capable | Preservar com `ISnapshotParticipant` | F12 | F12 | IF-FW-ROAD-12D | Ausente | Coberto | Gameplay avançado | RuntimeContentHandle + ContributionSet + release policy | Risco de consumer capturar core | Sem ajuste. |

### Diagnostics / QA
| Capacidade | Decisão | Fase original | Fase revisada | Corte | Status package | Status roadmap | Prioridade | Bloqueadores | Risco | Observação |
|---|---|---|---|---|---|---|---|---|---|---|
| FrameworkFact | Criar `FrameworkFact` mínimo | F1 | F1 | IF-FW-ROAD-1C | Parcial | Coberto | Core | Baseline reconciliado | Baixo / controlado | Sem ajuste. |
| Authoring validator | Expandir por fase | Todas | Todas | Por fase | Presente / Parcial | Coberto | Core | Baseline reconciliado | Baixo / controlado | Sem ajuste. |
| QA Canvas / smoke buttons | Preservar como dev tooling; não no runtime assembly de produto | F0B | F0B | IF-FW-ROAD-0B5 | Presente / Parcial | Coberto | Core | Baseline reconciliado | Baixo / controlado | Sem ajuste. |
| Boot fail-fast | Preservar | F1 | F1 | IF-FW-ROAD-1D | Parcial | Coberto | Core | Baseline reconciliado | Baixo / controlado | Sem ajuste. |
| Required/optional policy | Centralizar em policy por scope | F5 | F5 | IF-FW-ROAD-5F/F5G | F5H closed / QA compile-smoke pass | Coberto | Core | Baseline reconciliado | Baixo / controlado | Requiredness metadata aplicada; validator policy implementada para listas expected futuras; smoke dedicado local fechado em F5H. |
| Smoke doc (BASELINE_SMOKE) | Criar/manter `BASELINE_SMOKE.md` | F0B | F0B | IF-FW-ROAD-0B7 | Presente / Parcial | Coberto | Core | Baseline reconciliado | Baixo / controlado | Sem ajuste. |

---
## 5. Índice por fase revisado

Este índice substitui o mapeamento antigo de F10/F11/F12. Linhas históricas do catálogo original que ainda mencionem F10/F11/F12 devem ser lidas por esta tabela autoritativa.

### F0B
- Session — Boot global antes da cena
- Diagnostics / QA — QA Canvas / smoke buttons
- Diagnostics / QA — Smoke doc (BASELINE_SMOKE)

### F1
- Session — Config de runtime obrigatória
- Session — Registry imutável de config
- Session — Runtime policy (Strict/Release)
- Session — Session diagnostics/facts
- Route — Route identity tipada
- Activity — Activity diagnostics/facts
- Diagnostics / QA — FrameworkFact
- Diagnostics / QA — Boot fail-fast

### F2
- Session — Session runtime state
- Session — Session content set
- Session — Settings source policy
- Session — Startup route signal

### F3
- Route — Route asset declarativo
- Route — Route runtime state
- Route — Route exit plan
- Route — Primary scene loading
- Route — Route → Activity handoff
- Route — Route startup activity policy

### F4
- Activity — Activity asset declarativo
- Activity — Activity runtime state
- Activity — Activity entry context
- Activity — Activity content set
- Activity — Activity content lifecycle result
- Activity — Readiness gate

### F5
- Local — Scene-authored local binding
- Local — Local content identity
- Local — Scoped contribution discovery
- Local — Contribution set
- Local — Requiredness policy
- Local — Stale/foreign reference check
- Diagnostics / QA — Required/optional policy

### F6
- Route — Scene composition plan/result
- Route — Additive scene loading
- Route — Active scene policy
- Route — Route content profile execution
- Route — Content release plan

### F7
- Route — Route Content Anchor set
- Local — Local slots/anchors cobertos por ContentAnchorSlot/ContentAnchorPoint
- Content Anchor — Content Anchor identity
- Content Anchor — Content Anchor root
- Content Anchor — Content Anchor slot
- Content Anchor — Content Anchor point
- Content Anchor — Route Content Anchor authoring
- Content Anchor — Content Anchor set por scope
- Content Anchor — Duplicate detection

### F8
- RuntimeSpawned — Runtime scope root
- RuntimeSpawned — Runtime root registry
- RuntimeSpawned — RuntimeContentRuntime / RuntimeScopeContext
- RuntimeSpawned — Materialization request/result
- RuntimeSpawned — Transition guard/scoped cancellation
- RuntimeSpawned — Materialization adapter boundary
- RuntimeSpawned — Runtime release policy
- RuntimeSpawned — Runtime identity
- RuntimeSpawned — Destroy/release policy
- Local — Exit freeze baseline for runtime scopes

### F9
- RuntimeSpawned — Spawn origin / slot via Content Anchor
- Content Anchor — Content Anchor binding request/result
- Content Anchor — Content Anchor content handle
- Content Anchor — Runtime Content Anchor binding
- Content Anchor — Content Anchor lifecycle/release policy

### F10
- Transition — Transition request/policy/result
- Transition — Loading progress contract
- Transition — Input lock policy during transition/readiness
- Activity — ActivityContentProfile execution
- Activity — Activity-owned content scenes/prefabs
- Activity — Activity reset plan baseline

### F11
- Session/Activity — Participation boundary
- Session/Activity — PlayerSlot / ParticipantId / ParticipationScope contracts
- Local — Live Capability Inventory
- Local — Runtime capability reference
- Local — Local release participant
- Local — Local reset participant
- Local — Local snapshot participant boundary
- Local — Exit freeze and stale/foreign rejection
- Consumers — Consumer descriptor pattern

### F12
- Input — Input mode contract
- Input — Activity/Route/Pause input consumer
- Input — Input mode owner
- Input — Player slot / command binding
- Save / Snapshot — Snapshot envelope
- Save / Snapshot — Snapshot participant contract
- Save / Snapshot — Snapshot set
- Save / Snapshot — Save backend port
- Save / Snapshot — Schema versioning
- Save / Progression — SaveSlot, manifest, current save pointer, checkpoint/auto/manual policy, migration
- Pause — Pause content anchor consumer
- Pause — Pause state contract
- Pause — Pause lifecycle
- Activity — Activity snapshot

### F13
- Camera — Camera consumer
- Camera — Camera anchor binding
- Camera — Camera scope (Session/Route/Activity)
- Camera — Camera binding result/handle
- Camera — Virtual camera binding
- Audio — BGM lifecycle consumer
- Audio — SFX dispatch
- Audio — Audio listener as Session persistent content future
- Actor — Actor materialization request/result
- Actor — Actor contribution
- Actor — Actor runtime identity
- Actor — Actor reset/release
- Actor — Player actor binding
- Pooling — Pool rent
- Pooling — Pool return
- Pooling — Pool ownership
- RuntimeSpawned — Pooled materializer
- RuntimeSpawned — Runtime spawned contribution
- Transition — Fade/loading screen/curtain presentation adapters

### F14
- Projectile / Damage / Attributes — Projectile as RuntimeSpawned
- Projectile / Damage / Attributes — Impact/collision handling
- Projectile / Damage / Attributes — Damage as actor capability
- Projectile / Damage / Attributes — Attributes as snapshot-capable
- Cinematics — Cutscene/cinematic as Activity/Transition consumer

### F15/FX
- Productization — Settings Source Hardening
- Productization — Assembly / Build / Stripping Boundary Audit
- Productization — Documentation Hygiene
- Productization — Framework Versioning & Migration
- Productization — Pre-build Content Validation Pipeline
- Productization — Scoped Messaging Policy
- Productization — Editor Simulation / Visualizer
- Productization — Asset Provider / Addressables / DLC Boundary
- Productization — Domain Reload / Hot Reload Resilience
- Productization — Telemetry / Analytics Hooks

### Deferred / Future consumers
- Session — Session composition context
- Session — Session persistent scenes/content
- Networking / Multiplayer boundary
- Localization
- Achievements/progression completo
- Replay system
- Accessibility layer
- Remote config/experimentation

---
## 6. Gaps de rastreabilidade

Itens que precisam de detalhamento futuro porque estavam como `—`, `Fase+` ou dependem de bloqueadores:

- **Session — Pipeline de composição em fases**: Futura F2/F3. Não obrigatório como corte imediato. Preservar o padrão, mas evitar composition genérico cedo demais.
- **Session — Session composition context**: Deferred. Conceito válido, mas perigoso cedo. Primeiro formalizar Session scope/content sem service locator.
- **Session — Player participation**: realinhado para F11 como Participation Boundary antes de Input/Actor/Camera/Save.
- **Session — Subsystem composition descriptors**: realinhado para F11 como Consumer Descriptor Pattern; consumers concretos entram em F12/F13.
- **Route — Route contribution set**: F5/F7/F10+. Contribution genérico em F5; Content Anchor em F7; consumers reais depois.
- **Activity — Activity content profile**: realinhado para F10 como ActivityContentProfile execution, depois de RuntimeContent/Content Anchor foundations.
- **Activity — Activity content loading**: realinhado para F10, com plan/result/release e readiness próprios de Activity.
- **Activity — Activity reset plan**: F10 define baseline; F11 adiciona participants; F12 adiciona snapshot-backed behavior quando aplicável.
- **Local — Runtime capability reference**: realinhado para F11, após RuntimeContentHandle/lifetime validity de F8.
- **Local — Local reset participant**: realinhado para F11 junto de Local release/snapshot participants e capability runtime.

---
## 7. Checklist de uso por sprint

Antes de abrir um corte técnico, responder:

```text
1. Qual capacidade da matriz este corte cobre?
2. A capacidade está na fase correta ou foi antecipada?
3. Os bloqueadores da linha já existem?
4. Existe ADR aceito para identity/owner/release se necessário?
5. O corte cria consumer antes de core? Se sim, parar.
6. O corte adiciona string/path/name como chave funcional? Se sim, redesenhar.
7. O validator/smoke da fase será atualizado?
```

---
## 8. Apêndice — Catálogo original preservado

# NewScripts — Catálogo Compacto de Capacidades

> **Propósito:** referência rápida para garantir que nenhuma funcionalidade do NewScripts se perca no roadmap.  
> **Como usar:** ao planejar cada fase, consulte o grupo correspondente e confirme que os itens estão cobertos pelo corte técnico da fase.  
> **Regra:** cada capacidade tem uma fase-alvo no roadmap revisado. Se ela não aparecer em nenhum corte técnico daquela fase, é um sinal de que o corte precisa ser detalhado.

---

## Como ler as colunas

| Coluna | Significado |
|---|---|
| **Capacidade** | Nome funcional do que existe no NewScripts. |
| **O que faz** | Descrição em uma linha do comportamento real. |
| **Origem NS** | Classe/arquivo principal no NewScripts. |
| **Decisão** | Preservar conceito / Redesenhar shape / Descartar como API pública. |
| **Fase** | Fase do roadmap revisado onde esta capacidade deve aparecer. |
| **Corte** | ID do corte técnico correspondente (`IF-FW-ROAD-Xn`). |

---

## Grupo 1 — Session

| Capacidade | O que faz | Origem NS | Decisão | Fase | Corte |
|---|---|---|---|---|---|
| Boot global antes da cena | Inicializa runtime antes de qualquer cena Unity carregar. | `GlobalCompositionRoot.Entry` | Preservar | F0B | IF-FW-ROAD-0B7 |
| Config de runtime obrigatória | Valida `RuntimeConfigSetAsset`; falha fatal se ausente. | `RuntimeConfigRegistry` | Preservar fail-fast | F1 | IF-FW-ROAD-1D |
| Registry imutável de config | Snapshot de config disponível sem service locator. | `RuntimeConfigSnapshot` | Preservar como contexto explícito | F1 | IF-FW-ROAD-1D |
| Pipeline de composição em fases | Instala dependencies na ordem correta com detecção de ciclo. | `CompositionPipelineExecutor` | Redesenhar sem DependencyManager público | F2 | IF-FW-ROAD-2C |
| Session runtime state | Guarda estado ativo de sessão (app, mode, status). | `SessionOperationalRuntimeState` | Preservar; tipar IDs | F2 | IF-FW-ROAD-2B |
| Session composition context | Registra e fornece dependências sem service locator público. | `SessionOperationalRuntimeComposer` | Redesenhar; composição interna | F2 | IF-FW-ROAD-2C |
| Persistent scenes policy | Garante que cenas persistentes existem antes da rota inicial. | `RuntimePersistentScenesComposition`, `RuntimePersistentScenesPolicyAsset` | Preservar conceito | F2 | IF-FW-ROAD-2C |
| Session content set | Registro de conteúdo que vive acima de Route (câmera, áudio, etc.). | `SessionOperationalRuntimeComposer` (implícito) | Preservar como `SessionContentSet` | F2 | IF-FW-ROAD-2C |
| Settings source policy | Define a origem explícita de settings do bootstrap sem fallback silencioso. | `ImmersiveFrameworkSettingsAsset`, `Resources.Load` | Aceito como temporário e documentado para F2 | F2 | IF-FW-ROAD-2E |
| Startup route signal | Dispara a rota inicial após Session estar pronta. | `StartupRequestEmitter`, `StartupRouteEmitter` | Preservado via smoke de Session; evento estático global não foi criado | F2 | IF-FW-ROAD-2F |
| Runtime policy (Strict/Release) | Define comportamento em caso de erro (fatal vs. degraded). | `GlobalCompositionRoot.RuntimePolicy`, `IDegradedModeReporter` | Preservar | F1 | IF-FW-ROAD-1D |
| Player participation | Mantém participantes/slots/estado de sessão entre rotas. | `PlayerParticipationRuntime`, `SessionParticipationContext` | Preservar typed; adiar implementação | F10+ | — |
| Subsystem composition descriptors | Cada subsistema declara suas dependências separadamente. | `AudioCompositionDescriptor`, `CameraPresentationCompositionDescriptor` etc. | Preservar padrão descriptor | F11 | IF-FW-ROAD-11C, 11D |
| Session diagnostics/facts | Observabilidade da sessão com fatos estruturados. | Espalhado em logs e `SessionActivityFact` | Preservar como `FrameworkFact` mínimo | F1 | IF-FW-ROAD-1C |

---

## Grupo 2 — Route

| Capacidade | O que faz | Origem NS | Decisão | Fase | Corte |
|---|---|---|---|---|---|
| Route asset declarativo | Declara identidade da rota, cenas, startup activity. | `OperationalRouteAsset` | Preservar; dividir campos de subsistemas | F3 | IF-FW-ROAD-3A |
| Route identity tipada | Identifica rota/operação/transição sem string concatenada. | `RouteIdentity`, `RouteOperationId`, `TransitionId` | Redesenhar como typed ID | F1 | IF-FW-ROAD-1B |
| Route runtime state | Guarda estado da rota ativa para que a próxima saiba o que liberar. | `SessionOperationalRuntimeState` | Preservar como `RouteRuntimeState` | F3 | IF-FW-ROAD-3A |
| Route exit plan | Fecha a rota anterior antes de entrar na nova. | `SessionOperationalPipeline` (implícito) | Preservar como `RouteExitResult` | F3 | IF-FW-ROAD-3B |
| Scene composition plan/result | Cria plano antes de executar side effects. | `SessionOperationalPipeline` scene stages / package F6 ADR | Preservar plan/result explícitos; começar por plan inerte | F6B/F6C | IF-FW-ROAD-6A, 6B, 6C |
| Primary scene loading | Carrega cena principal da rota. | `SceneLifecycleRuntime` (package) | Preservar; já existe no package | F3 | IF-FW-ROAD-3C |
| Additive scene loading | Carrega cenas adicionais da rota em modo additive. | `SessionOperationalPipeline` scene stages / package SceneLifecycle | Preservar com plan/result; só após result | F6D | IF-FW-ROAD-6D |
| Active scene policy | Define qual cena é a ativa após composição. | Pipeline de composição de cenas | Política inicial PrimarySceneActive | F6B | IF-FW-ROAD-6A |
| Route content profile execution | Executa requiredness e carrega cenas declaradas no profile. | `RouteContentProfileAsset` + plan | Preservar execução; separar de planning | F6E | IF-FW-ROAD-6E |
| Route → Activity handoff | Passa contexto mínimo para Activity iniciar após rota pronta. | `SessionActivityEntryHandoff` | Preservar boundary; reduzir payload | F3 | IF-FW-ROAD-3C |
| Route startup activity policy | Decide qual Activity iniciar automaticamente ao entrar na rota. | `OperationalRouteAsset.StartupActivity` | Preservar como policy simples | F3 | IF-FW-ROAD-3C |
| Route contribution set | Câmera/áudio/input/save da rota expostos como contributions. | Pipeline stages de route | Redesenhar como `RouteContributionSet` | F5+ | F7 via Content Anchor |
| Route Content Anchor set | Conjunto passivo de anchors descobertos para a rota ativa. | Pause stages + Content Anchor authoring components | Redesenhar como `ContentAnchorSet` passivo + discovery Route | F7 | IF-FW-ROAD-7G |
| Content release plan | Libera cenas/conteúdo da rota com plano explícito. | `SessionOperationalPipeline` teardown | Preservar como `ContentReleasePlan`/`Result` | F6F/F6G | IF-FW-ROAD-6F |

---

## Grupo 3 — Activity

| Capacidade | O que faz | Origem NS | Decisão | Fase | Corte |
|---|---|---|---|---|---|
| Activity asset declarativo | Define unidade jogável/contextual. | `ActivityAsset` | Preservar; reduzir fields cross-cutting | F4 | IF-FW-ROAD-4A |
| Activity content profile | Declara cenas/conteúdo que a Activity precisa carregar. | `ActivityContentProfileAsset`, `ActivityContentSceneEntry` | Preservar como perfil separado | F4 | IF-FW-ROAD-4B |
| Activity runtime state | Estado ativo/none/transition da Activity. | `SessionActivityIdentity`, `SessionActivityCycleKey` | `ActivityRuntimeState` com typed identity | F4A | IF-FW-ROAD-4A |
| Activity entry context | Contexto de entrada recebido do handoff da Route. | `SessionActivityEntryHandoff` | Preservar boundary; reduzir payload | F4 | IF-FW-ROAD-4B |
| Activity content set | Registro de conteúdo local conhecido da Activity. | `ActivityContentLoadedSet` | Preservar como `ActivityContentSet` | F4B | IF-FW-ROAD-4B |
| Activity content loading | Carrega cenas de conteúdo da Activity via adapter. | `UnityActivityContentSceneAdapter` | Preservar com plan/result/handles | F4–F6 | IF-FW-ROAD-4B, 6D |
| Activity content lifecycle result | Resultado tipado do enter/exit com referência ao ContentSet. | Implícito na pipeline | Criar como `ActivityContentLifecycleResult` | F4C | IF-FW-ROAD-4C |
| Readiness gate | Só revela gameplay quando todos objetos/actors estão prontos. | `ActivityParticipantReadinessStage`, `ISessionActivityVisualReadinessBoundary` | Preservar como state/result | F4 | IF-FW-ROAD-4D |
| Activity exit plan | Planeja release, unload e teardown ao sair da Activity. | Pipeline teardown stages | Preservar como `ActivityExitPlan` | F4 | IF-FW-ROAD-4B |
| Activity reset plan | Reseta estado da Activity sem descarregar conteúdo. | `ActivityObjectResetContracts` | Preservar; entrar depois de LocalContributionSet | F5+ | — |
| Participant binding | Conecta participação (player slots) a actors e input. | `ActivityEntryParticipantBindingStage` | Preservar boundary; redesenhar ownership | F10–F11 | IF-FW-ROAD-10B, 11F |
| Pause content/Content Anchor contribution | Activity contribui Content Anchor de pause para a rota. | `ActivityEntryPauseContentStage`, `ActivityPauseContentRuntimeState` | Redesenhar como Content Anchor consumer | F9–F10 | IF-FW-ROAD-9D, 10G |
| Activity snapshot | Captura estado da Activity para save. | `SessionActivitySnapshot` | Preservar contrato; entrar após snapshot model | F10 | IF-FW-ROAD-10D, 10E |
| Activity diagnostics/facts | Facts estruturados de lifecycle de Activity. | `SessionActivityFact` | Preservar como `FrameworkFact` | F1 | IF-FW-ROAD-1C |

---

## Grupo 4 — Local

| Capacidade | O que faz | Origem NS | Decisão | Fase | Corte |
|---|---|---|---|---|---|
| Scene-authored local binding | Objeto authored expõe contribuição local relevante para Activity/Route. | `ActivityObjectContributor` / package bindings reais | Preservar como `ActivityLocalVisibilityAdapter` / `RouteContentBinding`; sem marker paralelo | F5 | IF-FW-ROAD-5C |
| Local content identity | Identidade tipada de objeto local; substitui `targetId` string. | `ActivityObjectContributor.targetId` (frágil) | Redesenhar como `LocalContentIdentity` | F5 | IF-FW-ROAD-5A, 5B |
| Scoped contribution discovery | Descobre contributors dentro do conteúdo carregado do escopo. | `ActivityEntryObjectContributorDiscoveryStage` | Preservar discovery scoped; remover scan global | F5 | IF-FW-ROAD-5D |
| Contribution set | Conjunto tipado de contributions por escopo (Activity ou Route). | `ActivityObjectContributor` → inventory | Criar `LocalContributionSet` | F5 | IF-FW-ROAD-5E |
| Requiredness policy | Required expected ausente falha; optional expected ausente gera skip. | Distribuída na pipeline | Centralizar em validator/policy | F5 | IF-FW-ROAD-5F/F5G/F5H |
| Capability inventory | Lista endpoints/providers vivos com validade por lifecycle. | `ActivityObjectCapabilityScanner`, `ActivitySetupInventoryBuilder` | Preservar como `LocalCapabilityDescriptor` | F5 | IF-FW-ROAD-5E |
| Runtime capability reference | Referência runtime a capability com validade por lifecycle. | `ActivityObjectCapabilityScanner` — runtime refs | Preservar com validade por lifecycle | F8+ | após RuntimeHandle |
| Local reset participant | Objeto local participa de reset de Activity. | `ActivityObjectResetContracts`, reset endpoint providers | Preservar contrato | F8+ | após LocalContributionSet |
| Local snapshot participant | Objeto local captura/restaura próprio estado. | `ActivityObjectSnapshotContracts` | Preservar contrato com typed payload | F10 | IF-FW-ROAD-10D |
| Local release participant | Objeto local declara release policy. | `ActivityObjectReleaseContracts` | Preservar contrato | F8 | IF-FW-ROAD-8G |
| Exit freeze | Congela correlação Local → Activity no teardown para evitar stale. | Implícito na pipeline de exit | Preservar como parte de `ActivityExitPlan` | F8 | IF-FW-ROAD-8G |
| Local slots | Pontos authored tipados para binding físico. | Content Anchor authoring components e transform anchors | Coberto por `ContentAnchorSlot`; não criar `LocalSlot` separado | F7 | IF-FW-ROAD-7D |
| Local anchors | Pontos authored de posicionamento tipados. | Transform anchors em cena | Coberto por `ContentAnchorPoint`; não criar `LocalAnchor` separado | F7 | IF-FW-ROAD-7E |
| Stale/foreign reference check | Detecta referências de lifecycle morto ou de escopo errado. | Implícito em inventory builders | Preservar como validação de `LocalContributionSet` | F5 | IF-FW-ROAD-5G/F5H |

---

## Grupo 5 — RuntimeSpawned

| Capacidade | O que faz | Origem NS | Decisão | Fase | Corte |
|---|---|---|---|---|---|
| Runtime ownership primitives | Define scope, owner, state e identity para conteúdo criado em runtime. | Actor runtime refs e adapters ad hoc | Criar primitivas tipadas antes de qualquer spawn | F8 | IF-FW-ROAD-8B |
| Runtime content handle | Handle passivo de instância com owner, scope, estado e diagnostics de release. | Actor runtime refs (implícitas) | Criar `RuntimeContentHandle` antes do materializer | F8 | IF-FW-ROAD-8C |
| Runtime scope root | Raiz lógica por escopo/owner para instâncias dinâmicas. | Criado por adapters ad hoc (problema) | Redesenhar como `RuntimeScopeRoot` com registry | F8 | IF-FW-ROAD-8D |
| Runtime root registry | Associa owner → root sem `GameObject.Find` ou statics globais. | Implícito; criação espalhada por adapters | Criar `RuntimeRootRegistry` interno | F8 | IF-FW-ROAD-8D |
| Runtime content owner/context | Owner interno do registry/handles e contexto explícito por owner. | Ausente no NewScripts como boundary único | Criar `RuntimeContentRuntime` + `RuntimeScopeContext` | F8 | IF-FW-ROAD-8E |
| Lifecycle integration for runtime roots | Session/Route/Activity criam/removem roots/contextos explicitamente no lifecycle. | Teardown/setup espalhados | Integrar sem executar materialização física ainda | F8 | IF-FW-ROAD-8F |
| Materialization request/result | Contrato genérico de criação auditável: request antes, result depois. | `ActivityEntryPipeline` actor stages | Preservar como `RuntimeMaterializationRequest/Result` | F8 | IF-FW-ROAD-8G |
| Transition guard / scoped cancellation | Impede operação stale/foreign durante troca de Route/Activity. | Guardas espalhadas na pipeline | Criar guarda antes de qualquer adapter físico | F8 | IF-FW-ROAD-8H |
| Materialization adapter boundary | Define a fronteira para adapters físicos; prefab/cena/addressable ficam fora do core. | Adapters de actor/presentation | Preservar como adapter físico fora do core | F8 | IF-FW-ROAD-8I |
| Runtime release policy | Limpa logicamente handles ao sair do escopo; cleanup físico fica em adapter/consumer explícito. | Teardown stages espalhados | `RuntimeReleasePolicy` + request/result + `IRuntimeReleaseAdapter` | F8 | IF-FW-ROAD-8J |
| Pooled materializer | Materializa via pool rent; devolve via pool return no release. | `PoolingService` + actor adapters | Redesenhar como `PooledContentMaterializer` | F11 | IF-FW-ROAD-11H |
| Runtime spawned contribution | Instância dinâmica pode contribuir capabilities ao `ActivityContributionSet`. | Actor como contributor (implícito) | Preservar como `RuntimeSpawnedContribution` | F11 | IF-FW-ROAD-11F |
| Spawn origin / slot | Posicionamento authored de onde instâncias aparecem. | Transform anchors em cena | Integrar com `ContentAnchorSlot` / `ContentAnchorPoint` | F9 | IF-FW-ROAD-9D |
| Runtime identity | Distingue instâncias individuais sem usar nome de GameObject. | `ActorInstanceRuntimeId` | Preservar typed; generalizar para todo spawned | F8 | IF-FW-ROAD-8B/8C |
| Pool rent/return cycle | Reutiliza objetos: rent antes de usar, return no release. | `PoolingService` | Preservar como infra técnica separada | F11 | IF-FW-ROAD-11G, 11H |
| Physical destroy policy | Destroy explícito apenas quando pool return não se aplica. | `Destroy` espalhado (problema) | Deferir para adapter físico/consumer específico | F9/F11+ | Deferred |

## Grupo 6 — Content Anchor

| Capacidade | O que faz | Origem NS | Decisão | Fase | Corte |
|---|---|---|---|---|---|
| Content Anchor identity primitives | Id, scope, kind e requiredness tipados para Content Anchor. | String solta por endpoint (problema) | Criar `ContentAnchorId`, `ContentAnchorScope`, `ContentAnchorKind`, `ContentAnchorRequiredness` | F7 | IF-FW-ROAD-7B |
| Content Anchor root | Raiz física (Transform) associada a um Content Anchor com role definido. | Container de pause, overlay root | Criar `ContentAnchorRoot` com role tipado | F7 | IF-FW-ROAD-7C |
| Content Anchor slot | Ponto authored de binding físico; não usa `GameObject.Find`. | Content Anchor slots em cena | Criar `ContentAnchorSlot` typed | F7 | IF-FW-ROAD-7D |
| Content Anchor point | Ponto authored de posicionamento/reference typed. | Anchors de câmera/actor | Criar `ContentAnchorPoint` typed | F7 | IF-FW-ROAD-7E |
| Route Content Anchor authoring | Primeiro componente público que declara anchors de Route. | Content Anchor authoring components | Criar `RouteContentAnchor` | F7 | IF-FW-ROAD-7F |
| Content Anchor set por scope | Route e Activity expõem seus `ContentAnchorSet`; consumers solicitam por identity. | Implícito por scope/stage | Criar `ContentAnchorSet` passivo; discovery por scope em cortes separados | F7 | IF-FW-ROAD-7G |
| Content Anchor binding request/result | Consumer solicita Content Anchor por identity; resultado devolve handle. Consumer não controla lifetime. | Direto por adapter (problema) | Criar `ContentAnchorBindingRequest/Result` | F9 | IF-FW-ROAD-9A, 9B |
| Content Anchor content handle | Handle de binding com release; consumer libera, não destrói. | Binding sem handle formal (problema) | Criar `ContentAnchorContentHandle` | F9 | IF-FW-ROAD-9C |
| Runtime Content Anchor binding | Vincula logicamente RuntimeContentHandle a root/slot/point de Content Anchor; materialização física fica fora do core. | `RuntimeContentAnchorBinding` lógico + Content Anchor Binding Smoke | F9J CLOSED / logical binding pass; placement físico futuro | F9 | IF-FW-ROAD-9D/9E/9G |
| Duplicate detection | Detecta Content Anchor authoring/slot duplicado na mesma cena/scope. | Implícito em validators | Fechado em F7H por `ContentAnchorSet` + authoring validation | F7 | IF-FW-ROAD-7H |
| Content Anchor lifecycle policy | Consumer/runtime libera binding antes de release lógico do runtime content. | `RuntimeContentAnchorBinding` cleanup/snapshots | F9F PASS; F9J closed logical cleanup coverage | F9 | IF-FW-ROAD-9H |
| Overlay root separado de content root | Roles distintos para UI de pausa vs. conteúdo. | `ContentAnchorRoot.Role` implícito | Deferred para F9/F10; depende de runtime placement e consumers de UI/Pause | F9/F10 | Deferred |

---

## Grupo 7 — Input

| Capacidade | O que faz | Origem NS | Decisão | Fase | Corte |
|---|---|---|---|---|---|
| Input mode contract | Contrato de modo de input por escopo (sem string action map). | `InputModesCompositionDescriptor`, string action maps | Redesenhar como `InputModeContract` typed | F10 | IF-FW-ROAD-10A, 10B |
| Activity input consumer | Activity registra input mode ao entrar; libera ao sair. | `ActivityEntryParticipantBindingStage` | Redesenhar como consumer de input | F10 | IF-FW-ROAD-10B |
| Input mode owner | Quem possui o modo atual; sem singleton global. | Implícito via DependencyManager | Criar `InputModeOwner` explícito | F10 | IF-FW-ROAD-10A |
| Player slot / command binding | Conecta player slot a actor via input commands. | `SessionParticipantBinding`, `PlayerSlotReservation` | Preservar boundary; redesenhar ownership | F10–F11 | IF-FW-ROAD-10B, 11F |

---

## Grupo 8 — Save / Snapshot

| Capacidade | O que faz | Origem NS | Decisão | Fase | Corte |
|---|---|---|---|---|---|
| Snapshot envelope | Contrato typed de captura: owner, schema, version, payload. | `ActivityObjectSnapshot`, save payload implícito | Redesenhar como `SnapshotEnvelope` typed | F10 | IF-FW-ROAD-10C, 10D |
| Snapshot participant contract | Objeto local declara Capture/Restore sem backend. | `ActivityObjectSnapshotContracts` | Preservar como `ISnapshotParticipant` | F10 | IF-FW-ROAD-10D |
| Snapshot set | Coleta participants de um scope para captura/restore coordenada. | `ActivityObjectSnapshot` por scope | Criar `SnapshotSet` | F10 | IF-FW-ROAD-10E |
| Save backend port | Interface para persistência de snapshots; implementação fora do core. | `SaveCompositionDescriptor`, save adapters | Preservar como port/adapter | F10 | IF-FW-ROAD-10C |
| Schema versioning | Payload com versão de schema para migração segura. | Implícito/frágil no NewScripts | Criar como parte do `SnapshotEnvelope` | F10 | IF-FW-ROAD-10C |

---

## Grupo 9 — Pause

| Capacidade | O que faz | Origem NS | Decisão | Fase | Corte |
|---|---|---|---|---|---|
| Pause content anchor consumer | Pause usa Content Anchor de Route/Activity; não cria sua própria. | `ActivityEntryPauseContentStage`, pause Content Anchor stages | Redesenhar como `PauseContentAnchorConsumer` | F10 | IF-FW-ROAD-10F, 10G |
| Pause state contract | Estado de pause tipado; Route/Activity não sabem de Pause como subsistema. | `SessionActivityPipeline` pause handling | Criar `PauseStateContract` | F10 | IF-FW-ROAD-10F |
| Pause content materialization | Materializa UI/overlay de pause em Content Anchor slot. | Pause stages + Content Anchor binding | Usa `RuntimeContentAnchorBinding` | F9–F10 | IF-FW-ROAD-9D, 10G |
| Pause/unpause lifecycle | Pause é consumer de Activity state; não possui lifecycle. | Implícito na pipeline | Preservar boundary: pause recebe evento, não controla Activity | F10 | IF-FW-ROAD-10F |

---

## Grupo 10 — Camera

| Capacidade | O que faz | Origem NS | Decisão | Fase | Corte |
|---|---|---|---|---|---|
| Camera consumer | Resolve câmera virtual ativa via ContentAnchorSet; sem authority global estático. | `OperationalCameraRuntimeComposition`, `FrameworkCameraAuthority` (problema) | Redesenhar como `CameraConsumer` | F11 | IF-FW-ROAD-11A, 11B |
| Camera anchor binding | Câmera física posicionada via `ContentAnchor`; não por nome de GameObject. | Camera anchor stages (string-based, problema) | Redesenhar com `ContentAnchor` typed | F11 | IF-FW-ROAD-11B |
| Camera scope (Session/Route/Activity) | Câmera muda conforme scope ativo; Route câmera ≠ Activity câmera. | Implícito em stages | Preservar modelo de escopo | F11 | IF-FW-ROAD-11B |
| Camera binding result/handle | Handle de câmera ativa com release ao sair do scope. | Sem handle formal (problema) | Criar `CameraBindingResult` com handle | F11 | IF-FW-ROAD-11B |
| Virtual camera binding | Liga virtual camera Cinemachine a Content Anchor/anchor typed. | `FrameworkCinemachineCameraBinding` (package) | Redesenhar como consumer de Content Anchor; não no core | F11 | IF-FW-ROAD-11B |

---

## Grupo 11 — Audio

| Capacidade | O que faz | Origem NS | Decisão | Fase | Corte |
|---|---|---|---|---|---|
| BGM lifecycle consumer | BGM muda ao entrar Route; faz fade out ao sair. Sem ownership de Route. | `AudioCompositionDescriptor`, audio stages | Preservar como consumer de lifecycle | F11 | IF-FW-ROAD-11C, 11D |
| SFX dispatch | Dispara SFX por evento de Activity/Local; sem acesso direto ao lifecycle. | Audio adapters | Preservar como port/adapter | F11 | IF-FW-ROAD-11D |
| Audio listener | Listener de áudio persistente no escopo Session. | Session audio composition | Preservar em `SessionContentSet` | F2 | IF-FW-ROAD-2C |

---

## Grupo 12 — Actor

| Capacidade | O que faz | Origem NS | Decisão | Fase | Corte |
|---|---|---|---|---|---|
| Actor materialization request/result | Actor é RuntimeSpawned com request/result/handle; não entra por pipeline ActivityEntry. | `ActivityEntryParticipantBindingStage` (acoplado) | Redesenhar como `ActorMaterializationRequest/Result` | F11 | IF-FW-ROAD-11E, 11F |
| Actor contribution | Actor materializado contribui capabilities ao `ActivityContributionSet`. | Actor como contributor implícito | Criar `ActorContribution` | F11 | IF-FW-ROAD-11F |
| Actor runtime identity | Distingue instâncias de actor sem usar nome de GameObject. | `ActorInstanceRuntimeId` | Preservar typed; especializar `RuntimeContentIdentity` | F11 | IF-FW-ROAD-11F |
| Actor reset/release | Actor participa de reset/release da Activity. | Actor adapters no teardown | Preservar via `LocalResetParticipant` / `LocalReleaseParticipant` | F11 | IF-FW-ROAD-11F |
| Player actor binding | Vincula player slot a actor específico para input. | `SessionParticipantBinding` | Preservar boundary; redesenhar sem pipeline monolítica | F11 | IF-FW-ROAD-11F |

---

## Grupo 13 — Pooling

| Capacidade | O que faz | Origem NS | Decisão | Fase | Corte |
|---|---|---|---|---|---|
| Pool rent | Retira instância do pool para uso. | `PoolingService.Rent` | Preservar como infra técnica em package separado | F11 | IF-FW-ROAD-11G |
| Pool return | Devolve instância ao pool no release. | `PoolingService.Return` | Preservar; integrar com `RuntimeReleasePolicy` | F11 | IF-FW-ROAD-11H |
| Pool ownership | Pool é técnico; não é dono de lifecycle de feature. | Acoplado a projectile (problema) | Desacoplar de gameplay; package independente | F11 | IF-FW-ROAD-11G |

---

## Grupo 14 — Projectile / Damage / Attributes

| Capacidade | O que faz | Origem NS | Decisão | Fase | Corte |
|---|---|---|---|---|---|
| Projectile as RuntimeSpawned | Projectile é instância dinâmica com pool return ao colidir/expirar. | `ProjectileController` + pool | Redesenhar como `RuntimeSpawned` com pool return | F12 | IF-FW-ROAD-12A |
| Impact/collision handling | Detecta colisão sem controlar lifecycle de projectile. | `ImpactReceiver`, impact handlers | Preservar boundary: impact não controla projectile | F12 | IF-FW-ROAD-12B |
| Damage as actor capability | Damage aplica mutação sobre Attribute por contrato. | `DamageComponent`, attribute modifiers | Preservar como capability; sem acesso a Actor internals | F12 | IF-FW-ROAD-12C |
| Attributes as snapshot-capable | Attributes participam de `SnapshotSet` para save. | `AttributeComponent` + save | Preservar com `ISnapshotParticipant` | F12 | IF-FW-ROAD-12D |

---

## Grupo 15 — Diagnostics / QA

| Capacidade | O que faz | Origem NS | Decisão | Fase | Corte |
|---|---|---|---|---|---|
| FrameworkFact | Fato estruturado para QA/smoke; distinto de log humano. | `SessionActivityFact`, facts espalhados | Criar `FrameworkFact` mínimo | F1 | IF-FW-ROAD-1C |
| Authoring validator | Valida setup em editor; idempotente e editor-only. | `FrameworkAuthoringValidator` (package) | Expandir por fase | Todas | Por fase |
| QA Canvas / smoke buttons | Smoke manual via IMGUI runtime; não é API de produto. | `FrameworkQaCanvas` | Preservar como dev tooling; não no runtime assembly de produto | F0B | IF-FW-ROAD-0B5 |
| Boot fail-fast | Config obrigatória ausente = erro fatal imediato com diagnóstico. | `RuntimeConfigRegistry.ResolveConfigSetOrFail` | Preservar | F1 | IF-FW-ROAD-1D |
| Required/optional policy | Required ausente = fact + erro; optional ausente = skip | Distribuída em stages | Centralizar em policy por scope | F5 | IF-FW-ROAD-5F |
| Smoke doc (BASELINE_SMOKE) | Documento vivo de smoke manual: boot, route, activity, callbacks. | Logs de smoke no package | Criar/manter `BASELINE_SMOKE.md` | F0B | IF-FW-ROAD-0B7 |

---

## Índice por fase-alvo

> Índice autoritativo após `IF-FW-F9PLUS-REALIGNMENT`. A tabela antiga F10/F11/F12 foi renumerada.

| Fase | Capacidades cobertas |
|---|---|
| **F0B** | Boot global, QA Canvas boundary, Smoke doc |
| **F1** | Config de runtime, Runtime policy, diagnostics/facts, FrameworkFact, Typed identity policy, Content identity, Boot fail-fast |
| **F2** | Session runtime state, Session content set, Settings source policy, Startup route signal |
| **F3** | Route asset, Route runtime state, Route exit result, Route primary scene, Route → Activity handoff, Route startup activity policy |
| **F4** | Activity asset, Activity runtime state, Activity entry context, Activity content set, Activity content lifecycle result, Readiness gate |
| **F5** | Local content identity, explicit local ids, scoped loaded discovery, LocalContributionSet, Requiredness metadata, LocalContributionValidator, Local Contribution Smoke |
| **F6** | RouteSceneCompositionPlan/Result, additive scene primitive, Route content profile execution, ContentReleasePlan/Result, owned additive scene release execution |
| **F7** | Content Anchor identity primitives, Root/Slot/Point declarations, Route Content Anchor authoring, ContentAnchorSet, Route discovery, diagnostics smoke, validation and duplicate detection |
| **F8** | Runtime ownership primitives, RuntimeContentHandle, RuntimeScopeRoot/internal registry, RuntimeContentRuntime/RuntimeScopeContext, lifecycle integration, materialization request/result, transition guard/scoped cancellation, adapter boundary, runtime release policy, runtime content closure smoke |
| **F9** | CLOSED logical Content Anchor binding: request/result/content handle [F9A], RuntimeContentAnchorBinding lógico [F9B], Route binding smoke [F9C PASS], lifecycle cleanup/snapshots [F9D PASS], host-owned binding runtime [F9E PASS], automatic logical cleanup [F9F PASS], Activity Content Anchor discovery/positive/binding [F9G/F9H/F9I PASS], placement adapters ainda futuros |
| **F10** | Transition request/policy/result, loading progress, input lock policy, ActivityContentProfile execution, Activity-owned content, Activity reset baseline |
| **F11** | Participation boundary, PlayerSlot/ParticipantId, Live Capability Inventory, RuntimeCapabilityReference, Local reset/release/snapshot participants, exit freeze, Consumer Descriptor Pattern |
| **F12** | Input mode, input owner, Snapshot envelope/participants/set, Save backend port, SaveSlot/progression/migration, Pause content anchor consumer, Pause state/lifecycle |
| **F13** | Camera, Audio, Actor, Pooling, Transition presentation adapters |
| **F14** | Projectile, Impact, Damage, Attributes, Cinematics/Cutscene consumers |
| **F15/FX** | Settings source, assembly/build/stripping, versioning/migration, pre-build validation, scoped messaging, editor simulation/visualizer, Asset Provider/Addressables/DLC, domain reload, telemetry |


### F2 closure — Session scope

| Capability | Cut | Status | Evidence | Notes |
|---|---|---|---|---|
| Session runtime state | F2B | CLOSED / COMPILE-SMOKE PASS | `Runtime/SessionLifecycle/SessionRuntimeState.cs` | Explicit Session state boundary created. `FrameworkRuntimeState` remains compatibility facade. |
| Session content set | F2C | CLOSED / COMPILE-SMOKE PASS | `Runtime/SessionLifecycle/SessionContentSet.cs` | Minimal Session content set created; initial set can be empty. |
| Session content ownership | F2C | CLOSED / COMPILE-SMOKE PASS | `Runtime/SessionLifecycle/SessionContentOwnership.cs` | `Registered`, `Owned` and `DiagnosticOnly` semantics created. |
| Session smoke | F2D | CLOSED / DOCUMENTATION ONLY | `Documentation~/COMPLETENESS_TRACKER.md` | F2B/F2C smokes close the technical Session phase. |

F2 intentionally does not implement persistent scenes, Route baseline, Content Anchor or RuntimeMaterialization.


## F4 closure note

F4 fecha o baseline mínimo de Activity. `ActivityContentSet` permanece snapshot local/diagnóstico de adapters de visibilidade. F5 deve iniciar por identidade local própria, sem reutilizar nome/path de cena ou GameObject como chave funcional canônica.


### F9G — Activity Content Anchor authoring/discovery

Status: `PASS DIAGNOSTIC SMOKE`

Adds Activity-scoped Content Anchor authoring, discovery diagnostics and QA smoke. No physical placement, prefab/scene adapter, Addressables, pooling or gameplay consumers.

### F9H — Activity Content Anchor positive-path smoke

Status: `PASS`

Adds QA positive-path coverage for one valid Activity Content Anchor accepted by discovery. Does not introduce placement, materialization adapters or gameplay consumers.

### F9I — Activity Content Anchor binding smoke

Status: `PASS`

Adds and validates QA Activity-scoped binding coverage for one valid Activity Content Anchor, including idempotent binding and Activity exit cleanup. Does not introduce placement, materialization adapters or gameplay consumers.

### F9J — Content Anchor logical binding closure

Status: `CLOSED`

Closes F9 as the logical Content Anchor binding layer. Physical placement, prefab/scene/Addressables/pooling adapters, physical release and gameplay consumers remain future work.
