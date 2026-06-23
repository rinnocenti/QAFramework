# F7-01 — ADR-ANCHOR-001 — Content Anchor as Placement Contract

Status: Accepted / F7 closed  
Fase: F7  
Ordem no Plano: F7-01  
Tipo: Content Anchor  
Escopo: Content Anchor declaration baseline

---

## Contexto

F6 fechou o baseline de composição e release de cenas de Route. Uma Route agora pode carregar Primary Scene, carregar additional scenes owned por `RouteContentProfileAsset`, registrar os handles carregados em `RouteContentSet` e liberar additional scenes owned na saída da Route.

Ainda falta um contrato autoral para responder onde, dentro do conteúdo carregado, sistemas futuros podem localizar, montar ou associar conteúdo runtime sem usar `Find`, nomes mágicos de GameObject, singletons, referências hardcoded ou ownership implícito.

O vocabulário anterior planejado foi descartado porque era ambíguo em Unity e em português. O nome canônico passa a ser `Content Anchor`.

---

## Decisão

`Content Anchor` é um contrato autoral, passivo, nomeado e escopado dentro de conteúdo carregado.

Ele identifica pontos confiáveis para consumidores futuros, mas não executa esses consumidores.

Uma Content Anchor pode representar:

- um root de conteúdo;
- um slot de montagem;
- um ponto semântico de referência;
- metadados mínimos de escopo, identidade e requiredness.

Uma Content Anchor não é:

- scene loader;
- runtime materializer;
- prefab binding;
- spawn system;
- camera rig;
- pause behavior;
- UI behavior;
- input policy;
- save/snapshot system;
- service locator.

Consumers futuros consomem um `ContentAnchorSet` já resolvido pelo lifecycle. Eles não devem procurar GameObjects arbitrários nem definir o modelo de Content Anchor.

---

## Linguagem canônica

Use `Content Anchor` para o conceito.

Use `Anchor` em UX curta quando o contexto já estiver claro, por exemplo em Inspector dentro de uma seção `Content Anchor`.

Não usar:

- o vocabulário antigo de ponto de espaço;
- `Hook`;
- `Content Hook`;
- `Hook Content`;
- qualquer nome duplicado no estilo “anchor anchor”.

Nomes duplicados no estilo “anchor anchor” são proibidos por redundância e baixa legibilidade. Quando for necessário nomear um ponto específico, usar `ContentAnchorPoint`.

---

## Modelo conceitual aprovado

| Conceito | Papel |
|---|---|
| `ContentAnchorId` | Identidade estável, curta e explícita do anchor. |
| `ContentAnchorScope` | Escopo autoral/lifecycle onde o anchor vive: Route, Activity ou Local. |
| `ContentAnchorKind` | Tipo da declaração: Root, Slot ou Point. |
| `ContentAnchorRequiredness` | Required ou Optional para validação autoral. |
| `ContentAnchorRoot` | Container/raiz semântica de um conjunto de conteúdo. |
| `ContentAnchorSlot` | Ponto de montagem planejado para conteúdo runtime futuro. |
| `ContentAnchorPoint` | Ponto semântico/posição/referência; não implica montagem. |
| `ContentAnchorSet` | Resultado escopado com anchors descobertos e validados. |

A primeira API pública de authoring deve preferir componentes legíveis por escopo, por exemplo:

```text
Route Content Anchor
Activity Content Anchor
```

A implementação pode compartilhar contrato interno, mas a UX pública não deve começar por um componente genérico ambíguo se isso dificultar entendimento no Inspector.

---

## Regras de identidade

A identidade de Content Anchor deve ser explícita e estável.

Permitido como diagnóstico, mas não como identidade funcional:

- `GameObject.name`;
- hierarchy path;
- scene name;
- scene path;
- instance id;
- componente encontrado por busca global.

Formato recomendado para ids authored:

```text
gameplay.world
player.spawn.primary
camera.default-target
ui.hud-root
pause.overlay-root
```

Os ids devem ser únicos dentro do escopo que o validator definir. A política inicial recomendada é unicidade por:

```text
scope owner + anchor id + kind
```

---

## Escopo

Content Anchor deve nascer escopado ao conteúdo carregado, não global.

Escopos autorizados para F7:

| Escopo | Uso |
|---|---|
| Route | Anchor presente em cenas owned/loaded da Route. |
| Activity | Anchor presente em conteúdo authored/ativo da Activity. |
| Local | Anchor declarado por contribuição local, quando aplicável. |

F7 não deve introduzir Session/global anchors ainda. Se esse caso aparecer, deve virar ADR próprio.

---

## Relação com F6

F6 responde:

```text
Quais cenas pertencem à Route?
Como elas carregam?
Como elas são liberadas?
```

F7 responde:

