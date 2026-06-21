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
- Consumers não devem entrar antes de owner, identity, content set, release e surface/runtime quando aplicável.
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
| `ActivityContentProfile` | Não tratar como F4 obrigatório; F4 deve estabilizar `ActivityContentSet` e readiness mínimo. Profile avançado entra depois de scene composition/release. |
| `SessionCompositionContext` | Não criar composition genérico cedo demais. Primeiro Session scope/content sem service locator. |
| `PersistentScenesPolicy` | Não puxar scene ownership para F2. Reavaliar com Route scene composition/release ou futura Session persistent content. |
| `CapabilityInventory` | Em F5 apenas descriptor/contribution simples. Runtime refs/inventory vivo só depois de RuntimeHandle/lifetime. |
| `Surface` | Dividida em declaração (`F7`) e binding/runtime placement (`F9`). |
| `RuntimeSpawned` | Entra só depois de identity/content/release; consumers não podem criar roots ad hoc. |
| Campos `—` | Tratados como `Deferred` ou `Requer detalhamento`; não podem ficar sem status no backlog. |

---
## 4. Matriz consolidada por grupo

### Session
| Capacidade | Decisão | Fase original | Fase revisada | Corte | Status package | Status roadmap | Prioridade | Bloqueadores | Risco | Observação |
|---|---|---|---|---|---|---|---|---|---|---|
| Boot global antes da cena | Preservar | F0B | F0B | IF-FW-ROAD-0B7 | Presente / Parcial | Coberto | Core | Baseline ADRs + API/identity policy | Baixo / controlado | Sem ajuste. |
| Config de runtime obrigatória | Preservar fail-fast | F1 | F1 | IF-FW-ROAD-1D | Parcial / Ausente | Coberto | Core | RuntimeContentHandle + ContributionSet + release policy | Baixo / controlado | Sem ajuste. |
| Registry imutável de config | Preservar como contexto explícito | F1 | F1 | IF-FW-ROAD-1D | Parcial / Ausente | Coberto | Core | Baseline ADRs + API/identity policy | Baixo / controlado | Sem ajuste. |
| Pipeline de composição em fases | Redesenhar sem DependencyManager público | F2 | Futura F2/F3 | IF-FW-ROAD-2C | Parcial / Ausente | Coberto com ajuste / Deferred | Core | Baseline ADRs + API/identity policy | Baixo / controlado | Não obrigatório como corte imediato. Preservar o padrão, mas evitar composition genérico cedo demais. |
| Session runtime state | Preservar; tipar IDs | F2 | F2 | IF-FW-ROAD-2B | Presente / F2B | Coberto | Core | RuntimeContentHandle + ContributionSet + release policy | Baixo / controlado | Sem ajuste. |
| Session composition context | Redesenhar; composição interna | F2 | Deferred | IF-FW-ROAD-2C | Parcial / Ausente | Coberto com ajuste / Deferred | Core | Baseline ADRs + API/identity policy | Risco de entrar cedo demais | Conceito válido, mas perigoso cedo. Primeiro formalizar Session scope/content sem service locator. |
| Persistent scenes policy | Preservar conceito | F2 | F6 ou futura Session Persistent Content | IF-FW-ROAD-2C | Parcial / Ausente | Coberto, mas com correção | Core | Baseline ADRs + API/identity policy | Risco de entrar cedo demais | Não puxar scene ownership para F2; depende de scene composition/release. |
| Session content set | Preservar como `SessionContentSet` | F2 | F2 | IF-FW-ROAD-2C | Presente / F2C mínimo | Coberto, mas com correção | Core | Baseline ADRs + API/identity policy | Baixo / controlado | Mantém F2, mas como set mínimo, possivelmente vazio/diagnóstico. |
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
| Route runtime state | Preservar como `RouteRuntimeState` | F3 | F3 | IF-FW-ROAD-3A | Parcial / Ambíguo | Coberto | Core | RuntimeContentHandle + ContributionSet + release policy | Baixo / controlado | Sem ajuste. |
| Route exit plan | Preservar como `RouteExitResult` | F3 | F3 | IF-FW-ROAD-3B | Parcial / Ambíguo | Coberto | Core | Route baseline + Content identity | Baixo / controlado | Sem ajuste. |
| Scene composition plan/result | Preservar plan/result explícitos | F6 | F6 | IF-FW-ROAD-6A, 6B, 6C | Parcial / Ausente | Coberto | Core | Route baseline + Content identity | Baixo / controlado | Sem ajuste. |
| Primary scene loading | Preservar; já existe no package | F3 | F3 | IF-FW-ROAD-3C | Presente / Parcial | Coberto | Core | Route baseline + Content identity | Baixo / controlado | Sem ajuste. |
| Additive scene loading | Preservar com plan/result | F6 | F6 | IF-FW-ROAD-6D | Parcial / Ausente | Coberto | Core | Route baseline + Content identity | Baixo / controlado | Sem ajuste. |
| Active scene policy | Preservar como parte do plan | F6 | F6 | IF-FW-ROAD-6A | Parcial / Ausente | Coberto | Core | Route baseline + Content identity | Baixo / controlado | Sem ajuste. |
| Route content profile execution | Preservar execução; separar de planning | F6 | F6 | IF-FW-ROAD-6E | Parcial / Ambíguo | Coberto | Core | Route baseline + Content identity | Risco de entrar cedo demais | Sem ajuste. |
| Route → Activity handoff | Preservar boundary; reduzir payload | F3 | F3 | IF-FW-ROAD-3C | Parcial / Ausente | Coberto | Core | Route baseline + Content identity | Baixo / controlado | Sem ajuste. |
| Route startup activity policy | Preservar como policy simples | F3 | F3 | IF-FW-ROAD-3C | Parcial / Ausente | Coberto | Core | Route baseline + Content identity | Baixo / controlado | Sem ajuste. |
| Route contribution set | Redesenhar como `RouteContributionSet` | F5+ | F5/F7/F10+ | F7 via Surface | Parcial / Ausente | Coberto com ajuste / Deferred | Core | Route baseline + Content identity | Baixo / controlado | Contribution genérico em F5; surfaces em F7; consumers reais depois. |
| Route surface set | Redesenhar como `RouteSurfaceSet` | F7 | F7 | IF-FW-ROAD-7G | Parcial / Ausente | Coberto, mas com correção | Core | LocalContributionSet para discovery; Runtime binding só na F9 | Baixo / controlado | Correto. |
| Content release plan | Preservar como `ContentReleasePlan` | F6 | F6 | IF-FW-ROAD-6F | Parcial / Ausente | Coberto | Core | Route baseline + Content identity | Baixo / controlado | Sem ajuste. |

