# Immersive Framework — Roadmap Revisado

Status: versão revisada a partir do roadmap externo e das auditorias consolidadas  
Modo: planejamento arquitetural / sem patch / sem implementação  
Uso pretendido: orientar ADRs, cortes técnicos e validação incremental do `com.immersive.framework`.

---

## 0. Decisão central

```text
O framework deve nascer como lifecycle/content/contribution framework.
Não como camera framework, audio framework, actor framework ou clone do Base 2.0.
```

A regra prática é:

```text
Primeiro estabilizar owners, identidade, conteúdo e contribuição.
Depois materialização, Content Anchor e release.
Só então consumidores como Input, Save, Pause, Camera, Audio, Actor, Pooling e Projectile.
```

---

## 1. Avaliação do roadmap original

O roadmap original está correto na direção macro:

- começa por reconciliação do baseline;
- preserva o que o package já acertou;
- reconhece `CameraFlow`, `RouteContentRuntime`, `ContentFlow`, `RouteContentProfileAsset`, `FrameworkQaCanvas` e `ValidationMode` como decisões obrigatórias;
- mantém camera/audio/actor/save fora do core inicial;
- traz `LocalContributionSet`, `RuntimeRootRegistry`, `Content Anchor` e consumers em fases posteriores.

Mas ele ainda pode gerar retrabalho por quatro motivos.

### 1.1. Fase 0 mistura decisão, documentação, remoção e conexão

A fase 0 diz “nada de código novo”, mas também permite remover código, isolar classes, conectar `RouteContentRuntime`, expandir validator e executar smoke. Isso é muita coisa para uma fase de reconciliação.

Correção:

```text
Separar Fase 0A — decisões/documentos
de Fase 0B — higiene mínima do baseline
e deixar conexão funcional de RouteContentRuntime para uma fase própria.
```

### 1.2. Additive scene support entra cedo demais

No roadmap original, additive scene support aparece dentro de “Runtime roots e Release”. Isso mistura dois problemas diferentes:

```text
Scene composition
≠ Runtime spawned roots
≠ Materialização física runtime
```

Correção:

```text
Route scene composition/additive deve ficar em fase própria de Route Content,
depois de RouteContentSet e release semantics.
```

### 1.3. Content Anchor aparece como se dependesse de RuntimeSpawned

Content Anchor é primeiro um contrato authored/local:

```text
ContentAnchorIdentity
ContentAnchorRoot
ContentAnchorSlot
ContentAnchorPoint
RouteContentAnchor
ContentAnchorSet
```

Ela não precisa começar com prefab spawn. Só a segunda camada, `ContentAnchorBinding`/`ContentAnchorContentHandle`, depende de materialização/release.

Correção:

```text
Dividir Content Anchor em:
1. Content Anchor declaration baseline.
2. Content Anchor binding/runtime content depois.
```

### 1.4. LocalContribution já quer substituir discovery global cedo demais

Remover `FindObjectsByType` do fluxo principal é correto, mas só é seguro depois de existir um `ActivityContentSet` real que defina o escopo de busca.

Correção:

```text
ActivityContentSet vem antes de LocalContributionDiscovery.
Local discovery deve operar sobre content sets já conhecidos.
```

---

## 2. Regras de governança do roadmap

### 2.1. Tipos de corte

| Tipo | Pode alterar código? | Finalidade |
|---|---:|---|
| ADR | Não, salvo docs | Decidir ownership, status ou semântica. |
| Hygiene | Sim, mínimo | Remover contradição, renomear, marcar experimental, mover tooling. |
| Foundation | Sim | Criar primitivo usado pelo próximo corte. |
| Integration | Sim | Conectar um primitivo ao fluxo real. |
| Consumer | Sim | Plugar subsistema sem capturar core. |
| Gameplay | Sim | Feature final apoiada em consumers e primitives. |

### 2.2. Critério para avançar fase

Uma fase só fecha quando:

```text
1. O baseline compila.
2. Smoke feliz mínimo passa.
3. Nenhuma API entry point pública nova fica ambígua.
4. Toda API nova está marcada como Stable, Experimental ou Internal.
5. Não há fallback silencioso para caminho required.
6. O docs/ADR refletem o código.
```

### 2.3. Regra contra abstração prematura

Não criar:

```text
manager global
service locator público
materializer genérico sem primeiro uso real
contribution inventory sem discovery real
content anchor consumer antes de ContentAnchorSet
pooling antes de RuntimeContentHandle
camera antes de Content Anchor
actor antes de RuntimeMaterialization
```

---

# Parte I — Fases Revisadas

## Fase 0A — Reconciliação de baseline por ADR

Objetivo: decidir o status oficial do que já existe, sem implementar feature nova.

| ID | Entrega | Decisão |
|---|---|---|
| IF-FW-ROAD-0A1 | ADR: Baseline Reconciliation | Documento único decidindo CameraFlow, RouteContentRuntime, ContentFlow API, RouteContentProfile, QA Canvas e ValidationMode. |
| IF-FW-ROAD-0A2 | ADR: Core vs Consumers | Formalizar que camera/audio/actor/save/input/pause/projectile não ditam o core. |
| IF-FW-ROAD-0A3 | ADR: Public API Status Policy | Definir marcadores: Stable, Experimental, Internal, Deferred. |
| IF-FW-ROAD-0A4 | ADR: QA/Diagnostics Boundary | Definir se `FrameworkQaCanvas` é runtime dev tool, editor tooling ou package separado. |
| IF-FW-ROAD-0A5 | ADR: Dependency Policy | Definir quando dependência como Cinemachine pode entrar no core. |

### Done

- Decisões explícitas registradas.
- Nenhum comportamento novo exigido.
- Cada item ambíguo tem um status: ativo, experimental, congelado, removido ou futuro.

---

## Fase 0B — Higiene mínima do baseline

Objetivo: aplicar somente o necessário para o código não contradizer o baseline decidido.