```text
Quais pontos confiáveis existem dentro do conteúdo carregado?
Como esses pontos são identificados e validados?
```

F7 consome o resultado de F6 como boundary de conteúdo carregado, mas não altera composition/release de cenas.

---

## Relação com F8/F9/F10/F11

F7 declara anchors.

F8/F9 podem usar anchors para materialização/binding runtime.

F10/F11 podem consumir anchors para Pause, Camera, UI, Actor, Audio ou outros consumers.

Consumers não podem capturar F7. Se um consumer precisar de campo específico, o campo pertence ao consumer ou a um binding posterior, não ao Content Anchor core.

---

## Fora do escopo de F7

- Materializar prefab.
- Criar runtime spawned content.
- Criar `RuntimeContentAnchorBinding`.
- Criar `ContentAnchorBindingRequest`/`ContentAnchorBindingResult`.
- Criar Camera/Pause/UI/Actor/Audio consumer.
- Criar lifecycle próprio de anchors.
- Criar service locator/registry global.
- Criar Addressables backend.

---

## Critérios de validação da fase

F7 só pode fechar quando houver:

- identidade tipada de Content Anchor;
- modelo Root/Slot/Point definido;
- componente público de authoring inicial;
- discovery escopado para conteúdo carregado;
- `ContentAnchorSet` com diagnostics mínimos;
- validator para missing id e duplicidade;
- smoke manual demonstrando cena carregada com anchors descobertos.

---

## Consequências positivas

- Evita que Camera, Pause ou UI definam o modelo de ancoragem.
- Evita `Find` e nomes mágicos como contrato funcional.
- Cria linguagem mais clara para designer e programação.
- Mantém materialização/runtime binding em fase posterior.
- Dá um ponto de integração estável para consumers sem transformar o framework em service locator.

## Trade-offs

- Introduz uma camada conceitual antes de recursos visíveis.
- Requer UX cuidadosa no Inspector.
- Exige validators para evitar ids duplicados e anchors órfãos.
- Pode parecer abstrato se não for documentado com exemplos de Route/Activity.

---

## Corte atual

F7A aceitou este ADR e definiu o detalhe do modelo.

F7B introduziu apenas primitivas passivas de identidade:

```text
ContentAnchorId
ContentAnchorScope
ContentAnchorKind
ContentAnchorRequiredness
```

F7B não cria componente de authoring, discovery, validator, set/registry, materialization, binding runtime ou smoke novo.

F7C introduziu somente o modelo passivo de declaração:

```text
ContentAnchorDeclaration
ContentAnchorRoot
ContentAnchorSlot
ContentAnchorPoint
```

F7C não cria componente de authoring, discovery, validator, set/registry, materialization, binding runtime ou smoke novo.

F7D introduced the first passive Route-scoped authoring component:

```text
RouteContentAnchor
```

It declares Route owner, Anchor Id, Kind, Requiredness, Display Name and Description, and can produce a local `ContentAnchorDeclaration`. F7D does not add discovery, validators, smoke, RuntimeRoot/materialization or gameplay consumers.

F7E introduced the passive scoped set model:

```text
ContentAnchorSet
ContentAnchorSetIssue
ContentAnchorSetIssueKind
```

`ContentAnchorSet` stores unique `ContentAnchorDeclaration` entries and records local diagnostic issues for invalid declarations, duplicate full identity and duplicate owner/scope/anchor id. It does not discover scene objects, integrate with Route lifecycle, validate authoring globally, emit logs, bind runtime content or serve gameplay consumers.

F7F introduced diagnostic discovery of scene-authored `RouteContentAnchor` components from loaded Route scenes into a local `ContentAnchorSet`.

F7G introduced the dedicated Content Anchor diagnostics smoke and simplified the visible QA Canvas buttons to the current validation path.

F7H introduced authoring validation for loaded `RouteContentAnchor` components. It reports missing Route, missing Anchor Id, `Kind = Unknown`, invalid Requiredness, scene/Route declaration mismatch and duplicate Content Anchor identity/id. It does not enforce Required anchors in Route lifecycle and does not add runtime binding, placement, Activity anchors or consumers.

F7I closed this phase as the Content Anchor declaration baseline.

Next authorized phase:

```text
F8A — Runtime roots/materialization ADR-detail audit
```


---

## F7 closure note

F7 closed after authoring validation smoke passed. The accepted baseline includes passive identity primitives, declaration models, `RouteContentAnchor` authoring, `ContentAnchorSet`, Route-scoped discovery, dedicated diagnostics smoke and loaded authoring validation.

F7 intentionally does not add Activity anchors, required-anchor lifecycle blocking, runtime placement/binding, RuntimeRootRegistry, materialização física runtime or gameplay consumers. Those belong to later phases, starting with F8 ADR/detail audit.