### Activity
| Capacidade | Decisão | Fase original | Fase revisada | Corte | Status package | Status roadmap | Prioridade | Bloqueadores | Risco | Observação |
|---|---|---|---|---|---|---|---|---|---|---|
| Activity asset declarativo | Preservar; reduzir fields cross-cutting | F4 | F4 | IF-FW-ROAD-4A | Presente / Parcial | Coberto | Core | Route handoff + ActivityContentSet | Baixo / controlado | Sem ajuste. |
| Activity content profile | Preservar como perfil separado | F4 | Deferred após F4/F6 | IF-FW-ROAD-4B | Parcial / Ausente | Coberto com ajuste / Deferred | Core | Route handoff + ActivityContentSet | Risco de entrar cedo demais | F4 deve criar ActivityContentSet/readiness mínimo; profile avançado vem depois de scene composition/release. |
| Activity runtime state | Redesenhar com typed IDs | F4 | F4 | IF-FW-ROAD-4A | Parcial / Ausente | Coberto | Core | RuntimeContentHandle + ContributionSet + release policy | Baixo / controlado | Sem ajuste. |
| Activity entry context | Preservar boundary; reduzir payload | F4 | F4 | IF-FW-ROAD-4B | Parcial / Ausente | Coberto | Core | Route handoff + ActivityContentSet | Baixo / controlado | Sem ajuste. |
| Activity content set | Preservar como `ActivityContentSet` | F4 | F4 | IF-FW-ROAD-4B | Parcial / Ausente | Coberto | Core | Route handoff + ActivityContentSet | Baixo / controlado | Sem ajuste. |
| Activity content loading | Preservar com plan/result/handles | F4–F6 | F6+ | IF-FW-ROAD-4B, 6D | Parcial / Ausente | Coberto com ajuste / Deferred | Core | Route handoff + ActivityContentSet | Baixo / controlado | Carregamento real de cenas da Activity depende de plan/result/release. |
| Activity content lifecycle result | Criar como `ActivityContentLifecycleResult` | F4 | F4 | IF-FW-ROAD-4C | Parcial / Ausente | Coberto | Core | Route handoff + ActivityContentSet | Baixo / controlado | Sem ajuste. |
| Readiness gate | Preservar como state/result | F4 | F4 | IF-FW-ROAD-4D | Parcial / Ausente | Coberto | Core | Route handoff + ActivityContentSet | Baixo / controlado | Sem ajuste. |
| Activity exit plan | Preservar como `ActivityExitPlan` | F4 | F6/F8 | IF-FW-ROAD-4B | Parcial / Ausente | Coberto, mas com correção | Core | Route handoff + ActivityContentSet | Baixo / controlado | Exit simples em F4; plano de release completo após release/runtime policy. |
| Activity reset plan | Preservar; entrar depois de LocalContributionSet | F5+ | Futura após F5/F10 | — | Parcial / Ausente | Coberto com ajuste / Deferred | Core | Route handoff + ActivityContentSet | Risco de ficar sem corte rastreável | Depende de LocalContributionSet e snapshot/reset participants. |
| Participant binding | Preservar boundary; redesenhar ownership | F10–F11 | F10–F11 | IF-FW-ROAD-10B, 11F | Parcial / Ausente | Coberto | Core | Route handoff + ActivityContentSet | Baixo / controlado | Sem ajuste. |
| Pause content/surface contribution | Redesenhar como Surface consumer | F9–F10 | F9–F10 | IF-FW-ROAD-9D, 10G | Parcial / Ausente | Coberto | Core | LocalContributionSet para discovery; Runtime binding só na F9 | Baixo / controlado | Sem ajuste. |
| Activity snapshot | Preservar contrato; entrar após snapshot model | F10 | F10 | IF-FW-ROAD-10D, 10E | Parcial / Ausente | Coberto | Core | Route handoff + ActivityContentSet | Baixo / controlado | Sem ajuste. |
| Activity diagnostics/facts | Preservar como `FrameworkFact` | F1 | F1 | IF-FW-ROAD-1C | Parcial / Ausente | Coberto | Core | Route handoff + ActivityContentSet | Baixo / controlado | Sem ajuste. |