| ID | Entrega | Regra |
|---|---|---|
| IF-FW-ROAD-0B1 | CameraFlow baseline action | Se removido: apagar código/dependência. Se congelado: excluir do fluxo e marcar claramente. Se ativo: documentar e validar. |
| IF-FW-ROAD-0B2 | RouteContentRuntime baseline action | Não conectar feature nova aqui. Apenas remover/congelar se a decisão for deferir. Se a decisão for conectar, entra na Fase 3. |
| IF-FW-ROAD-0B3 | ContentFlow API status | Marcar materializer/contribution como Experimental/Internal se ainda sem uso real. |
| IF-FW-ROAD-0B4 | RouteContentProfile UX | Marcar como planning-only no Inspector/docs enquanto não executa. |
| IF-FW-ROAD-0B5 | QA Canvas boundary | Aplicar diretiva/dev-only ou documentação de runtime dev tool. |
| IF-FW-ROAD-0B6 | Namespace editor hygiene | Corrigir `Immersive.Framework.Editor.Editor.*` se for corte seguro. |
| IF-FW-ROAD-0B7 | Baseline smoke doc | Criar/atualizar `BASELINE_SMOKE.md` com boot, route switch, activity switch e clear. |

### Done

```text
Código, README e ADRs não se contradizem.
Smoke baseline ainda passa.
Nenhum feature novo foi criado.
```

---

## Fase 1 — Identidade, status de API e diagnostics mínimos

Objetivo: colocar travas antes de expandir ContentFlow/Local/Runtime.

| ID | Entrega | Detalhes |
|---|---|---|
| IF-FW-ROAD-1A | `FrameworkApiStatus` ou convenção equivalente | Status documental/código para Stable, Experimental, Internal, Deferred. |
| IF-FW-ROAD-1B | ADR: Typed Identity Policy | Strings podem existir como labels/diagnostics; não como chave funcional sem domínio. |
| IF-FW-ROAD-1C | `FrameworkFact` mínimo | Fato estruturado para validation/smoke/diagnostics. Log humano continua separado. |
| IF-FW-ROAD-1D | `ValidationMode` semantics | Definir efeito mínimo: Strict, Standard, Release. |
| IF-FW-ROAD-1E | Content identity ADR | Domínio de identity para handles de content. Sem fallback Guid instável como contrato público. |
| IF-FW-ROAD-1F | Revisar `FrameworkContentHandle` | Aplicar policy de identity ou marcar pontos instáveis como experimental. |

### Não entra

- SessionContentSet.
- Runtime roots.
- Materializer.
- Content Anchor.
- Additive scene.
- Consumers.

### Done

```text
Novas APIs passam a ter status.
Diagnostics estruturado mínimo existe.
Content identity deixa de avançar sem ADR.
ValidationMode deixa de ser enum decorativo.
```

---

## Fase 2 — Session scope e content ownership mínimo

Objetivo: formalizar Session como owner sem criar service locator.

| ID | Entrega | Detalhes |
|---|---|---|
| IF-FW-ROAD-2A | ADR: Session Scope | `FrameworkRuntimeHost` é owner da Session runtime? Decidir. |
| IF-FW-ROAD-2B | `SessionRuntimeState` explícito | Estado de app/session separado da classe host. |
| IF-FW-ROAD-2C | `SessionContentSet` mínimo | Registro de conteúdo persistente/session-scoped. Inicialmente pode ser vazio ou conter host/diagnostics. |
| IF-FW-ROAD-2D | `SessionContentOwnership` semantics | Distinguir conteúdo registrado, conteúdo owned e conteúdo diagnostic-only. |
| IF-FW-ROAD-2E | Settings source decision | Confirmar `Resources` como temporário ou oficial. |
| IF-FW-ROAD-2F | Session smoke | Boot → Session state válido → Startup route. |

### Não entra

- SessionCompositionContext genérico.
- Service registry.
- Persistent gameplay services.
- Camera/audio/input.

### Done

```text
Session tem owner formal.
SessionContentSet existe sem virar manager global.
RuntimeHost continua simples.
```

---

## Fase 3 — Route baseline e RouteContentRuntime

Objetivo: estabilizar Route sem pular para additive composition.

Status atual:

```text
F3A — CLOSED / ADRS ACCEPTED
F3B — CLOSED / COMPILE-SMOKE PASS
F3C — CLOSED / COMPILE-SMOKE PASS
F3D — CLOSED / COMPILE-SMOKE PASS
F3E — CLOSED / COMPILE-SMOKE PASS
F3F — CLOSED / CALLBACK-SMOKE PASS
F3F1 — CLOSED / COMPILE-SMOKE PASS
F3G — CLOSED / COMPILE-SMOKE PASS
F3G1 — CLOSED / COMPILE-SMOKE PASS
F3  — CLOSED / PASS
```

Corte técnico atual:

```text
F3 — CLOSED / PASS
```

Próximo corte autorizado após F3:

```text
F4A — IF-FW-ROAD-4A — ActivityRuntimeState refinado
```

Status: histórico. F4 já foi fechada no baseline atual.

| ID | Entrega | Detalhes |
|---|---|---|
| IF-FW-ROAD-3A | `RouteRuntimeState` tipado | CLOSED em F3B. Estado da rota ativa com identity própria, não só referência direta a `RouteAsset`. |
| IF-FW-ROAD-3B | `RouteExitResult` mínimo | CLOSED em F3C. Resultado explícito da saída anterior, mesmo que ainda simples. |
| IF-FW-ROAD-3C | `RouteContentRuntime` execution decision | CLOSED em F3D. Ativo e conectado ao `RouteLifecycleRuntime` para callbacks locais de Route Content na Primary Scene carregada. |
| IF-FW-ROAD-3D | `RouteContentSet` semantics | CLOSED em F3E. `RouteContentSet` tem `RouteContentEntry` e `RouteContentOwnership` explícitos; Primary Scene baseline é required/owned. |
| IF-FW-ROAD-3E | Route local callback smoke | CLOSED em F3F. QA Canvas tem smoke dedicado e `RouteContentLifecycleSmokeProbe` validado com receivers reais. F3F1 simplifica o painel e fechou por Standard Smoke. |
| IF-FW-ROAD-3F | Route validator expansion | CLOSED em F3G/F3G1. Valida `RouteContentBinding` em cenas carregadas via QA, Route errada e ausência de receivers; Inspector fica mínimo. |

