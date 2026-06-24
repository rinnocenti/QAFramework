# F8 — Runtime Roots and Materialization Audit

Status: `F8K PASS / F8 CLOSED`

F8 começa depois de F7 fechar o baseline de Content Anchor. F7 entregou identidade, declaração, authoring, discovery, diagnostics smoke e authoring validation para `RouteContentAnchor`, mas não criou runtime placement nem materialização.

F8 define a base de conteúdo criado em runtime: roots por escopo, handles, requests/results de materialização e release policy. F8 ainda não conecta isso a Content Anchors; essa conexão pertence à F9.

---

## Estado de entrada

Já existe:

- `RouteSceneCompositionPlan` / `RouteSceneCompositionResult`;
- carregamento de Primary Scene por `Single`;
- carregamento additive de additional Route scenes;
- `ContentReleasePlan` / `ContentReleaseResult`;
- unload físico de additional scenes owned;
- `ContentAnchorId`, `ContentAnchorDeclaration`, `RouteContentAnchor`, `ContentAnchorSet`;
- discovery e validation de `RouteContentAnchor` nas cenas carregadas.

Já existe após F8B:

- `RuntimeContentScope`;
- `RuntimeContentState`;
- `RuntimeContentId`;
- `RuntimeContentOwner`;
- `RuntimeContentIdentity`.

Já existe após F8C–F8K:

- `RuntimeContentHandle`;
- `RuntimeScopeRoot`;
- `RuntimeRootRegistry`;
- `RuntimeContentRuntime`;
- `RuntimeScopeContext`;
- `RuntimeScopeLifecycleResult`;
- `RuntimeMaterializationResource`;
- `RuntimeMaterializationRequest`;
- `RuntimeMaterializationResult`;
- `RuntimeMaterializationStatus`;
- `RuntimeScopeTransitionState`;
- `RuntimeScopeCancellationToken`;
- internal transition guard/result/status;
- `IRuntimeMaterializationAdapter` como boundary de adapter físico externo ao core.

Ainda não existe:

- implementação de adapter físico;
- F8 closure smoke;
- binding entre Content Anchor e runtime content;
- Activity Content Anchor;
- Actor/Pause/Camera/UI/Pool consumers.

---

## Problema que F8 resolve

Até F7, o framework sabe carregar cenas e localizar pontos autorais. Ele ainda não tem uma forma canônica de responder:

```text
Quem é dono de um GameObject criado em runtime?
Onde esse objeto deve ficar na hierarquia?
Quando ele deve ser liberado?
Como o framework diagnostica double-release, orphan ou stale handle?
```

F8 deve resolver ownership/runtime lifetime antes de qualquer consumer criar conteúdo dinâmico.

---

## Decisão principal

F8 deve separar três conceitos:

| Conceito | Papel |
|---|---|
| `Content Anchor` | Ponto autoral/passivo dentro de cena carregada. Não cria objetos. |
| `Runtime Root` | Fronteira lógica de ownership por escopo/lifecycle. Não é hierarchy root físico em F8. |
| `Runtime Content Handle` | Referência canônica e liberável para uma instância criada em runtime. |

Essa separação evita que `ContentAnchor` vire root global, service locator ou spawn system.

---

## Runtime scopes autorizados

F8 trabalha com estes scopes:

```text
Session
Route
Activity
Transient
```

Semântica inicial:

| Scope | Lifetime esperado |
|---|---|
| `Session` | Vive enquanto a sessão/aplicação runtime vive. Não é destruído por troca de Route. |
| `Route` | Vive enquanto a Route está ativa. É liberado no Route exit. |
| `Activity` | Vive enquanto a Activity está ativa. É liberado no Activity clear/switch/exit futuro. |
| `Transient` | Curto prazo; release explícito pelo owner/request. Não deve virar fallback silencioso. |

`Application` não entra como root de conteúdo neste baseline. O Application Runtime já é owner de boot/flow, não root de gameplay content.

---

## Runtime Root

Em F8, `Runtime Root` é um registro lógico de ownership por escopo, representado por `RuntimeScopeRoot` e controlado por `RuntimeContentRuntime`.

Ele não é `GameObject`, `Transform`, parent de hierarchy ou container físico. Hierarchy root físico, prefab instance, scene object, pool instance ou Addressables handle pertencem a adapters/consumers futuros.

Regras:

- não usar `GameObject.Find`;
- não usar singleton/service locator global;
- não usar nome de GameObject como identidade funcional;
- não destruir conteúdo authored de cena;
- não assumir Content Anchor;
- não criar Actor/Pause/Camera/UI por conta própria.

O root lógico carrega owner scope e handles suficientes para diagnostics e release lógico.

---

## Runtime Root Registry

O registry de roots deve ser interno/scoped. Ele resolve roots por scope/owner dentro do runtime atual.

Ele não deve ser API pública global. Consumers futuros devem receber requests/resultados ou depender de APIs explícitas do framework, não buscar o registry diretamente.

---

## Runtime Content Handle

`RuntimeContentHandle` será a unidade de ownership de uma instância runtime.

Deve conter, no mínimo:

```text
identity
owner scope
state
resource name/path diagnostic only
release policy/action
release diagnostics
```

O handle deve ser seguro contra double-release e stale usage. Double-release não deve destruir duas vezes; deve ser diagnosticado.