### Local
| Capacidade | Decisão | Fase original | Fase revisada | Corte | Status package | Status roadmap | Prioridade | Bloqueadores | Risco | Observação |
|---|---|---|---|---|---|---|---|---|---|---|
| Contributor marker | Preservar como `LocalContributionMarker` | F5 | F5 | IF-FW-ROAD-5C | Parcial / Ausente | Coberto | Core | ActivityContentSet/RouteContentSet + typed identity | Risco de identity/discovery frágil | Sem ajuste. |
| Local content identity | Redesenhar como `LocalContentIdentity` | F5 | F5 | IF-FW-ROAD-5A, 5B | Parcial / Ausente | Coberto | Core | ActivityContentSet/RouteContentSet + typed identity | Risco de identity/discovery frágil | Sem ajuste. |
| Scoped contribution discovery | Preservar discovery scoped; remover scan global | F5 | F5 | IF-FW-ROAD-5D | Parcial / Ausente | Coberto | Core | ActivityContentSet/RouteContentSet + typed identity | Risco de identity/discovery frágil | Sem ajuste. |
| Contribution set | Criar `LocalContributionSet` | F5 | F5 | IF-FW-ROAD-5E | Parcial / Ausente | Coberto | Core | ActivityContentSet/RouteContentSet + typed identity | Risco de identity/discovery frágil | Sem ajuste. |
| Requiredness policy | Centralizar em policy | F5 | F5 | IF-FW-ROAD-5F | Parcial / Ausente | Coberto | Core | ActivityContentSet/RouteContentSet + typed identity | Risco de identity/discovery frágil | Sem ajuste. |
| Capability inventory | Preservar como `LocalCapabilityDescriptor` | F5 | F5 parcial / Runtime refs depois | IF-FW-ROAD-5E | Parcial / Ausente | Coberto, mas com correção | Core | ActivityContentSet/RouteContentSet + typed identity | Risco de entrar cedo demais | Em F5 apenas descriptor simples; inventory vivo/runtime reference fica depois de RuntimeHandle. |
| Runtime capability reference | Preservar com validade por lifecycle | F8+ | F8+ | após RuntimeHandle | Parcial / Ausente | Coberto com ajuste / Deferred | Core | RuntimeContentHandle + ContributionSet + release policy | Risco de identity/discovery frágil | Correto: depende de RuntimeContentHandle/lifetime validity. |
| Local reset participant | Preservar contrato | F8+ | F10+ | após LocalContributionSet | Parcial / Ausente | Coberto com ajuste / Deferred | Core | ActivityContentSet/RouteContentSet + typed identity | Risco de identity/discovery frágil | Depende de LocalContributionSet e capability participant model. |
| Local snapshot participant | Preservar contrato com typed payload | F10 | F10 | IF-FW-ROAD-10D | Parcial / Ausente | Coberto | Core | ActivityContentSet/RouteContentSet + typed identity | Risco de identity/discovery frágil | Sem ajuste. |
| Local release participant | Preservar contrato | F8 | F8/F10 | IF-FW-ROAD-8G | Parcial / Ausente | Coberto, mas com correção | Core | ActivityContentSet/RouteContentSet + typed identity | Risco de identity/discovery frágil | Release policy vem F8; participant local específico depois. |
| Exit freeze | Preservar como parte de `ActivityExitPlan` | F8 | F8/F10 | IF-FW-ROAD-8G | Parcial / Ausente | Coberto, mas com correção | Core | ActivityContentSet/RouteContentSet + typed identity | Risco de identity/discovery frágil | Só faz sentido quando há runtime references/release participants. |
| Local slots | Preservar como `LocalSlot` (futuramente `SurfaceSlot`) | F7 | F7 | IF-FW-ROAD-7D | Parcial / Ausente | Coberto | Core | ActivityContentSet/RouteContentSet + typed identity | Risco de identity/discovery frágil | Sem ajuste. |
| Local anchors | Preservar como `LocalAnchor` (futuramente `SurfaceAnchor`) | F7 | F7 | IF-FW-ROAD-7E | Parcial / Ausente | Coberto | Core | ActivityContentSet/RouteContentSet + typed identity | Risco de identity/discovery frágil | Sem ajuste. |
| Stale/foreign reference check | Preservar como validação de `LocalContributionSet` | F5 | F5 | IF-FW-ROAD-5F | Parcial / Ausente | Coberto | Core | ActivityContentSet/RouteContentSet + typed identity | Risco de identity/discovery frágil | Sem ajuste. |

### RuntimeSpawned
| Capacidade | Decisão | Fase original | Fase revisada | Corte | Status package | Status roadmap | Prioridade | Bloqueadores | Risco | Observação |
|---|---|---|---|---|---|---|---|---|---|---|
| Runtime scope root | Redesenhar como `RuntimeScopeRoot` com registry | F8 | F8 | IF-FW-ROAD-8B, 8C | Ausente | Coberto | Foundation avançada | Content identity + release semantics + RuntimeRootRegistry | Risco de ownership/lifetime incompleto | Sem ajuste. |
| Runtime root registry | Criar `RuntimeRootRegistry` | F8 | F8 | IF-FW-ROAD-8C | Ausente | Coberto | Foundation avançada | Content identity + release semantics + RuntimeRootRegistry | Risco de ownership/lifetime incompleto | Sem ajuste. |
| Materialization request/result | Preservar como `RuntimeMaterializationRequest/Result` | F8 | F8 | IF-FW-ROAD-8E | Ausente | Coberto | Foundation avançada | Content identity + release semantics + RuntimeRootRegistry | Risco de ownership/lifetime incompleto | Sem ajuste. |
| Runtime content handle | Criar `RuntimeContentHandle` | F8 | F8 | IF-FW-ROAD-8D | Ausente | Coberto | Foundation avançada | Content identity + release semantics + RuntimeRootRegistry | Risco de ownership/lifetime incompleto | Sem ajuste. |
| Prefab materializer | Preservar como `PrefabContentMaterializer` | F8 | F8 | IF-FW-ROAD-8F | Ausente | Coberto | Foundation avançada | Content identity + release semantics + RuntimeRootRegistry | Risco de ownership/lifetime incompleto | Sem ajuste. |
| Runtime release policy | Criar `RuntimeReleasePolicy` por escopo | F8 | F8 | IF-FW-ROAD-8G | Ausente | Coberto | Foundation avançada | Content identity + release semantics + RuntimeRootRegistry | Risco de ownership/lifetime incompleto | Sem ajuste. |
| Pooled materializer | Redesenhar como `PooledContentMaterializer` | F11 | F11 | IF-FW-ROAD-11H | Ausente | Coberto | Foundation avançada | Content identity + release semantics + RuntimeRootRegistry | Risco de ownership/lifetime incompleto | Sem ajuste. |
| Runtime spawned contribution | Preservar como `RuntimeSpawnedContribution` | F11 | F11 | IF-FW-ROAD-11F | Ausente | Coberto | Foundation avançada | Content identity + release semantics + RuntimeRootRegistry | Risco de ownership/lifetime incompleto | Sem ajuste. |
| Spawn origin / slot | Integrar com `LocalAnchor` / `SurfaceSlot` | F9 | F9 | IF-FW-ROAD-9D | Ausente | Coberto | Foundation avançada | Content identity + release semantics + RuntimeRootRegistry | Risco de ownership/lifetime incompleto | Sem ajuste. |
| Runtime identity | Preservar typed; generalizar para todo spawned | F8 | F8 | IF-FW-ROAD-8D | Ausente | Coberto | Foundation avançada | Content identity + release semantics + RuntimeRootRegistry | Risco de ownership/lifetime incompleto | Sem ajuste. |
| Pool rent/return cycle | Preservar como infra técnica separada | F11 | F11 | IF-FW-ROAD-11G, 11H | Ausente | Coberto | Foundation avançada | Content identity + release semantics + RuntimeRootRegistry | Risco de ownership/lifetime incompleto | Sem ajuste. |
| Destroy policy | Centralizar em `RuntimeReleasePolicy` | F8 | F8 | IF-FW-ROAD-8G | Ausente | Coberto | Foundation avançada | Content identity + release semantics + RuntimeRootRegistry | Risco de ownership/lifetime incompleto | Sem ajuste. |

