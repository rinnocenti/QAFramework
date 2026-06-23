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
≠ Prefab materialization
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

- Prefab materializer.
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
Next — F8I / PrefabContentMaterializer
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
| IF-FW-ROAD-8F | Lifecycle integration for runtime roots | `APPLIED`: Session/Route/Activity criam/removem roots lógicos e contextos explicitamente no lifecycle, sem materializar prefabs. |
| IF-FW-ROAD-8G | `RuntimeMaterializationRequest` / `Result` | `APPLIED`: Request/result/resource/status explícitos depois do owner/context/lifecycle roots; sem materializer concreto. |
| IF-FW-ROAD-8H | Transition guard + scoped cancellation | `APPLIED`: Guardas para transição e cancelamento por escopo antes do materializer concreto. |
| IF-FW-ROAD-8I | `PrefabContentMaterializer` | `NEXT`: Primeiro materializer concreto e local. |
| IF-FW-ROAD-8J | `RuntimeReleasePolicy` / release execution | Activity exit futuro, Route exit e Session shutdown. |
| IF-FW-ROAD-8K | Runtime materialization smoke / F8 closure | Prefab → handle → release on exit → zero orphan. |

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

## Fase 9 — Content Anchor binding e content placement

Objetivo: conectar Content Anchor declaration com runtime materialization.

| ID | Entrega | Detalhes |
|---|---|---|
| IF-FW-ROAD-9A | `ContentAnchorBindingRequest` | Solicita root/slot/anchor por identity. |
| IF-FW-ROAD-9B | `ContentAnchorBindingResult` | Resultado com diagnostics e handle. |
| IF-FW-ROAD-9C | `ContentAnchorContentHandle` | Binding + runtime content + release. |
| IF-FW-ROAD-9D | `RuntimeContentAnchorBinding` | Materializa prefab em ContentAnchorSlot/ContentAnchorRoot. |
| IF-FW-ROAD-9E | Binding release order | Content Anchor binding libera antes de content/root exit. |
| IF-FW-ROAD-9F | Content Anchor binding smoke | Prefab em slot → Activity exit → binding released → content released. |

### Não entra

- Pause overlay.
- Camera rig.
- Actor presentation.
- Pooling.

### Done

```text
Content Anchor pode receber conteúdo runtime sem virar pause/camera.
```

---

## Fase 10 — Consumers intermediários

Objetivo: consumidores úteis, mas ainda sem gameplay pesado.

Ordem recomendada:

```text
Input
Snapshot/Save contract
Pause as Content Anchor consumer
```

| ID | Entrega | Módulo | Regra |
|---|---|---|---|
| IF-FW-ROAD-10A | ADR: Input ownership | Input | Sem action map string como chave funcional. |
| IF-FW-ROAD-10B | `InputModeContract` | Input | Activity/Route declaram requisito, consumer aplica. |
| IF-FW-ROAD-10C | ADR: Snapshot model | Save | Envelope typed: owner, schema, version, payload. |
| IF-FW-ROAD-10D | `ISnapshotParticipant` | Save | Capability-level, sem backend concreto. |
| IF-FW-ROAD-10E | `SnapshotSet` | Save | Captura/restaura participants descobertos. |
| IF-FW-ROAD-10F | ADR: Pause as consumer | Pause | Pause consome Content Anchor/Input/Activity; não possui content anchor. |
| IF-FW-ROAD-10G | `PauseContentAnchorConsumer` | Pause | Usa ContentAnchorBinding; não instancia diretamente em endpoint local. |

### Done

```text
Input, Snapshot e Pause consomem contextos.
Nenhum deles descobre o mundo sozinho.
```

---

## Fase 11 — Consumers avançados

Objetivo: plugar subsistemas que dependem de Content Anchor + Runtime + Contribution.

Ordem sugerida:

```text
Camera
Audio
Actor
Pooling
```