---

## Materialization

Materialização deve ser explícita:

```text
RuntimeMaterializationRequest
→ IRuntimeMaterializationAdapter.Materialize
→ RuntimeMaterializationResult
→ RuntimeContentHandle
```

O framework core não deve introduzir um materializer físico inicial. O core deve parar em contratos, ownership, guardas e release lógico; prefab, cena, Addressables ou pooling devem ser adapters explícitos fora do core.

F8 não deve materializar Actor, Pause, Camera, UI ou pooled objects.

---

## Release

F8 precisa de release policy para runtime-created content, separada do release de cenas da F6.

Diferença:

| Release | Dono |
|---|---|
| F6 scene release | `ContentReleasePlan` descarrega additional Route scenes owned. |
| F8 runtime release | `RuntimeContentHandle` libera instâncias criadas em runtime. |

F8J define release lógico por handle/escopo dentro do core. Destruir GameObject, devolver ao pool, descarregar Addressables ou liberar cenas são responsabilidades de adapters explícitos fora do core ou de trilhos já existentes, como F6 para cenas.

---

## Relação com Content Anchor

F8 não conecta materialization a Content Anchor.

F8 entrega:

```text
Criar conteúdo runtime com owner/lifetime/release claro.
```

F9 entrega:

```text
Criar/posicionar conteúdo runtime usando ContentAnchorRoot/Slot/Point.
```

---

## Fora do escopo de F8

- `RuntimeContentAnchorBinding`;
- binding request/result por anchor;
- Activity Content Anchor;
- Pause overlay root;
- Camera anchor consumer;
- Actor materialization;
- pooling rent/return;
- save/snapshot;
- gameplay-specific lifecycle;
- automatic fallback root quando root obrigatório estiver ausente.

---

## Plano incremental recomendado

| Corte | Objetivo |
|---|---|
| `F8A` | ADR/detail audit de runtime roots/materialization. |
| `F8B` | Primitivas de runtime ownership/scope/state. `APPLIED` |
| `F8C` | `RuntimeContentHandle` passivo e release state. `APPLIED` |
| `F8D` | `RuntimeScopeRoot` + registry interno mínimo. `APPLIED` |
| `F8E` | `RuntimeContentRuntime` + `RuntimeScopeContext`. `APPLIED` |
| `F8F` | Integração do runtime owner/context aos lifecycles de Session/Route/Activity. `APPLIED` |
| `F8G` | `RuntimeMaterializationRequest` / `RuntimeMaterializationResult`. `APPLIED` |
| `F8H` | Transition guard + scoped cancellation. `APPLIED` |
| `F8I` | `IRuntimeMaterializationAdapter` boundary. `CLOSED / COMPILE-SMOKE PASS` |
| `F8J` | Runtime release request/result/policy, release adapter boundary and logical release by handle/scope. `CLOSED / COMPILE-SMOKE PASS` |
| `F8K` | Runtime Content Smoke, `ApplyMaterializationResult` registry handoff and F8 closure gate. `PASS / F8 CLOSED` |

F8 foi realinhada após F8D para inserir `RuntimeContentRuntime` e `RuntimeScopeContext` antes de request/result de materialização. A ordem nova evita criar request pública sem owner interno explícito para roots e handles. Nenhum consumer deve entrar antes de handle/root/context/release mínimos.

F8F aplica a primeira integração real com lifecycle: `FrameworkRuntimeHost` cria o root lógico de Session; `RouteLifecycleRuntime` cria/remove roots lógicos de Route; `ActivityFlowRuntime` cria/remove roots lógicos de Activity. A integração produz diagnostics via `RuntimeScopeLifecycleResult`, mas ainda não executa materialização física, não cria hierarchy root e não executa release físico.

F8G aplica o contrato explícito de materialização: `RuntimeMaterializationRequest`, `RuntimeMaterializationResult`, `RuntimeMaterializationResource` e `RuntimeMaterializationStatus`. O request usa `RuntimeScopeContext + RuntimeContentId + RuntimeMaterializationResource`; o result reporta status/handle/mensagem. F8H adiciona transition guard e `RuntimeScopeCancellationToken`, impedindo request novo quando o owner scope está cancelando/removido e permitindo validar token stale antes de qualquer adapter externo. F8I adiciona `IRuntimeMaterializationAdapter` como boundary. F8J adiciona `RuntimeReleaseRequest`, `RuntimeReleaseResult`, `RuntimeReleasePolicy`, `RuntimeReleaseStatus`, `IRuntimeReleaseAdapter` e release lógico por handle/scope. F8K adiciona `RuntimeContentRuntime.ApplyMaterializationResult` e o Runtime Content Smoke para validar request/guard/apply/release/unregister/root removal; ainda não há adapter físico no core, `Instantiate`, `Destroy`, scene unload, pool return, Addressables release ou Content Anchor binding.

---

## Critérios de fechamento de F8

F8 só fecha quando houver smoke demonstrando:

```text
request criado com owner/context corretos
`ApplyMaterializationResult` valida guard e registra handle materializado
guard aceita escopo ativo e rejeita escopo stale/cancelado
release lógico marca handle como Released e unregister quando solicitado
root lógico pode ser removido depois do unregister
root de owner correto
sem GameObject.Find
sem Instantiate/Destroy no core
sem fallback silencioso
```