### Surface
| Capacidade | Decisão | Fase original | Fase revisada | Corte | Status package | Status roadmap | Prioridade | Bloqueadores | Risco | Observação |
|---|---|---|---|---|---|---|---|---|---|---|
| Surface identity | Criar `SurfaceIdentity` tipado | F7 | F7 | IF-FW-ROAD-7B | Ausente | Coberto | Foundation avançada | LocalContributionSet para discovery; Runtime binding só na F9 | Risco de ownership/lifetime incompleto | Sem ajuste. |
| Surface root | Criar `SurfaceRoot` com role tipado | F7 | F7 | IF-FW-ROAD-7C | Ausente | Coberto | Foundation avançada | LocalContributionSet para discovery; Runtime binding só na F9 | Risco de ownership/lifetime incompleto | Sem ajuste. |
| Surface slot | Criar `SurfaceSlot` typed | F7 | F7 | IF-FW-ROAD-7D | Ausente | Coberto | Foundation avançada | LocalContributionSet para discovery; Runtime binding só na F9 | Risco de ownership/lifetime incompleto | Sem ajuste. |
| Surface anchor | Criar `SurfaceAnchor` typed | F7 | F7 | IF-FW-ROAD-7E | Ausente | Coberto | Foundation avançada | LocalContributionSet para discovery; Runtime binding só na F9 | Risco de ownership/lifetime incompleto | Sem ajuste. |
| Surface endpoint | Criar `SurfaceEndpoint` | F7 | F7 | IF-FW-ROAD-7F | Ausente | Coberto | Foundation avançada | LocalContributionSet para discovery; Runtime binding só na F9 | Risco de ownership/lifetime incompleto | Sem ajuste. |
| Surface set por scope | Criar `RouteSurfaceSet`, `ActivitySurfaceSet` | F7 | F7 | IF-FW-ROAD-7G | Ausente | Coberto | Foundation avançada | LocalContributionSet para discovery; Runtime binding só na F9 | Risco de ownership/lifetime incompleto | Sem ajuste. |
| Surface binding request/result | Criar `SurfaceBindingRequest/Result` | F9 | F9 | IF-FW-ROAD-9A, 9B | Ausente | Coberto | Foundation avançada | LocalContributionSet para discovery; Runtime binding só na F9 | Risco de ownership/lifetime incompleto | Sem ajuste. |
| Surface content handle | Criar `SurfaceContentHandle` | F9 | F9 | IF-FW-ROAD-9C | Ausente | Coberto | Foundation avançada | LocalContributionSet para discovery; Runtime binding só na F9 | Risco de ownership/lifetime incompleto | Sem ajuste. |
| Runtime surface binding | Criar `RuntimeSurfaceBinding` | F9 | F9 | IF-FW-ROAD-9D | Ausente | Coberto | Foundation avançada | LocalContributionSet para discovery; Runtime binding só na F9 | Risco de ownership/lifetime incompleto | Sem ajuste. |
| Duplicate detection | Preservar em Surface validators | F7 | F7 | IF-FW-ROAD-7H | Ausente | Coberto | Foundation avançada | LocalContributionSet para discovery; Runtime binding só na F9 | Risco de ownership/lifetime incompleto | Sem ajuste. |
| Surface lifecycle policy | Preservar policy explícita de release | F9 | F9 | IF-FW-ROAD-9E | Ausente | Coberto | Foundation avançada | LocalContributionSet para discovery; Runtime binding só na F9 | Risco de ownership/lifetime incompleto | Sem ajuste. |
| Overlay root separado de content root | Preservar modelagem de roles | F7 | F7 | IF-FW-ROAD-7C | Ausente | Coberto | Foundation avançada | LocalContributionSet para discovery; Runtime binding só na F9 | Risco de ownership/lifetime incompleto | Sem ajuste. |

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
| Pause surface consumer | Redesenhar como `PauseSurfaceConsumer` | F10 | F10 | IF-FW-ROAD-10F, 10G | Ausente | Coberto | Consumer intermediário | SurfaceSet + Runtime/Binding conforme escopo | Baixo / controlado | Sem ajuste. |
| Pause state contract | Criar `PauseStateContract` | F10 | F10 | IF-FW-ROAD-10F | Ausente | Coberto | Consumer intermediário | SurfaceSet + Runtime/Binding conforme escopo | Baixo / controlado | Sem ajuste. |
| Pause content materialization | Usa `RuntimeSurfaceBinding` | F9–F10 | F9–F10 | IF-FW-ROAD-9D, 10G | Ausente | Coberto | Consumer intermediário | SurfaceSet + Runtime/Binding conforme escopo | Baixo / controlado | Sem ajuste. |
| Pause/unpause lifecycle | Preservar boundary: pause recebe evento, não controla Activity | F10 | F10 | IF-FW-ROAD-10F | Ausente | Coberto | Consumer intermediário | SurfaceSet + Runtime/Binding conforme escopo | Baixo / controlado | Sem ajuste. |

### Camera
| Capacidade | Decisão | Fase original | Fase revisada | Corte | Status package | Status roadmap | Prioridade | Bloqueadores | Risco | Observação |
|---|---|---|---|---|---|---|---|---|---|---|
| Camera consumer | Redesenhar como `CameraConsumer` | F11 | F11 | IF-FW-ROAD-11A, 11B | Ambíguo / Experimental | Coberto | Consumer avançado | SurfaceSet + Runtime/Binding conforme escopo | Risco de consumer capturar core | Sem ajuste. |
| Camera anchor binding | Redesenhar com `SurfaceAnchor` typed | F11 | F11 | IF-FW-ROAD-11B | Ambíguo / Experimental | Coberto | Consumer avançado | SurfaceSet + Runtime/Binding conforme escopo | Risco de consumer capturar core | Sem ajuste. |
| Camera scope (Session/Route/Activity) | Preservar modelo de escopo | F11 | F11 | IF-FW-ROAD-11B | Ambíguo / Experimental | Coberto | Consumer avançado | SurfaceSet + Runtime/Binding conforme escopo | Risco de consumer capturar core | Sem ajuste. |
| Camera binding result/handle | Criar `CameraBindingResult` com handle | F11 | F11 | IF-FW-ROAD-11B | Ambíguo / Experimental | Coberto | Consumer avançado | SurfaceSet + Runtime/Binding conforme escopo | Risco de consumer capturar core | Sem ajuste. |
| Virtual camera binding | Redesenhar como consumer de Surface; não no core | F11 | F11 | IF-FW-ROAD-11B | Ambíguo / Experimental | Coberto | Consumer avançado | SurfaceSet + Runtime/Binding conforme escopo | Risco de consumer capturar core | Sem ajuste. |

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
| Required/optional policy | Centralizar em policy por scope | F5 | F5 | IF-FW-ROAD-5F | Parcial | Coberto | Core | Baseline reconciliado | Baixo / controlado | Sem ajuste. |
| Smoke doc (BASELINE_SMOKE) | Criar/manter `BASELINE_SMOKE.md` | F0B | F0B | IF-FW-ROAD-0B7 | Presente / Parcial | Coberto | Core | Baseline reconciliado | Baixo / controlado | Sem ajuste. |