| ID | Entrega | Módulo | Regra |
|---|---|---|---|
| IF-FW-ROAD-11A | ADR: Camera as consumer | Camera | Camera consome Content Anchor/Anchor; não define lifecycle core. |
| IF-FW-ROAD-11B | Camera consumer baseline | Camera | Sem `FrameworkCameraAuthority` static global. |
| IF-FW-ROAD-11C | ADR: Audio as consumer | Audio | Audio recebe lifecycle context; não possui route/activity. |
| IF-FW-ROAD-11D | Audio lifecycle consumer | Audio | Port/adapter, sem global service locator. |
| IF-FW-ROAD-11E | ADR: Actor runtime boundary | Actor | Actor é runtime content/contribution, não lifecycle core. |
| IF-FW-ROAD-11F | Actor materialization baseline | Actor | Request/result/handle; contribui capabilities depois. |
| IF-FW-ROAD-11G | ADR: Pooling package boundary | Pooling | `com.immersive.pooling` é técnico, não projectile-first. |
| IF-FW-ROAD-11H | Pooled materializer | Pooling | Rent/return por `RuntimeContentHandle`. |

### Done

```text
Consumers avançados entram sem statics globais e sem controlar lifecycle.
```

---

## Fase 12 — Gameplay capabilities

Objetivo: recursos de gameplay apoiados na base.

Ordem:

```text
Projectile
Impact/Damage
Attributes
Advanced Actor capabilities
```

| ID | Entrega | Regra |
|---|---|
| IF-FW-ROAD-12A | Projectile as RuntimeSpawned | Pooled/Prefab runtime content; return/release explícito. |
| IF-FW-ROAD-12B | Impact as capability | Impact não controla projectile lifecycle diretamente. |
| IF-FW-ROAD-12C | Damage as actor capability | Damage aplica mutation por contrato. |
| IF-FW-ROAD-12D | Attributes as snapshot-capable capability | Attributes participam de SnapshotSet. |

### Done

```text
Gameplay capabilities não dependem de pipeline monolítica.
Cada capability entra por contribution/consumer.
```

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
F8   Runtime roots/materialization
  ↓
F9   Content Anchor binding/runtime placement
  ↓
F10  Consumers intermediários
  ↓
F11  Consumers avançados
  ↓
F12  Gameplay capabilities
```

Paralelismo permitido:

```text
ADRs de F10/F11 podem ser escritos antes.
Código de F10/F11 não deve entrar antes de F7/F8/F9.
Hygiene editorial pode ocorrer junto com qualquer fase se não alterar runtime.
```

---

# Parte III — Correções específicas sobre o roadmap original

## 1. CameraFlow

Original: decidir e executar em Fase 0.

Revisado:

```text
F0A decide.
F0B só remove/congela se a decisão for remover/congelar.
Se for manter ativo, não expandir até F11.
```

## 2. RouteContentRuntime

Original: decidir e talvez conectar em Fase 0/Fase 2.

Revisado:

```text
F0A decide.
F0B remove/congela se deferido.
F3 conecta se ativo, porque F3 é a fase Route baseline.
```

## 3. ContentFlow

Original: `SessionContentSet`, identity, ownership e materializer avançam próximos.

Revisado:

```text
F1 primeiro define identity/status.
F2 define SessionContent.
F3/F4 estabilizam Route/Activity content.
F8 só então cria materializer concreto.
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
| F6-01 | ADR-RELEASE-001 | Content release plan by scope — Accepted in F6A; implementation starts at F6F. |
| F6-02 | ADR-SCENE-001 | Route scene composition plan/result — Accepted in F6A; implementation starts at F6B. |
| F7-01 | ADR-ANCHOR-001 | Content Anchor as placement contract. |
| F8-01 | ADR-RUNTIME-001 | Runtime ownership and roots. |
| F8-02 | ADR-RUNTIME-002 | Materialization request/result/handle. |
| F9-01 | ADR-ANCHOR-002 | Content Anchor binding and runtime placement. |
| F10-01 | ADR-INPUT-001 | Input ownership. |
| F10-02 | ADR-PAUSE-001 | Pause as Content Anchor/Input/Activity consumer. |
| F10-03 | ADR-SAVE-001 | Snapshot envelope and schema. |
| F11-01 | ADR-ACTOR-001 | Actor runtime boundary. |
| F11-02 | ADR-AUDIO-001 | Audio as lifecycle consumer. |
| F11-03 | ADR-CAMERA-001 | Camera as Content Anchor consumer. |
| F11-04 | ADR-POOL-001 | Pooling package boundary. |