### Não entra

- Additive scene execution.
- Runtime materialization.
- Content Anchor.
- Camera.

### Done

```text
RouteContentRuntime não fica ambíguo.
RouteContentSet tem semântica clara.
Route switch continua funcionando.
Callbacks locais de Route são validados com receivers reais.
Authoring mínimo de RouteContentBinding tem validação QA.
```

---

## Fase 4 — Activity content set e readiness mínimo

Objetivo: amadurecer Activity sem copiar `ActivityEntryPipeline`.

Status atual:

```text
F4 — CLOSED / ACTIVITY BASELINE PASS
F4A — CLOSED / COMPILE-SMOKE PASS
F4B — CLOSED / COMPILE-SMOKE PASS
F4C — CLOSED / COMPILE-SMOKE PASS
F4D — CLOSED / COMPILE-SMOKE PASS
F4E — CLOSED / COMPILE-SMOKE PASS
F4F — CLOSED / COMPILE-SMOKE PASS
F4G — CLOSED / COMPILE-SMOKE PASS
```

| ID | Entrega | Detalhes |
|---|---|---|
| IF-FW-ROAD-4A | `ActivityRuntimeState` refinado | CLOSED em F4A. Estado explícito `None`/`Active`, identidade tipada `Activity:*` e `Transitioning` reservado. |
| IF-FW-ROAD-4B | `ActivityContentSet` mínimo | CLOSED em F4B. Snapshot de conteúdo scene-authored local registrado para a Activity ativa; sem profile loading/materialization/release. |
| IF-FW-ROAD-4C | `ActivityContentLifecycleResult` | CLOSED em F4C. Resultado agregado de callbacks locais enter/exit, com contagem de bindings, receivers e falhas. |
| IF-FW-ROAD-4D | `ActivityReadinessState` mínimo | CLOSED em F4D. Readiness mínimo `Ready`/`None`/`NotReady` após aplicação baseline de Activity Content. |
| IF-FW-ROAD-4E | Reclassificar `ActivityLocalVisibilityAdapter` | CLOSED em F4E. `ActivityLocalVisibilityAdapter` é classe C# e authoring entry point do adapter local de visibilidade; não é materialização canônica. |
| IF-FW-ROAD-4F | Activity smoke | CLOSED em F4F. `Run Activity Baseline Smoke` valida switch → content set → readiness → clear → restore. |
| IF-FW-ROAD-4G | F4 closure hygiene | CLOSED em F4G. Remove warning redundante do Activity Baseline Smoke, alinha mensagens do Activity Local Visibility Adapter e registra a fronteira formal F4 → F5. |

### Não entra

- ActivityContentProfile.
- Actor/input/camera.
- Reset/snapshot/release.
- Local contribution inventory.

Nota de fronteira: `ActivityContentSet` em F4 é snapshot local/diagnóstico de adapters de visibilidade. `LocalContributionSet` em F5 deve definir identidade funcional própria, sem usar nome/path de GameObject ou cena como chave canônica.

### Done

```text
Activity tem ContentSet e Readiness mínimos.
ActivityLocalVisibilityAdapter continua simples, mas não confunde a arquitetura.
```

---

## Fase 5 — Local Contribution baseline

Objetivo: trazer o melhor do Local do NewScripts sem `targetId` universal, sem fallback por nome/path e sem scan global como fonte de verdade.

Status atual:

```text
F5A — CLOSED / ADR ACCEPTED
F5B — CLOSED / STANDARD COMPILE-SMOKE PASS
F5  — CLOSED / LOCAL CONTRIBUTION FOUNDATION PASS
```

Sequência obrigatória:

```text
ActivityContentSet / RouteContentSet como fronteira de busca
→ LocalContentIdentity explícita
→ explicit local ids on existing scene-authored bindings
→ Scoped discovery
→ LocalContributionSet
→ Required/Optional policy
→ Local validators
→ Local smoke dedicado
```

| ID | Entrega | Detalhes |
|---|---|---|
| IF-FW-ROAD-5A | ADR: Local Identity | CLOSED em F5A. Define `LocalContentIdentity`, proíbe path/name como chave funcional e bloqueia reaproveitamento direto do marker experimental com fallback por `GameObject.name`. |
| IF-FW-ROAD-5B | `LocalContentIdentity` | CLOSED / STANDARD COMPILE-SMOKE PASS. Tipo pequeno, imutável, validável, ordinal e sem fallback silencioso. Não cria marker/discovery. |
| IF-FW-ROAD-5C | `ActivityLocalVisibilityAdapter` / `RouteContentBinding` | CLOSED / QA COMPILE-SMOKE PASS. Bindings/adapters scene-authored recebem `Local Content Id` explícito; sem capability runtime ainda. |
| IF-FW-ROAD-5D | `LocalContributionDiscovery` loaded | CLOSED / QA COMPILE-SMOKE PASS. Descobre bindings/adapters carregados com Local Content Id explícito e reporta issues estruturadas. Integração formal por ContentSet fica diferida. |
| IF-FW-ROAD-5E | `LocalContributionSet` | CLOSED / QA COMPILE-SMOKE PASS. Set tipado e consultável por scope/source/identity, com resumo diagnóstico por escopo. |
| IF-FW-ROAD-5F | Required/Optional policy | CLOSED / QA COMPILE-SMOKE PASS. Requiredness passa a viajar no handle e no set; absence policy fica para validator/expected contribution. |
| IF-FW-ROAD-5G | Local validators | CLOSED / QA COMPILE-SMOKE PASS. Validação explícita sobre discovery/set; required expected ausente vira erro quando houver lista expected; optional expected ausente vira skip diagnóstico. |
| IF-FW-ROAD-5H | Local smoke | CLOSED / QA COMPILE-SMOKE PASS. Smoke dedicado valida loaded/secondary/primary local contribution snapshot sem materialização canônica. |

### Não entra

- Capabilities específicas.
- Runtime references.
- Reset/snapshot/release.
- Content Anchor.
- Actors.
- ActivityContentProfile loading.
- Canonical Activity materialization.

