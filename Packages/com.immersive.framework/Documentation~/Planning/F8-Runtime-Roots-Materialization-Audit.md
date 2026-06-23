# F8 — Runtime Roots and Materialization Audit

Status: `F8D1 APPLIED / PLAN REALIGNED`

F8 começa depois de F7 fechar o baseline de Content Anchor. F7 entregou identidade, declaração, authoring, discovery, diagnostics smoke e authoring validation para `RouteContentAnchor`, mas não criou runtime placement nem materialização.

F8 define a base de conteúdo criado em runtime: roots por escopo, handles, contexto runtime, requests/results de materialização, prefab materializer e release policy. F8 ainda não conecta isso a Content Anchors; essa conexão pertence à F9.

`F8D1` realinha a sequência da fase: depois de `RuntimeScopeRoot` + `RuntimeRootRegistry`, o próximo corte não é mais request/result. O próximo corte é `RuntimeContentRuntime` + `RuntimeScopeContext`, porque o registry precisa de um owner runtime interno antes de qualquer materializer.

---

## Estado de entrada

Já existe antes de F8:

- `RouteSceneCompositionPlan` / `RouteSceneCompositionResult`;
- carregamento de Primary Scene por `Single`;
- carregamento additive de additional Route scenes;
- `ContentReleasePlan` / `ContentReleaseResult`;
- unload físico de additional scenes owned;
- `ContentAnchorId`, `ContentAnchorDeclaration`, `RouteContentAnchor`, `ContentAnchorSet`;
- discovery e validation de `RouteContentAnchor` nas cenas carregadas.

Já existe após F8D:

- `RuntimeContentScope`;
- `RuntimeContentState`;
- `RuntimeContentId`;
- `RuntimeContentOwner`;
- `RuntimeContentIdentity`;
- `RuntimeContentHandle`;
- `RuntimeContentHandleTransitionStatus`;
- `RuntimeContentHandleTransitionResult`;
- `RuntimeScopeRoot`;
- `RuntimeRootRegistry`;
- `RuntimeRootRegistryOperationStatus`;
- `RuntimeRootRegistryOperationResult`.

Ainda não existe:

- `RuntimeContentRuntime` como owner interno do registry;
- `RuntimeScopeContext` para passar contexto sem provider global;
- root GameObject/Transform controlado pelo runtime;
- integração de root creation/release com Route/Activity lifecycle;
- materialization request/result;
- prefab materializer;
- transition guard/cancellation scoped para runtime content;
- runtime content release execution;
- binding entre Content Anchor e runtime content;
- Activity Content Anchor;
- Actor/Pause/Camera/UI/Pool consumers.

---

## Problema que F8 resolve

Até F7, o framework sabe carregar cenas, liberar cenas additive owned e localizar pontos autorais. Ele ainda não tem uma forma canônica de responder:

```text
Quem é dono de um GameObject criado em runtime?
Onde esse objeto fica enquanto o escopo vive?
Quem registra e libera esse objeto?
O que acontece se Activity/Route sair durante uma operação runtime?
Como o framework diagnostica double-release, orphan ou stale handle?
```

F8 resolve ownership/runtime lifetime antes de qualquer consumer criar conteúdo dinâmico.

---

## Decisão principal

F8 separa três conceitos:

| Conceito | Papel |
|---|---|
| `Content Anchor` | Ponto autoral/passivo dentro de cena carregada. Não cria objetos. |
| `Runtime Root` | Container runtime por escopo/lifecycle. Recebe objetos criados em runtime. |
| `RuntimeContentHandle` | Referência canônica e liberável para uma instância criada em runtime. |

Essa separação evita que `ContentAnchor` vire root global, service locator ou spawn system.

`F8D1` adiciona uma quarta decisão operacional:

```text
Runtime Root Registry precisa de um owner runtime interno antes de materialization request/result.
```

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
| `Activity` | Vive enquanto a Activity está ativa. É liberado no Activity clear/switch/exit. |
| `Transient` | Curto prazo; release explícito pelo owner/request. Não deve virar fallback silencioso. |

`Application` não entra como root de conteúdo neste baseline. O Application Runtime já é owner de boot/flow, não root de gameplay content.

---

## Runtime Root

`Runtime Root` é o container runtime de um escopo. Em F8D ele é apenas lógico/passivo. Em cortes posteriores, ele pode ganhar representação Unity interna controlada para parent de prefabs.

Regras:

- não usar `GameObject.Find`;
- não usar singleton/service locator global;
- não usar nome de GameObject como identidade funcional;
- não destruir conteúdo authored de cena;
- não assumir Content Anchor;
- não criar Actor/Pause/Camera/UI por conta própria;
- não criar fallback root quando root obrigatório estiver ausente.

O root deve carregar identidade, owner scope e estado suficiente para diagnostics.

---

## RuntimeContentRuntime

`RuntimeContentRuntime` é o próximo owner interno autorizado para F8E.

Responsabilidades esperadas:

- manter o `RuntimeRootRegistry` da sessão runtime atual;
- criar roots explicitamente por `RuntimeContentOwner`;
- fornecer operações internas controladas para registro, materialização e release futuros;
- preparar `RuntimeScopeContext` sem expor provider global;
- continuar sem instanciar prefab no F8E;
- continuar sem destruir objeto no F8E;
- continuar sem Content Anchor binding.