---
## 5. Índice por fase revisado

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

### Futura F2/F3
- Session — Pipeline de composição em fases

### F3
- Route — Route asset declarativo
- Route — Route runtime state
- Route — Route exit plan
- Route — Primary scene loading
- Route — Route → Activity handoff
- Route — Route startup activity policy

### Deferred após F4/F6
- Activity — Activity content profile

### F4
- Activity — Activity asset declarativo
- Activity — Activity runtime state
- Activity — Activity entry context
- Activity — Activity content set
- Activity — Activity content lifecycle result
- Activity — Readiness gate

### F5
- Local — Contributor marker
- Local — Local content identity
- Local — Scoped contribution discovery
- Local — Contribution set
- Local — Requiredness policy
- Local — Stale/foreign reference check
- Diagnostics / QA — Required/optional policy

### F5 parcial / Runtime refs depois
- Local — Capability inventory

### F5/F7/F10+
- Route — Route contribution set

### Futura após F5/F10
- Activity — Activity reset plan

### F6
- Route — Scene composition plan/result
- Route — Additive scene loading
- Route — Active scene policy
- Route — Route content profile execution
- Route — Content release plan

### F6 ou futura Session Persistent Content
- Session — Persistent scenes policy

### F6+
- Activity — Activity content loading

### F6/F8
- Activity — Activity exit plan

### F7
- Route — Route surface set
- Local — Local slots
- Local — Local anchors
- Surface — Surface identity
- Surface — Surface root
- Surface — Surface slot
- Surface — Surface anchor
- Surface — Surface endpoint
- Surface — Surface set por scope
- Surface — Duplicate detection
- Surface — Overlay root separado de content root

### F8
- RuntimeSpawned — Runtime scope root
- RuntimeSpawned — Runtime root registry
- RuntimeSpawned — Materialization request/result
- RuntimeSpawned — Runtime content handle
- RuntimeSpawned — Prefab materializer
- RuntimeSpawned — Runtime release policy
- RuntimeSpawned — Runtime identity
- RuntimeSpawned — Destroy policy

### F8+
- Local — Runtime capability reference

### F8/F10
- Local — Local release participant
- Local — Exit freeze

### F9
- RuntimeSpawned — Spawn origin / slot
- Surface — Surface binding request/result
- Surface — Surface content handle
- Surface — Runtime surface binding
- Surface — Surface lifecycle policy

### F9–F10
- Activity — Pause content/surface contribution
- Pause — Pause content materialization

### F10
- Activity — Activity snapshot
- Local — Local snapshot participant
- Input — Input mode contract
- Input — Activity input consumer
- Input — Input mode owner
- Save / Snapshot — Snapshot envelope
- Save / Snapshot — Snapshot participant contract
- Save / Snapshot — Snapshot set
- Save / Snapshot — Save backend port
- Save / Snapshot — Schema versioning
- Pause — Pause surface consumer
- Pause — Pause state contract
- Pause — Pause/unpause lifecycle

### F10+
- Local — Local reset participant

### F10+/F11
- Session — Player participation

### F10–F11
- Activity — Participant binding
- Input — Player slot / command binding

### F11
- RuntimeSpawned — Pooled materializer
- RuntimeSpawned — Runtime spawned contribution
- RuntimeSpawned — Pool rent/return cycle
- Camera — Camera consumer
- Camera — Camera anchor binding
- Camera — Camera scope (Session/Route/Activity)
- Camera — Camera binding result/handle
- Camera — Virtual camera binding
- Audio — BGM lifecycle consumer
- Audio — SFX dispatch
- Actor — Actor materialization request/result
- Actor — Actor contribution
- Actor — Actor runtime identity
- Actor — Actor reset/release
- Actor — Player actor binding
- Pooling — Pool rent
- Pooling — Pool return
- Pooling — Pool ownership

### F11 ou SessionContent futuro
- Audio — Audio listener

### F11+
- Session — Subsystem composition descriptors

### F12
- Projectile / Damage / Attributes — Projectile as RuntimeSpawned
- Projectile / Damage / Attributes — Impact/collision handling
- Projectile / Damage / Attributes — Damage as actor capability
- Projectile / Damage / Attributes — Attributes as snapshot-capable

### Deferred
- Session — Session composition context

### Todas
- Diagnostics / QA — Authoring validator

---
## 6. Gaps de rastreabilidade

Itens que precisam de detalhamento futuro porque estavam como `—`, `Fase+` ou dependem de bloqueadores:

- **Session — Pipeline de composição em fases**: Futura F2/F3. Não obrigatório como corte imediato. Preservar o padrão, mas evitar composition genérico cedo demais.
- **Session — Session composition context**: Deferred. Conceito válido, mas perigoso cedo. Primeiro formalizar Session scope/content sem service locator.
- **Session — Player participation**: F10+/F11. Adiar. Entra com Input/Actor boundary, não no core inicial.
- **Session — Subsystem composition descriptors**: F11+. Como padrão de consumer/adapters, não Session core.
- **Route — Route contribution set**: F5/F7/F10+. Contribution genérico em F5; surfaces em F7; consumers reais depois.
- **Activity — Activity content profile**: Deferred após F4/F6. F4 deve criar ActivityContentSet/readiness mínimo; profile avançado vem depois de scene composition/release.
- **Activity — Activity content loading**: F6+. Carregamento real de cenas da Activity depende de plan/result/release.
- **Activity — Activity reset plan**: Futura após F5/F10. Depende de LocalContributionSet e snapshot/reset participants.
- **Local — Runtime capability reference**: F8+. Correto: depende de RuntimeContentHandle/lifetime validity.
- **Local — Local reset participant**: F10+. Depende de LocalContributionSet e capability participant model.

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
| Settings source policy | Define a origem explícita de settings do bootstrap sem fallback silencioso. | `ImmersiveFrameworkSettingsAsset`, `Resources.Load` | Aceitar como temporário e documentado para F2 | F2 | IF-FW-ROAD-2E |
| Startup route signal | Dispara a rota inicial após Session estar pronta. | `StartupRequestEmitter`, `StartupRouteEmitter` | Preservar sinal; remover evento estático global | F2 | IF-FW-ROAD-2F |
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
| Scene composition plan/result | Cria plano de load/unload antes de executar side effects. | `SessionOperationalPipeline` scene stages | Preservar plan/result explícitos | F6 | IF-FW-ROAD-6A, 6B, 6C |
| Primary scene loading | Carrega cena principal da rota. | `SceneLifecycleRuntime` (package) | Preservar; já existe no package | F3 | IF-FW-ROAD-3C |
| Additive scene loading | Carrega cenas adicionais da rota em modo additive. | `SessionOperationalPipeline` scene stages | Preservar com plan/result | F6 | IF-FW-ROAD-6D |
| Active scene policy | Define qual cena é a ativa após composição. | Pipeline de composição de cenas | Preservar como parte do plan | F6 | IF-FW-ROAD-6A |
| Route content profile execution | Executa requiredness e carrega cenas declaradas no profile. | `RouteContentProfileAsset` + plan | Preservar execução; separar de planning | F6 | IF-FW-ROAD-6E |
| Route → Activity handoff | Passa contexto mínimo para Activity iniciar após rota pronta. | `SessionActivityEntryHandoff` | Preservar boundary; reduzir payload | F3 | IF-FW-ROAD-3C |
| Route startup activity policy | Decide qual Activity iniciar automaticamente ao entrar na rota. | `OperationalRouteAsset.StartupActivity` | Preservar como policy simples | F3 | IF-FW-ROAD-3C |
| Route contribution set | Câmera/áudio/input/save da rota expostos como contributions. | Pipeline stages de route | Redesenhar como `RouteContributionSet` | F5+ | F7 via Surface |
| Route surface set | Surfaces de apresentação da rota (pause surface, overlay). | Pause stages + surface endpoints | Redesenhar como `RouteSurfaceSet` | F7 | IF-FW-ROAD-7G |
| Content release plan | Libera cenas/conteúdo da rota com plano explícito. | `SessionOperationalPipeline` teardown | Preservar como `ContentReleasePlan` | F6 | IF-FW-ROAD-6F |

---

## Grupo 3 — Activity

| Capacidade | O que faz | Origem NS | Decisão | Fase | Corte |
|---|---|---|---|---|---|
| Activity asset declarativo | Define unidade jogável/contextual. | `ActivityAsset` | Preservar; reduzir fields cross-cutting | F4 | IF-FW-ROAD-4A |
| Activity content profile | Declara cenas/conteúdo que a Activity precisa carregar. | `ActivityContentProfileAsset`, `ActivityContentSceneEntry` | Preservar como perfil separado | F4 | IF-FW-ROAD-4B |
| Activity runtime state | Estado ativo/none/transition da Activity. | `SessionActivityIdentity`, `SessionActivityCycleKey` | Redesenhar com typed IDs | F4 | IF-FW-ROAD-4A |
| Activity entry context | Contexto de entrada recebido do handoff da Route. | `SessionActivityEntryHandoff` | Preservar boundary; reduzir payload | F4 | IF-FW-ROAD-4B |
| Activity content set | Registro de cenas/conteúdo efetivamente carregados. | `ActivityContentLoadedSet` | Preservar como `ActivityContentSet` | F4 | IF-FW-ROAD-4B |
| Activity content loading | Carrega cenas de conteúdo da Activity via adapter. | `UnityActivityContentSceneAdapter` | Preservar com plan/result/handles | F4–F6 | IF-FW-ROAD-4B, 6D |
| Activity content lifecycle result | Resultado tipado do enter/exit com referência ao ContentSet. | Implícito na pipeline | Criar como `ActivityContentLifecycleResult` | F4 | IF-FW-ROAD-4C |
| Readiness gate | Só revela gameplay quando todos objetos/actors estão prontos. | `ActivityParticipantReadinessStage`, `ISessionActivityVisualReadinessBoundary` | Preservar como state/result | F4 | IF-FW-ROAD-4D |
| Activity exit plan | Planeja release, unload e teardown ao sair da Activity. | Pipeline teardown stages | Preservar como `ActivityExitPlan` | F4 | IF-FW-ROAD-4B |
| Activity reset plan | Reseta estado da Activity sem descarregar conteúdo. | `ActivityObjectResetContracts` | Preservar; entrar depois de LocalContributionSet | F5+ | — |
| Participant binding | Conecta participação (player slots) a actors e input. | `ActivityEntryParticipantBindingStage` | Preservar boundary; redesenhar ownership | F10–F11 | IF-FW-ROAD-10B, 11F |
| Pause content/surface contribution | Activity contribui surface de pause para a rota. | `ActivityEntryPauseContentStage`, `ActivityPauseContentRuntimeState` | Redesenhar como Surface consumer | F9–F10 | IF-FW-ROAD-9D, 10G |
| Activity snapshot | Captura estado da Activity para save. | `SessionActivitySnapshot` | Preservar contrato; entrar após snapshot model | F10 | IF-FW-ROAD-10D, 10E |
| Activity diagnostics/facts | Facts estruturados de lifecycle de Activity. | `SessionActivityFact` | Preservar como `FrameworkFact` | F1 | IF-FW-ROAD-1C |

---

## Grupo 4 — Local