### Guardrails aceitos em F5A

```text
ActivityContentSet F4 pode delimitar discovery, mas não é identity funcional F5.
FrameworkContentContributionMarker era precursor experimental e foi removido no F5C por ficar obsoleto diante dos bindings/adapters scene-authored com Local Content Id explícito.
GameObject.name, scene path e hierarchy path são diagnostics, não chaves funcionais.
targetId universal não deve ser recriado.
```

### Done

```text
Local deixa de ser só SetActive.
Contribution existe sem virar capability system completo.
Discovery é scoped, não global.
Nenhuma identidade required depende de fallback silencioso.
```

---

## Fase 6 — Route scene composition e release de conteúdo

Objetivo: separar scene composition de runtime spawned/materialization.

| ID | Entrega | Detalhes |
|---|---|---|
| IF-FW-ROAD-6A | ADR: Route Scene Composition | Plan/result, primary/additive, active scene, ownership. |
| IF-FW-ROAD-6B | `RouteSceneCompositionPlan` | CLOSED / PASS. Plano inerte antes de load/unload; sem additive execution e sem release. |
| IF-FW-ROAD-6C | `RouteSceneCompositionResult` | CLOSED / PASS. Resultado inerte para evidência pós-composição. |
| IF-FW-ROAD-6D | Additive scene support | CLOSED / PASS. Primitivo interno `LoadAdditiveSceneAsync`. |
| IF-FW-ROAD-6E | `RouteContentProfileAsset` execution | CLOSED / PROFILE SMOKE PASS. Profile executado no fluxo de Route. |
| IF-FW-ROAD-6F | `ContentReleasePlan` mínimo | CLOSED / PASS. Modelo/planejamento de release por escopo. |
| IF-FW-ROAD-6G | Scene/release smoke | CLOSED / RELEASE SMOKE PASS. Unload físico de additional scene owned validado por QA smoke. |

### Não entra

- Materialization adapter boundary.
- Runtime spawned.
- Pool.
- Content Anchor consumers.

### Done

```text
Route additive não é efeito colateral.
Release de conteúdo tem plano/resultado.
RouteContentProfile deixa de parecer promessa falsa.
```

---

## Fase 7 — Content Anchor declaration baseline

Objetivo: criar Content Anchor como contrato authored/local, sem camera/pause/UI ainda.

| ID | Entrega | Detalhes |
|---|---|---|
| IF-FW-ROAD-7A | ADR/detail audit: Content Anchor como contrato de espaço | `CLOSED / DOCS`: Content Anchor não é camera, pause, UI ou presentation; nome ruim de anchor duplicado rejeitado. |
| IF-FW-ROAD-7B | `ContentAnchor` identity primitives | `ContentAnchorId`, `ContentAnchorScope`, `ContentAnchorKind`, `ContentAnchorRequiredness`. |
| IF-FW-ROAD-7C | `ContentAnchorRoot` | Root com role, sem materialização ainda. |
| IF-FW-ROAD-7D | `ContentAnchorSlot` | Slot authored tipado. |
| IF-FW-ROAD-7E | `ContentAnchorPoint` | Point authored tipado; substitui o nome ruim de anchor duplicado. |
| IF-FW-ROAD-7F | `RouteContentAnchor` authoring | Primeiro componente público de authoring por escopo; declara anchors de Route sem materialização. |
| IF-FW-ROAD-7G | `ContentAnchorSet` por scope | Route primeiro; Activity/Local só depois se a semântica continuar estável. |
| IF-FW-ROAD-7H | Content Anchor authoring validation | Missing Route/id, invalid kind, scene/Route mismatch and duplicate anchor identity/id. |
| IF-FW-ROAD-7I | F7 closure | Fechar docs e guardrails depois dos smokes. |

### Não entra

- `ContentAnchorBindingRequest` com prefab.
- Camera.
- Pause.
- UI.
- Presentation.
- Runtime materializer.

### Done

```text
Content Anchor existe como dado e contrato.
Nenhum consumer concreto ainda capturou o modelo.
```

### F7A status

```text
F7A — CLOSED / DOCS
F7B — CLOSED / PASS
F7C — CLOSED / PASS
F7D — CLOSED / PASS
F7E — CLOSED / PASS
F7F — CLOSED / PASS
F7G — CLOSED / PASS
F7H — CLOSED / PASS
F7I — CLOSED / DOCS
F7J — CLOSED / DOCS
Next — F8 continues / see F8 section
```

Naming guardrail: do not reintroduce the rejected previous placement-point vocabulary or `duplicated anchor naming` as canonical concept names.


---

## Fase 8 — Runtime roots, materialization e release

Objetivo: criar materialização runtime genérica, sem actor/projectile/pool.

| ID | Entrega | Detalhes |
|---|---|---|
| IF-FW-ROAD-8A | ADR/detail audit: Runtime roots/materialization | Aceitar fronteira: Runtime Root ≠ Content Anchor; F8 não cria consumers. |
| IF-FW-ROAD-8B | Runtime ownership primitives | `APPLIED`: Scope, owner, state e identity baseline para runtime-created content. |
| IF-FW-ROAD-8C | `RuntimeContentHandle` | `APPLIED`: Identity, owner scope, state and passive release diagnostics. |
| IF-FW-ROAD-8D | `RuntimeScopeRoot` + internal registry | `APPLIED`: Root lógico por escopo/owner, registry interno, sem `GameObject.Find`, sem hierarchy root real ainda. |
| IF-FW-ROAD-8E | `RuntimeContentRuntime` + `RuntimeScopeContext` | `APPLIED`: Owner interno do registry/context/handles e contexto explícito por owner, sem materialization request/result ainda. |
| IF-FW-ROAD-8F | Lifecycle integration for runtime roots | `APPLIED`: Session/Route/Activity criam/removem roots lógicos e contextos explicitamente no lifecycle, sem executar materialização física. |
| IF-FW-ROAD-8G | `RuntimeMaterializationRequest` / `Result` | `APPLIED`: Request/result/resource/status explícitos depois do owner/context/lifecycle roots; sem adapter físico. |
| IF-FW-ROAD-8H | Transition guard + scoped cancellation | `APPLIED`: Guardas para transição e cancelamento por escopo antes de qualquer adapter físico. |
| IF-FW-ROAD-8I | Materialization adapter boundary | `APPLIED / COMPILE-SMOKE PASS`: `IRuntimeMaterializationAdapter` boundary para adapters físicos externos ao core; sem Instantiate/Destroy no framework core. |
| IF-FW-ROAD-8J | `RuntimeReleasePolicy` / release execution | `APPLIED / COMPILE-SMOKE PASS`: release lógico por handle/scope; cleanup físico continua em adapters externos. |
| IF-FW-ROAD-8K | Runtime request/guard/release-policy smoke / F8 closure | `APPLIED / PENDING COMPILE + SMOKE`: request → guard → `ApplyMaterializationResult` → logical release/unregister → root removal → stale request rejection. |

