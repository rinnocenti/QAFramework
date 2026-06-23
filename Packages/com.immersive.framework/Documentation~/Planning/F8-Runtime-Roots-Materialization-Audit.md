# F8 — Runtime Roots and Materialization Audit

Status: `F8B APPLIED / PRIMITIVES`

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

Ainda não existe:

- runtime root por escopo;
- runtime content handle;
- materialization request/result;
- prefab materializer;
- runtime content release policy;
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
| `Runtime Root` | Container runtime por escopo/lifecycle. Recebe objetos criados em runtime. |
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

`Runtime Root` é um GameObject/container criado ou registrado pelo framework para um escopo.

Regras:

- não usar `GameObject.Find`;
- não usar singleton/service locator global;
- não usar nome de GameObject como identidade funcional;
- não destruir conteúdo authored de cena;
- não assumir Content Anchor;
- não criar Actor/Pause/Camera/UI por conta própria.

O root deve carregar identidade, owner scope e estado suficiente para diagnostics.

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
→ materializer
→ RuntimeMaterializationResult
→ RuntimeContentHandle
```

O materializer concreto inicial deve ser simples e local, provavelmente `PrefabContentMaterializer`.

F8 não deve materializar Actor, Pause, Camera, UI ou pooled objects.

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
- automatic fallback root quando root obrigatório estiver ausente.

---

## Plano incremental recomendado

| Corte | Objetivo |
|---|---|
| `F8A` | ADR/detail audit de runtime roots/materialization. |
| `F8B` | Primitivas de runtime ownership/scope/state. `APPLIED` |
| `F8C` | `RuntimeContentHandle` passivo e release state. `APPLIED` |
| `F8D` | `RuntimeScopeRoot` + registry interno mínimo. `APPLIED` |
| `F8E` | `RuntimeMaterializationRequest` / `RuntimeMaterializationResult`. `NEXT` |
| `F8F` | `PrefabContentMaterializer` simples. |
| `F8G` | Runtime release execution por scope. |
| `F8H` | Runtime materialization/release smoke e fechamento F8. |

A ordem pode ser ajustada se a implementação mostrar dependência menor, mas nenhum consumer deve entrar antes de handle/root/release mínimos.

---

## Critérios de fechamento de F8

F8 só fecha quando houver smoke demonstrando:

```text
Prefab materializado
handle retornado
root de owner correto
release executado no scope correto
zero orphan após release
sem GameObject.Find
sem fallback silencioso
```