Não é API pública de gameplay e não deve virar service locator.

---

## RuntimeScopeContext

F8 não deve criar um provider global como forma de materializer consultar Route/Activity atual.

A regra aceita é:

```text
Contexto entra no request/operação.
Materializer não consulta estado global.
```

`RuntimeScopeContext` deve transportar owner/scope/source/reason de forma explícita, criado pelo lifecycle owner que já conhece o escopo ativo.

---

## Runtime Root Registry

O registry de roots é interno/scoped. Ele resolve roots por scope/owner dentro do runtime atual.

Ele não deve ser API pública global. Consumers futuros devem receber requests/resultados ou depender de APIs explícitas do framework, não buscar o registry diretamente.

F8 não cria registry paralelo por Activity. Activity e Route integram-se por owner/scope no registry canônico.

---

## Runtime Content Handle

`RuntimeContentHandle` é a unidade de ownership de uma instância runtime.

Deve conter, no mínimo:

```text
identity
owner scope
state
resource name/path diagnostic only
release policy/action futuro
release diagnostics
```

O handle deve ser seguro contra double-release e stale usage. Double-release não deve destruir duas vezes; deve ser diagnosticado.

---

## Lifecycle integration

Antes de prefab materializer, F8 precisa conectar roots ao lifecycle real:

```text
Route enter      -> cria Route runtime root
Activity enter   -> cria Activity runtime root
Activity clear   -> libera Activity runtime root
Route switch     -> libera Activity root antes de Route root
Session shutdown -> libera roots restantes
```

A ordem de saída deve ser bottom-up:

```text
Runtime content
  -> Activity content
    -> Activity
      -> Route content
        -> Route
          -> Session
```

---

## Materialization

Materialização deve ser explícita:

```text
RuntimeMaterializationRequest
→ materializer
→ RuntimeMaterializationResult
→ RuntimeContentHandle
```

Depois do realinhamento F8D1, request/result entram somente após owner/context/lifecycle integration:

```text
F8E RuntimeContentRuntime + RuntimeScopeContext
F8F Lifecycle root integration
F8G RuntimeMaterializationRequest / RuntimeMaterializationResult
```

O materializer concreto inicial deve ser simples e local, provavelmente `PrefabContentMaterializer`.

F8 não deve materializar Actor, Pause, Camera, UI ou pooled objects.

---

## Transition guard e cancelamento scoped

F8 deve formalizar o mínimo de segurança para runtime content:

- não materializar em root inexistente;
- não materializar em root releasing/released;
- não registrar handle se a operação foi cancelada;
- não concluir operação cancelada como materialized;
- rejeitar mutações incompatíveis durante transições de Route/Activity;
- diagnosticar double-release sem executar duas vezes.

Modelo esperado:

```text
Session token
  -> Route linked token
    -> Activity linked token
      -> materialization/release operation token
```

---

## Release

F8 precisa de release policy para runtime-created content, separada do release de cenas da F6.

Diferença:

| Release | Dono |
|---|---|
| F6 scene release | `ContentReleasePlan` descarrega additional Route scenes owned. |
| F8 runtime release | `RuntimeContentHandle` libera instâncias criadas em runtime. |

O primeiro release real de F8 pode destruir GameObject instanciado por prefab materializer. Pool return fica fora da F8.

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
- `FrameworkUpdateDispatcher` / `ITickable`;
- Addressables backend;
- DOTS/ECS/Subscenes adapter;
- assembly split;
- settings source hardening;
- automatic fallback root quando root obrigatório estiver ausente.

---

## Plano incremental oficial após F8D1

| Corte | Objetivo |
|---|---|
| `F8A` | ADR/detail audit de runtime roots/materialization. `CLOSED` |
| `F8B` | Primitivas de runtime ownership/scope/state. `CLOSED` |
| `F8C` | `RuntimeContentHandle` passivo e release state. `CLOSED` |
| `F8D` | `RuntimeScopeRoot` + registry interno mínimo. `CLOSED` |
| `F8D1` | Realinhamento documental do plano F8. `APPLIED / DOCS ONLY` |
| `F8E` | `RuntimeContentRuntime` + `RuntimeScopeContext`. `NEXT` |
| `F8F` | Lifecycle root integration para Route/Activity. |
| `F8G` | `RuntimeMaterializationRequest` / `RuntimeMaterializationResult`. |
| `F8H` | Transition guard + scoped cancellation model. |
| `F8I` | `PrefabContentMaterializer` simples/local. |
| `F8J` | Runtime release execution por handle/scope. |
| `F8K` | Runtime materialization/release smoke e fechamento F8. |

---

## Critérios de fechamento de F8

F8 só fecha quando houver smoke demonstrando:

```text
Prefab materializado em Activity scope
handle retornado como materialized
root de owner correto
registry contém o handle
clear/switch/exit do scope executa release
GameObject criado é destruído ou liberado pela policy
handle termina released
registry termina sem orphan
sem GameObject.Find
sem fallback silencioso
operação cancelada não registra handle ativo
release duplicado é seguro/diagnosticado
```