### Não entra

- Pool.
- Actor.
- Projectile.
- Camera.
- Audio.
- Save.

### Done

```text
Runtime-spawned genérico existe sem feature gameplay.
Materialização não depende de subsistema específico.
```

---

## Fase 9 — Content Anchor binding e runtime placement

Objetivo: conectar a declaração F7 de Content Anchor com os contratos F8 de RuntimeContent, sem criar Pause, Camera, Actor ou UI concreta.

F9 continua sendo a ponte técnica entre espaço authored e runtime content. Ela não é consumer final.

| ID | Entrega | Detalhes |
|---|---|---|
| IF-FW-ROAD-9A | ADR: Content Anchor binding and runtime placement | Reafirmar que binding resolve espaço e ownership; não cria feature concreta. |
| IF-FW-ROAD-9B | `ContentAnchorBindingRequest` | APPLIED F9A — solicita root/slot/point por identity explícita e runtime scope context. |
| IF-FW-ROAD-9C | `ContentAnchorBindingResult` | APPLIED F9A — resultado tipado com diagnostics, status e eventual handle. |
| IF-FW-ROAD-9D | `ContentAnchorContentHandle` | APPLIED F9A — handle passivo que correlaciona Content Anchor e runtime content; release order fica para corte posterior. |
| IF-FW-ROAD-9E | `RuntimeContentAnchorBinding` | APPLIED F9B — runtime lógico interno que resolve ContentAnchorSet + RuntimeContentHandle; sem placement físico e sem lifecycle próprio. |
| IF-FW-ROAD-9F | Binding release order | Binding libera antes do release do content owner/root. |
| IF-FW-ROAD-9G | Content Anchor binding smoke | Anchor/slot resolved -> binding created -> release order validado -> zero orphan. |

### Não entra

- Pause overlay final.
- Camera rig.
- Actor presentation.
- Pooling.
- Loading screen.
- Input mode.
- Save/snapshot.

### Done

```text
Content Anchor pode receber runtime content sem virar Pause, Camera ou Actor.
O release de binding tem ordem definida antes dos consumers.
```

---

## Fase 10 — Transition, loading e Activity content execution

Objetivo: recuperar do `NewScripts` a camada de transição/loading que ficou sub-representada no roadmap anterior, sem empurrar apresentação visual para o core de scene composition.

F10 diferencia:

```text
Scene composition/release técnico = F6
Runtime placement = F9
Transition/loading policy = F10
Transition visual concreta = consumer/adapters futuros
```

| ID | Entrega | Detalhes |
|---|---|---|
| IF-FW-ROAD-10A | ADR: Scene Transition System | Definir `TransitionRequest`, `TransitionPolicy`, `TransitionResult`, progress e bloqueio de input durante transições. |
| IF-FW-ROAD-10B | Transition request/result baseline | Contratos para Route/Activity transition sem fade/loading visual obrigatório. |
| IF-FW-ROAD-10C | Loading progress contract | Progresso agregado de scene load, runtime materialization e readiness, sem depender de UI. |
| IF-FW-ROAD-10D | Transition input lock policy | Política mínima de bloqueio/liberação de input durante loading/fade/readiness; Input consumer completo fica em F12. |
| IF-FW-ROAD-10E | ADR: Activity content profile execution | Reposicionar `ActivityContentProfile` como conteúdo Activity-owned depois de RuntimeContent/Anchor. |
| IF-FW-ROAD-10F | Activity content execution baseline | Activity-owned scenes/prefabs planejados por profile, com plan/result/release e readiness. |
| IF-FW-ROAD-10G | ADR: Activity reset baseline | Definir reset como lifecycle operation separada de clear/reload; execução plena depende de participants/snapshot. |
| IF-FW-ROAD-10H | Transition + Activity content smoke | Transition policy -> content execution -> readiness -> release/reset boundary validado. |

### Não entra

- UI de loading final.
- Cinematic/cutscene player.
- Full input mode consumer.
- Save backend.
- Actor materialization.
- Pooling.

### Done

```text
Transition/loading deixa de ser lacuna.
Activity pode ter conteúdo próprio planejado/executado sem reabrir F4 ou F6.
Reset ganha fronteira formal sem copiar a pipeline monolítica do NewScripts.
```

---

## Fase 11 — Participation, capability runtime e local lifecycle participants

Objetivo: formalizar participação, inventário vivo de capabilities e participants locais antes de consumidores avançados.

Esta fase evita que Input, Actor, Save ou Camera recriem seus próprios registries paralelos.

| ID | Entrega | Detalhes |
|---|---|---|
| IF-FW-ROAD-11A | ADR: Participation Boundary | Definir `ParticipantId`, `PlayerSlot`, `ParticipationScope`, handoff e binding result sem PlayerActor concreto. |
| IF-FW-ROAD-11B | Participation baseline | Contratos mínimos de participação que Input/Actor/Camera/Save podem consumir depois. |
| IF-FW-ROAD-11C | ADR: Live Capability Inventory | Diferenciar `LocalContributionSet` diagnóstico de capability runtime viva. |
| IF-FW-ROAD-11D | Runtime capability reference | Referência tipada com owner scope, validity, stale/foreign rejection e release semantics. |
| IF-FW-ROAD-11E | ADR: Local lifecycle participants | Contratos locais para reset, release e snapshot sem backend concreto. |
| IF-FW-ROAD-11F | Local release/reset participant baseline | Participants locais participam de teardown/reset com ordering e exit freeze. |
| IF-FW-ROAD-11G | Consumer descriptor pattern | Consumers declaram requirements/contributions/validators sem capturar lifecycle core. |
| IF-FW-ROAD-11H | Capability/participation smoke | Discovery -> runtime reference -> stale rejection -> participant ordering validado. |