---

# Parte V — Próximo passo recomendado

## Estado atual

```text
F0 — CLOSED / PASS
F1 — CLOSED / PASS
F2 — CLOSED / PASS
F3 — CLOSED / PASS
F4 — CLOSED / ACTIVITY BASELINE PASS
F5A — CLOSED / ADR ACCEPTED
F5B — CLOSED / STANDARD COMPILE-SMOKE PASS
F5  — CLOSED / LOCAL CONTRIBUTION FOUNDATION PASS
F6A — CLOSED / ADR ACCEPTED / DOCS ONLY
F6B — CLOSED / ROUTE SCENE COMPOSITION PLAN PASS
F6C — CLOSED / ROUTE SCENE COMPOSITION RESULT PASS
F6D — CLOSED / SCENE LIFECYCLE ADDITIVE PRIMITIVE PASS
F6E — CLOSED / ROUTE CONTENT PROFILE EXECUTION PASS
F6F — CLOSED / CONTENT RELEASE PLAN/RESULT PASS
F6G — CLOSED / SCENE RELEASE EXECUTION PASS
F6  — CLOSED / ROUTE SCENE COMPOSITION + RELEASE BASELINE PASS
```

ADR status relevante para a fronteira atual:

```text
F5-01 — ADR-LOCAL-001 — Local Identity — Accepted
F5-02 — ADR-LOCAL-002 — Local Contribution Discovery and Requiredness — Applied through F5H / Expected Declarations Deferred
F6-01 — ADR-RELEASE-001 — Content Release Plan by Scope — Accepted / F6G release smoke pass
F6-02 — ADR-SCENE-001 — Route Scene Composition Plan and Result — Accepted / F6 scene composition and release baseline pass
```

## Ação imediata

F6 está fechado:

```text
F6E — RouteContentProfileAsset execution [CLOSED / PROFILE SMOKE PASS]
F6F — ContentReleasePlan / ContentReleaseResult [CLOSED / PASS]
F6G — Scene release execution / release smoke [CLOSED / RELEASE SMOKE PASS]
```

Escopo fechado:

```text
- RouteContentProfileAsset executa additional scenes via RouteSceneCompositionPlan/Result;
- ContentReleasePlan/Result representa release por escopo;
- ContentReleaseRuntime descarrega cenas additive owned;
- Primary Scene continua controlada por LoadSceneMode.Single;
- Route Release Smoke valida routeReleaseReleased='1' e restore com routeSceneLoaded='2'.
```

Próximo passo autorizado:

```text
F7A — Content Anchor ADR/detail audit
```

F7 deve começar por auditoria/ADR de Content Anchor declaration, sem RuntimeRoot/materialization ou consumers avançados.

## Não avançar ainda

```text
Expected contribution declarations
Materialização canônica
Content Anchor
Runtime roots/materialization
Input/Camera/Actor/Save/Pooling
```

Esses cortes dependem da identidade local tipada e das fases intermediárias do roadmap.


## F7D–F7H — Route Content Anchor declaration/discovery/validation

Status: `F7H APPLIED / PENDING COMPILE-SMOKE`.

F7D added `RouteContentAnchor` as the first passive Route-scoped public authoring component.

F7E added `ContentAnchorSet` as a passive scoped collection for unique declarations and local issues.

F7F added loaded Route Content Anchor discovery into a diagnostic route-local `ContentAnchorSet`.

F7G added the dedicated Content Anchor diagnostics smoke and trimmed QA Canvas to the current validation path.

F7H adds authoring validation for loaded `RouteContentAnchor` components: missing Route, missing Anchor Id, `Kind = Unknown`, invalid Requiredness, scene/Route declaration mismatch and duplicate Content Anchor identity/id. It does not enforce Required anchors in lifecycle and does not add Activity anchors, runtime binding, placement, RuntimeRoot/materialization or consumers.

Next: `F7I — F7 closure`.