| Capacidade | O que faz | Origem NS | Decisão | Fase | Corte |
|---|---|---|---|---|---|
| Contributor marker | Objeto authored marca-se como contribuidor relevante para Activity/Route. | `ActivityObjectContributor` | Preservar como `LocalContributionMarker` | F5 | IF-FW-ROAD-5C |
| Local content identity | Identidade tipada de objeto local; substitui `targetId` string. | `ActivityObjectContributor.targetId` (frágil) | Redesenhar como `LocalContentIdentity` | F5 | IF-FW-ROAD-5A, 5B |
| Scoped contribution discovery | Descobre contributors dentro do conteúdo carregado do escopo. | `ActivityEntryObjectContributorDiscoveryStage` | Preservar discovery scoped; remover scan global | F5 | IF-FW-ROAD-5D |
| Contribution set | Conjunto tipado de contributions por escopo (Activity ou Route). | `ActivityObjectContributor` → inventory | Criar `LocalContributionSet` | F5 | IF-FW-ROAD-5E |
| Requiredness policy | Required ausente falha com fact estruturado; optional gera skip. | Distribuída na pipeline | Centralizar em policy | F5 | IF-FW-ROAD-5F |
| Capability inventory | Lista endpoints/providers vivos com validade por lifecycle. | `ActivityObjectCapabilityScanner`, `ActivitySetupInventoryBuilder` | Preservar como `LocalCapabilityDescriptor` | F5 | IF-FW-ROAD-5E |
| Runtime capability reference | Referência runtime a capability com validade por lifecycle. | `ActivityObjectCapabilityScanner` — runtime refs | Preservar com validade por lifecycle | F8+ | após RuntimeHandle |
| Local reset participant | Objeto local participa de reset de Activity. | `ActivityObjectResetContracts`, reset endpoint providers | Preservar contrato | F8+ | após LocalContributionSet |
| Local snapshot participant | Objeto local captura/restaura próprio estado. | `ActivityObjectSnapshotContracts` | Preservar contrato com typed payload | F10 | IF-FW-ROAD-10D |
| Local release participant | Objeto local declara release policy. | `ActivityObjectReleaseContracts` | Preservar contrato | F8 | IF-FW-ROAD-8G |
| Exit freeze | Congela correlação Local → Activity no teardown para evitar stale. | Implícito na pipeline de exit | Preservar como parte de `ActivityExitPlan` | F8 | IF-FW-ROAD-8G |
| Local slots | Pontos authored tipados para binding físico. | Surface endpoints e transform anchors | Preservar como `LocalSlot` (futuramente `SurfaceSlot`) | F7 | IF-FW-ROAD-7D |
| Local anchors | Pontos authored de posicionamento tipados. | Transform anchors em cena | Preservar como `LocalAnchor` (futuramente `SurfaceAnchor`) | F7 | IF-FW-ROAD-7E |
| Stale/foreign reference check | Detecta referências de lifecycle morto ou de escopo errado. | Implícito em inventory builders | Preservar como validação de `LocalContributionSet` | F5 | IF-FW-ROAD-5F |

---

## Grupo 5 — RuntimeSpawned

| Capacidade | O que faz | Origem NS | Decisão | Fase | Corte |
|---|---|---|---|---|---|
| Runtime scope root | Raiz por escopo (Session/Route/Activity) para instâncias dinâmicas. | Criado por adapters ad hoc (problema) | Redesenhar como `RuntimeScopeRoot` com registry | F8 | IF-FW-ROAD-8B, 8C |
| Runtime root registry | Associa scope → root sem `GameObject.Find` ou statics. | Implícito; criação espalhada por adapters | Criar `RuntimeRootRegistry` | F8 | IF-FW-ROAD-8C |
| Materialization request/result | Contrato genérico de criação auditável: request antes, result depois. | `ActivityEntryPipeline` actor stages | Preservar como `RuntimeMaterializationRequest/Result` | F8 | IF-FW-ROAD-8E |
| Runtime content handle | Handle imutável de instância com owner, scope e release callback. | Actor runtime refs (implícitas) | Criar `RuntimeContentHandle` | F8 | IF-FW-ROAD-8D |
| Prefab materializer | Instancia prefab, registra handle, devolve result. | Adapters de actor/presentation | Preservar como `PrefabContentMaterializer` | F8 | IF-FW-ROAD-8F |
| Runtime release policy | Limpa instâncias ao sair do escopo (Activity exit, Route exit, Session shutdown). | Teardown stages espalhados | Criar `RuntimeReleasePolicy` por escopo | F8 | IF-FW-ROAD-8G |
| Pooled materializer | Materializa via pool rent; devolve via pool return no release. | `PoolingService` + actor adapters | Redesenhar como `PooledContentMaterializer` | F11 | IF-FW-ROAD-11H |
| Runtime spawned contribution | Instância dinâmica pode contribuir capabilities ao `ActivityContributionSet`. | Actor como contributor (implícito) | Preservar como `RuntimeSpawnedContribution` | F11 | IF-FW-ROAD-11F |
| Spawn origin / slot | Posicionamento authored de onde instâncias aparecem. | Transform anchors em cena | Integrar com `LocalAnchor` / `SurfaceSlot` | F9 | IF-FW-ROAD-9D |
| Runtime identity | Distingue instâncias individuais sem usar nome de GameObject. | `ActorInstanceRuntimeId` (tipado) | Preservar typed; generalizar para todo spawned | F8 | IF-FW-ROAD-8D |
| Pool rent/return cycle | Reutiliza objetos: rent antes de usar, return no release. | `PoolingService` | Preservar como infra técnica separada | F11 | IF-FW-ROAD-11G, 11H |
| Destroy policy | Destroy explícito apenas quando pool return não se aplica. | `Destroy` espalhado (problema) | Centralizar em `RuntimeReleasePolicy` | F8 | IF-FW-ROAD-8G |

---

## Grupo 6 — Surface