### Não entra

- PlayerActor concreto.
- Multiplayer/networking.
- Save backend.
- Camera/Audio/Actor implementations.
- Projectile/damage/attributes.

### Done

```text
Participation e capability runtime ficam disponíveis como fronteiras reutilizáveis.
Consumers deixam de precisar criar registries próprios.
```

---

## Fase 12 — Consumers intermediários: Input, Snapshot/Save e Pause

Objetivo: plugar consumers úteis e recorrentes usando F9/F10/F11, ainda sem gameplay pesado.

Ordem recomendada:

```text
Input ownership
Snapshot/Save contract
Pause as Content Anchor + Input + Activity consumer
Save progression/migration depois do envelope
```

| ID | Entrega | Módulo | Regra |
|---|---|---|---|
| IF-FW-ROAD-12A | ADR: Input ownership | Input | Sem action map string como chave funcional; transition input lock F10 é policy, não full consumer. |
| IF-FW-ROAD-12B | `InputModeContract` | Input | Route/Activity/Pause declaram requisito, consumer aplica e libera. |
| IF-FW-ROAD-12C | ADR: Snapshot envelope and schema | Save | Envelope typed: owner, participant, schema, version, payload. |
| IF-FW-ROAD-12D | `ISnapshotParticipant` / `SnapshotSet` | Save | Participants vêm de capability/local lifecycle boundaries. |
| IF-FW-ROAD-12E | Save slot/progression/migration ADR | Save | SaveSlot, manifest, current save pointer, checkpoint/auto/manual policy e migration. |
| IF-FW-ROAD-12F | ADR: Pause as consumer | Pause | Pause consome Content Anchor/Input/Activity/Transition; não possui Content Anchor. |
| IF-FW-ROAD-12G | `PauseContentAnchorConsumer` | Pause | Usa ContentAnchorBinding; não instancia diretamente em endpoint local. |
| IF-FW-ROAD-12H | Intermediate consumers smoke | Input mode, snapshot capture/restore contract e pause binding/release validado. |

### Done

```text
Input, Snapshot/Save e Pause consomem contextos e participants.
Nenhum deles descobre o mundo sozinho ou controla o lifecycle core.
```

---

## Fase 13 — Consumers avançados

Objetivo: plugar subsistemas que dependem de Content Anchor + Runtime + Contribution + Participation/Capability boundaries.

Ordem sugerida:

```text
Camera
Audio
Actor
Pooling
Scene Transition presentation adapter, se necessário
```

| ID | Entrega | Módulo | Regra |
|---|---|---|---|
| IF-FW-ROAD-13A | ADR: Camera as consumer | Camera | Camera consome Content Anchor/Participation; não define lifecycle core. |
| IF-FW-ROAD-13B | Camera consumer baseline | Camera | Sem `FrameworkCameraAuthority` static global; Cinemachine adapter opcional. |
| IF-FW-ROAD-13C | ADR: Audio as consumer | Audio | Audio recebe lifecycle context; não possui route/activity. |
| IF-FW-ROAD-13D | Audio lifecycle consumer | Audio | Port/adapter, sem global service locator; AudioListener único como Session persistent content futuro. |
| IF-FW-ROAD-13E | ADR: Actor runtime boundary | Actor | Actor é runtime content/contribution/participant, não lifecycle core. |
| IF-FW-ROAD-13F | Actor materialization baseline | Actor | Request/result/handle; contribui capabilities depois. |
| IF-FW-ROAD-13G | ADR: Pooling package boundary | Pooling | `com.immersive.pooling` é técnico, não projectile-first. |
| IF-FW-ROAD-13H | Pooled materializer | Pooling | Rent/return por `RuntimeContentHandle`; não substitui release policy. |
| IF-FW-ROAD-13I | Transition presentation adapter | Transition | Fade/loading screen/curtain como adapter, não como SceneLifecycle owner. |

### Done

```text
Consumers avançados entram sem statics globais e sem controlar lifecycle.
Scene transition visual entra como adapter/consumer, não como F6 técnico.
```

---

## Fase 14 — Gameplay capabilities

Objetivo: recursos de gameplay apoiados na base.

Ordem:

```text
Projectile
Impact/Damage
Attributes
Advanced Actor capabilities
Cinematics/Cutscene, se necessário
```

| ID | Entrega | Regra |
|---|---|
| IF-FW-ROAD-14A | Projectile as RuntimeSpawned | Pooled/Prefab runtime content; return/release explícito. |
| IF-FW-ROAD-14B | Impact as capability | Impact não controla projectile lifecycle diretamente. |
| IF-FW-ROAD-14C | Damage as actor capability | Damage aplica mutation por contrato. |
| IF-FW-ROAD-14D | Attributes as snapshot-capable capability | Attributes participam de SnapshotSet. |
| IF-FW-ROAD-14E | Cinematics/Cutscene as consumer | Cutscene é Activity/Transition/Timeline consumer, não lifecycle core. |

### Done

```text
Gameplay capabilities não dependem de pipeline monolítica.
Cada capability entra por contribution/consumer.
```

---

## Fase 15 — Productization, tooling e hardening

Objetivo: transformar a arquitetura reutilizável em artefato reutilizável para múltiplos jogos, sem misturar isso com o core inicial.

