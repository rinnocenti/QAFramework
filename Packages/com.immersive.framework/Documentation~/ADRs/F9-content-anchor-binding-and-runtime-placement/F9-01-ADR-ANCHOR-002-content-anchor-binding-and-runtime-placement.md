# F9-01 — ADR-ANCHOR-002 — Content Anchor Binding and Runtime Placement

Status: Accepted / F9A contracts applied
Fase: F9
Ordem no Plano: F9-01
Tipo: Content Anchor / Runtime
Escopo: ContentAnchorBinding

---

## Contexto

F7 declarou Content Anchor como contrato authored/passivo de espaço. F8 cria RuntimeContent ownership, request/result, guardas e release policy. F9 é a ponte entre esses dois mundos.

F9 não é Pause, Camera, Actor, UI ou loading screen. Ele resolve placement e release order para que esses consumers possam existir depois.

---

## Decisão

Definir em etapas:

```text
F9A ContentAnchorBindingRequest
F9A ContentAnchorBindingResult
F9A ContentAnchorContentHandle
F9B RuntimeContentAnchorBinding
F9C Binding release order
F9D Binding smoke
```

Consumer solicita Content Anchor por identity. O runtime resolve root/slot/point, cria ou associa runtime content quando permitido, devolve handle e registra release order. Consumer não destrói o Content Anchor nem o runtime content diretamente.

---



## Implementação F9A

F9A adiciona os contratos passivos:

```text
ContentAnchorBindingRequest
ContentAnchorBindingResult
ContentAnchorBindingStatus
ContentAnchorContentHandle
```

Esses tipos correlacionam `ContentAnchorDeclaration`, `RuntimeScopeContext`, `RuntimeContentId`, `RuntimeMaterializationResource` e `RuntimeContentHandle` sem executar placement físico.

F9A não adiciona `Transform`, `GameObject`, `Instantiate`, `Destroy`, scene adapter, prefab adapter, Addressables, Pooling, Pause, Camera ou Actor.

## Regras

- Binding depende de F7 ContentAnchorSet e F8 RuntimeContent.
- Binding não cria `ContentAnchorManager` global.
- Binding não cria fallback quando anchor required está ausente.
- Binding release ocorre antes do release do owner scope/root.
- Binding não implementa fade/loading, Pause overlay, Camera rig, Actor presentation ou Pooling.

---

## Consequências

### Positivas

- Destrava Pause, Camera, Transition presentation e Actor presentation sem capturar core.
- Centraliza release order de content placed em anchors.
- Evita endpoints locais materializando e destruindo conteúdo diretamente.

### Negativas / trade-offs

- Exige validators adicionais.
- Exige smoke de release order.
- Adia consumers visíveis para F12/F13.

---

## Critérios de validação

- Binding falha se Content Anchor required ausente.
- Binding não cria root/anchor por fallback.
- ContentAnchorContentHandle correlaciona anchor, runtime content e owner scope.
- Release de binding ocorre antes do release do content/root owner.
- Smoke confirma zero orphan.

---

## Relação com roadmap

F9 permanece antes de Transition/Activity content execution (F10), Participation/Capability runtime (F11), Input/Save/Pause (F12) e advanced consumers (F13).