| Capacidade | O que faz | Origem NS | Decisão | Fase | Corte |
|---|---|---|---|---|---|
| Surface identity | Identidade tipada de surface; distingue pause/camera/overlay/content. | String solta por endpoint (problema) | Criar `SurfaceIdentity` tipado | F7 | IF-FW-ROAD-7B |
| Surface root | Raiz física (Transform) associada a uma surface com role definido. | Container de pause, overlay root | Criar `SurfaceRoot` com role tipado | F7 | IF-FW-ROAD-7C |
| Surface slot | Ponto authored de binding físico; não usa `GameObject.Find`. | Surface slots em cena | Criar `SurfaceSlot` typed | F7 | IF-FW-ROAD-7D |
| Surface anchor | Ponto authored de posicionamento typed. | Anchors de câmera/actor | Criar `SurfaceAnchor` typed | F7 | IF-FW-ROAD-7E |
| Surface endpoint | Componente que declara surface (roots/slots/anchors) para um scope. | Surface endpoint components | Criar `SurfaceEndpoint` | F7 | IF-FW-ROAD-7F |
| Surface set por scope | Route e Activity expõem seus `SurfaceSet`; consumers solicitam por identity. | Implícito por scope/stage | Criar `RouteSurfaceSet`, `ActivitySurfaceSet` | F7 | IF-FW-ROAD-7G |
| Surface binding request/result | Consumer solicita surface por identity; resultado devolve handle. Consumer não controla lifetime. | Direto por adapter (problema) | Criar `SurfaceBindingRequest/Result` | F9 | IF-FW-ROAD-9A, 9B |
| Surface content handle | Handle de binding com release; consumer libera, não destrói. | Binding sem handle formal (problema) | Criar `SurfaceContentHandle` | F9 | IF-FW-ROAD-9C |
| Runtime surface binding | Materializa conteúdo (prefab) dentro de slot/root de surface. | `RuntimeSurfaceBinding` (implícito em presentation) | Criar `RuntimeSurfaceBinding` | F9 | IF-FW-ROAD-9D |
| Duplicate detection | Detecta surface endpoint/slot duplicado na mesma cena/scope. | Implícito em validators | Preservar em Surface validators | F7 | IF-FW-ROAD-7H |
| Surface lifecycle policy | Consumer libera binding; surface não depende de consumer para cleanup. | Implícito no teardown | Preservar policy explícita de release | F9 | IF-FW-ROAD-9E |
| Overlay root separado de content root | Roles distintos para UI de pausa vs. conteúdo. | `SurfaceRoot.Role` implícito | Preservar modelagem de roles | F7 | IF-FW-ROAD-7C |

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
| Pause surface consumer | Pause usa surface de Route/Activity; não cria sua própria. | `ActivityEntryPauseContentStage`, pause surface stages | Redesenhar como `PauseSurfaceConsumer` | F10 | IF-FW-ROAD-10F, 10G |
| Pause state contract | Estado de pause tipado; Route/Activity não sabem de Pause como subsistema. | `SessionActivityPipeline` pause handling | Criar `PauseStateContract` | F10 | IF-FW-ROAD-10F |
| Pause content materialization | Materializa UI/overlay de pause em surface slot. | Pause stages + surface binding | Usa `RuntimeSurfaceBinding` | F9–F10 | IF-FW-ROAD-9D, 10G |
| Pause/unpause lifecycle | Pause é consumer de Activity state; não possui lifecycle. | Implícito na pipeline | Preservar boundary: pause recebe evento, não controla Activity | F10 | IF-FW-ROAD-10F |

---

## Grupo 10 — Camera

| Capacidade | O que faz | Origem NS | Decisão | Fase | Corte |
|---|---|---|---|---|---|
| Camera consumer | Resolve câmera virtual ativa via SurfaceSet; sem authority global estático. | `OperationalCameraRuntimeComposition`, `FrameworkCameraAuthority` (problema) | Redesenhar como `CameraConsumer` | F11 | IF-FW-ROAD-11A, 11B |
| Camera anchor binding | Câmera física posicionada via `SurfaceAnchor`; não por nome de GameObject. | Camera anchor stages (string-based, problema) | Redesenhar com `SurfaceAnchor` typed | F11 | IF-FW-ROAD-11B |
| Camera scope (Session/Route/Activity) | Câmera muda conforme scope ativo; Route câmera ≠ Activity câmera. | Implícito em stages | Preservar modelo de escopo | F11 | IF-FW-ROAD-11B |
| Camera binding result/handle | Handle de câmera ativa com release ao sair do scope. | Sem handle formal (problema) | Criar `CameraBindingResult` com handle | F11 | IF-FW-ROAD-11B |
| Virtual camera binding | Liga virtual camera Cinemachine a surface/anchor typed. | `FrameworkCinemachineCameraBinding` (package) | Redesenhar como consumer de Surface; não no core | F11 | IF-FW-ROAD-11B |

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

> Use este índice para checar quais capacidades pertencem a cada fase antes de planejar um sprint.

| Fase | Capacidades cobertas |
|---|---|
| **F0B** | Boot global, QA Canvas boundary, Smoke doc |
| **F1** | Config de runtime, Runtime policy, Session diagnostics/facts, FrameworkFact, Typed identity policy, Content identity, Boot fail-fast |
| **F2** | Session runtime state, Session composition context, Persistent scenes, Session content set, Settings source policy, Startup route signal, Audio listener (Session) |
| **F3** | Route asset, Route runtime state, Route exit result, Route primary scene, Route → Activity handoff, Route startup activity policy |
| **F4** | Activity asset, Activity content profile, Activity runtime state, Activity entry context, Activity content set, Activity content lifecycle result, Readiness gate, Activity exit plan |
| **F5** | Contributor marker, Local content identity, Scoped discovery, Contribution set, Requiredness policy, Capability inventory (descriptor), Stale/foreign check |
| **F6** | Scene composition plan/result, Additive scene loading, Active scene policy, Route content profile execution, Content release plan |
| **F7** | Surface identity, Surface root, Surface slot, Surface anchor, Surface endpoint, Surface set por scope, Duplicate detection, Overlay root vs content root, Local slots/anchors |
| **F8** | Runtime scope root, Runtime root registry, Materialization request/result, Runtime content handle, Prefab materializer, Runtime release policy, Destroy policy, Runtime identity, Local release participant, Exit freeze |
| **F9** | Surface binding request/result, Surface content handle, Runtime surface binding, Spawn origin/slot, Surface lifecycle policy, Pause content materialization |
| **F10** | Input mode contract, Activity input consumer, Input mode owner, Player slot/command binding, Snapshot envelope, Snapshot participant, Snapshot set, Save backend port, Schema versioning, Pause surface consumer, Pause state contract, Pause lifecycle, Activity snapshot, Local snapshot participant |
| **F11** | Camera consumer, Camera anchor binding, Camera scope, Camera binding result, Virtual camera binding, BGM lifecycle consumer, SFX dispatch, Actor materialization, Actor contribution, Actor runtime identity, Actor reset/release, Player actor binding, Pooling rent/return, Pool ownership, Pooled materializer, Runtime spawned contribution |
| **F12** | Projectile as RuntimeSpawned, Impact handling, Damage as capability, Attributes as snapshot-capable |


### F2B — SessionRuntimeState explicit boundary

| Capability | Cut | Status | Evidence | Notes |
|---|---|---|---|---|
| Session runtime state | F2B | APPLIED / PENDING COMPILE-SMOKE | `Runtime/SessionLifecycle/SessionRuntimeState.cs` | Explicit Session state boundary created. `FrameworkRuntimeState` remains compatibility facade. |

F2B intentionally does not implement `SessionContentSet`, persistent scenes, Route baseline, Surface or RuntimeMaterialization.