| ID | Entrega | Regra |
|---|---|
| FX1 | Settings Source Hardening | Formalizar/substituir `Resources.Load` por provider explícito sem fallback silencioso. |
| FX2 | Assembly / Build / Stripping Boundary Audit | Separar runtime core, Unity runtime, authoring, QA/dev tooling, editor e adapters opcionais. |
| FX3 | Documentation Hygiene | Remover ambiguidade de conceitos históricos/removidos como CameraFlow antigo. |
| FX4 | Framework Versioning & Migration | Package versioning, API compatibility, asset migration, snapshot migration. |
| FX5 | Pre-build Content Validation Pipeline | Validar anchors, required contributions, scenes, content profiles e runtime binding antes do build. |
| FX6 | Scoped Messaging Policy | Definir evento interno/session/route/activity/local e lifetime de mensagens. |
| FX7 | Editor Simulation / Visualizer | Visualizar Session -> Route -> Activity -> content sets -> runtime roots/handles. |
| FX8 | Asset Provider / Addressables / DLC Boundary | Provider local primeiro; Addressables/DLC/modding como adapters opcionais. |
| FX9 | Domain Reload / Hot Reload Resilience | Limpar/invalidar static state, registries e handles no Editor sem fallback silencioso. |
| FX10 | Telemetry / Analytics Hooks | Hooks opcionais tipados para tempo em Route, Activity completion, transition duration etc. |

### Fora do core atual

```text
Multiplayer/networking
Localization
Achievements/progression completo
Replay system
Accessibility layer
Remote config/experimentation
```

Esses itens podem virar consumers futuros, mas não alteram F8/F9/F10.

---

# Parte II — Mapa de dependências revisado

```text
F0A  Baseline ADRs
  ↓
F0B  Baseline hygiene
  ↓
F1   API status + Identity + Diagnostics
  ↓
F2   Session scope
  ↓
F3   Route baseline
  ↓
F4   Activity content/readiness
  ↓
F5   Local contribution
  ↓
F6   Route scene composition + release
  ↓
F7   Content Anchor declaration
  ↓
F8   Runtime roots/materialization contracts/release policy
  ↓
F9   Content Anchor binding/runtime placement
  ↓
F10  Transition, loading and Activity content execution
  ↓
F11  Participation, live capability inventory and local lifecycle participants
  ↓
F12  Input, Snapshot/Save and Pause
  ↓
F13  Advanced consumers: Camera, Audio, Actor, Pooling, transition presentation adapters
  ↓
F14  Gameplay capabilities
  ↓
F15/FX Productization, tooling and hardening
```

Paralelismo permitido:

```text
ADRs de F12/F13 podem ser escritos antes.
Código de F12/F13 não deve entrar antes de F9/F10/F11.
Hygiene editorial pode ocorrer junto com qualquer fase se não alterar runtime.
Productization FX pode ser planejado antes, mas código de hardening não deve interromper F8/F9.
```

---

# Parte III — Correções específicas sobre o roadmap original e o realinhamento F9+

## 1. CameraFlow

Original: decidir e executar em Fase 0.

Revisado:

```text
F0A decide.
F0B só remove/congela se a decisão for remover/congelar.
Se voltar, entra em F13 como Camera consumer/adapters opcionais.
```

## 2. RouteContentRuntime

Original: decidir e talvez conectar em Fase 0/Fase 2.

Revisado:

```text
F0A decide.
F0B remove/congela se deferido.
F3 conecta se ativo, porque F3 é a fase Route baseline.
```

## 3. ContentFlow / RuntimeContent

Original: `SessionContentSet`, identity, ownership e materializer avançam próximos.

Revisado:

```text
F1 primeiro define identity/status.
F2 define SessionContent.
F3/F4 estabilizam Route/Activity content.
F8 cria contratos de materialização, guardas de transição, contexto de escopo e política de ownership/release.
Adapters físicos concretos ficam fora do core ou entram como consumers/adapters explícitos.
```

## 4. Additive scene support

Original: aparece em Runtime roots/release.

Revisado:

```text
F6, junto de RouteSceneCompositionPlan/Result.
```

## 5. Content Anchor

Original: Content Anchor baseline depois de Runtime roots.

Revisado:

```text
F7 declara Content Anchor sem runtime binding.
F9 conecta Content Anchor com RuntimeContent.
```

## 6. LocalContribution

Original: já remove global `FindObjectsByType` como done da Fase 3.

Revisado:

```text
Só remover do fluxo principal depois de ActivityContentSet/RouteContentSet definirem escopo de discovery.
```

## 7. Transition/loading

Original: ficou sub-representado porque foi tratado como presentation.

Revisado:

```text
F6 cobre scene composition/release técnico.
F10 cobre transition/loading policy, progress, input lock e Activity content execution.
F13 pode adicionar presentation adapters visuais como fade/loading screen/curtain.
```

## 8. Activity content profile e reset

Original: ActivityContentProfile ficou deferred após F4/F6 sem fase concreta; reset ficou como gap futuro.

Revisado:

```text
F10 reintroduz ActivityContentProfile execution e Activity reset baseline depois de RuntimeContent/Anchor foundations.
Reset completo depende de F11 participants e F12 snapshot.
```

## 9. Participation/capability runtime

Original: Player participation, capability inventory vivo e local participants ficaram espalhados entre F10/F11/F12.

Revisado:

```text
F11 cria Participation Boundary, Live Capability Inventory e Local Lifecycle Participants antes de Input, Save, Pause, Actor e Camera consumirem esses dados.
```

---

# Parte IV — Backlog de ADRs revisado

ADR files follow the plan order first and the stable ADR id second.

| Ordem no Plano | ADR | Tema |
|---|---|---|
| F0A-01 | ADR-BL-001 | Baseline Reconciliation: CameraFlow, RouteContentRuntime, ContentFlow, RouteContentProfile, QA Canvas, ValidationMode. |
| F0A-02 | ADR-BL-002 | Core vs Consumers. |
| F0A-03 | ADR-BL-003 | Public API Status Policy. |
| F0A-04 | ADR-BL-004 | QA/Diagnostics Boundary. |
| F0A-05 | ADR-BL-005 | Dependency Policy. |
| F1A-01 | ADR-ID-001 | Typed Identity Policy. |
| F1A-02 | ADR-DIAG-001 | FrameworkFact vs human log. |
| F1A-03 | ADR-CONTENT-001 | Content identity domain. |
| F2-01 | ADR-SESSION-001 | Session Scope and owner. |
| F2-02 | ADR-SESSION-002 | SessionContent ownership semantics. |
| F2-03 | ADR-SETTINGS-001 | Settings source policy. |
| F3-01 | ADR-ROUTE-001 | RouteRuntimeState and RouteContentRuntime status. |
| F3-02 | ADR-ROUTE-002 | RouteContentSet semantics. |
| F4-01 | ADR-ACTIVITY-001 | ActivityContentSet and Readiness baseline. |
| F5-01 | ADR-LOCAL-001 | Local identity. |
| F5-02 | ADR-LOCAL-002 | Local contribution discovery and requiredness. |
| F6-01 | ADR-RELEASE-001 | Content release plan by scope. |
| F6-02 | ADR-SCENE-001 | Route scene composition plan/result. |
| F7-01 | ADR-ANCHOR-001 | Content Anchor as placement contract. |
| F8-01 | ADR-RUNTIME-001 | Runtime ownership and roots. |
| F8-02 | ADR-RUNTIME-002 | Materialization request/result/handle/guardrails. |
| F9-01 | ADR-ANCHOR-002 | Content Anchor binding and runtime placement. |
| F10-01 | ADR-TRANSITION-001 | Scene transition/loading policy. |
| F10-02 | ADR-ACTIVITY-002 | Activity content profile execution. |
| F10-03 | ADR-ACTIVITY-003 | Activity reset plan baseline. |
| F11-01 | ADR-PARTICIPATION-001 | Participation boundary. |
| F11-02 | ADR-CAPABILITY-001 | Live capability inventory and runtime references. |
| F11-03 | ADR-LOCAL-003 | Local lifecycle participants: reset/release/snapshot. |
| F12-01 | ADR-INPUT-001 | Input ownership. |
| F12-02 | ADR-SAVE-001 | Snapshot envelope and schema. |
| F12-03 | ADR-SAVE-002 | Save slot/progression/migration policy. |
| F12-04 | ADR-PAUSE-001 | Pause as Content Anchor/Input/Activity/Transition consumer. |
| F13-01 | ADR-CAMERA-001 | Camera as Content Anchor/Participation consumer. |
| F13-02 | ADR-AUDIO-001 | Audio as lifecycle consumer. |
| F13-03 | ADR-ACTOR-001 | Actor runtime boundary. |
| F13-04 | ADR-POOL-001 | Pooling package boundary. |
| F13-05 | ADR-TRANSITION-002 | Transition presentation adapters. |
| F14-01 | ADR-GAMEPLAY-001 | Projectile as RuntimeSpawned. |
| F14-02 | ADR-GAMEPLAY-002 | Damage/Attributes as capabilities. |
| F15/FX | ADR-PRODUCT-001+ | Settings, assembly/build, versioning, tooling, asset provider, domain reload, telemetry. |

---

# Parte V — Estado atual e próximo passo recomendado

## Estado atual consolidado

```text
F0 — CLOSED / PASS
F1 — CLOSED / PASS
F2 — CLOSED / PASS
F3 — CLOSED / PASS
F4 — CLOSED / ACTIVITY BASELINE PASS
F5 — CLOSED / LOCAL CONTRIBUTION FOUNDATION PASS
F6 — CLOSED / ROUTE SCENE COMPOSITION + RELEASE BASELINE PASS
F7 — CLOSED / CONTENT ANCHOR DECLARATION BASELINE PASS
F8 — CLOSED / RUNTIME CONTENT SMOKE PASS
F9 — OPEN / F9B LOGICAL CONTENT ANCHOR BINDING
```

No package atual, F8I existe como boundary de adapter (`IRuntimeMaterializationAdapter`) sem implementação física. F8J adiciona release lógico (`RuntimeReleaseRequest/Result/Policy/Status`) e `IRuntimeReleaseAdapter`, também sem implementação física. F8K adiciona o handoff explícito `ApplyMaterializationResult` e o Runtime Content Smoke. F9A adiciona `ContentAnchorBindingRequest`, `ContentAnchorBindingResult`, `ContentAnchorBindingStatus` e `ContentAnchorContentHandle`. F9B adiciona `RuntimeContentAnchorBinding` lógico.

## Ação imediata

```text
Validar F9B por compile/import smoke. Não há Play Mode behavior físico novo neste corte.
```

Depois de F9B compilar limpo, o próximo corte autorizado é o smoke/diagnóstico de binding lógico, ainda sem placement físico:

```text
F9C — Content Anchor binding smoke / lifecycle diagnostics
```

## Não avançar ainda

```text
Physical Content Anchor placement
Transition/loading runtime
ActivityContentProfile execution
Participation boundary
Input/Save/Pause consumers
Camera/Audio/Actor/Pooling
Gameplay capabilities
```

Esses cortes foram reposicionados no novo F9+ e dependem do fechamento de F8.

---

# Parte VI — Productization backlog

O backlog FX não bloqueia F8/F9, mas passa a ser rastreado como parte da maturidade multi-jogo do framework.

| ID | Tema | Momento recomendado |
|---|---|---|
| FX1 | Settings Source Hardening | Após F9/F10 ou quando houver fricção real de distribuição. |
| FX2 | Assembly / Build / Stripping Boundary Audit | Após F8/F9 estabilizarem fronteiras reais. |
| FX3 | Documentation Hygiene | Pode ocorrer junto de qualquer fase documental. |
| FX4 | Framework Versioning & Migration | Após F12 Snapshot/Save. |
| FX5 | Pre-build Content Validation Pipeline | Após F9/F10/F11, quando anchors/bindings/capabilities existirem. |
| FX6 | Scoped Messaging Policy | Antes de F13 consumers avançados, se event lifetime ficar ambíguo. |
| FX7 | Editor Simulation / Visualizer | Após F8/F9 para visualizar roots/handles/bindings. |
| FX8 | Asset Provider / Addressables / DLC Boundary | Após adapter boundary e release policy estarem fechados. |
| FX9 | Domain Reload / Hot Reload Resilience | Após F8, quando runtime handles/roots existirem. |
| FX10 | Telemetry / Analytics Hooks | Após transition/activity lifecycle ficarem estáveis. |
