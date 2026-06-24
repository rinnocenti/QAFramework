# F9-01 — ADR-ANCHOR-002 — Content Anchor Binding and Runtime Placement

Status: Accepted / F9F automatic logical binding cleanup applied
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
F9B RuntimeContentAnchorBinding [applied as logical runtime]
F9C Binding smoke and lifecycle diagnostics [PASS]
F9D Binding lifecycle cleanup/snapshot policy [PASS]
F9E FrameworkRuntimeHost ownership for binding runtime [PASS]
F9F automatic logical binding cleanup on Route/Activity exit [applied]
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

## Implementação F9B

F9B adiciona `RuntimeContentAnchorBinding` como runtime lógico interno. Ele resolve um `ContentAnchorBindingRequest` contra um `ContentAnchorSet` fornecido e um `RuntimeContentHandle` já registrado em `RuntimeContentRuntime`.

F9B não adiciona placement físico, prefab adapter, scene adapter, `Transform`, `GameObject`, `Instantiate`, `Destroy` ou consumer final.

## Implementação F9C

F9C adiciona `Run Content Anchor Binding Smoke` ao QA Canvas. O smoke usa um `ContentAnchorSet` de Route já descoberto, cria um `RuntimeContentHandle` sintético no `RuntimeScopeContext` da Route, executa `RuntimeContentAnchorBinding.Bind(...)`, valida idempotência com `SucceededAlreadyBound`, remove o binding lógico e libera/unregistra o handle via `RuntimeReleasePolicy.MarkReleasedAndUnregister`.

F9C permanece diagnóstico: não move `Transform`, não instancia prefab, não carrega cena, não chama `Destroy`, não cria adapter físico e não cria consumer final.

## Implementação F9D/F9E

F9D adiciona operações explícitas de cleanup/snapshot por runtime content, runtime owner/scope, anchor e anchor owner/scope.

F9E torna o `FrameworkRuntimeHost` o owner canônico do `RuntimeContentAnchorBinding` e expõe métodos internos controlados para bind/unbind/snapshot. O smoke deixa de criar binding runtime local e passa a validar o runtime owned pelo host.

F9F injeta esse runtime owned pelo host nos lifecycles de Route/Activity e executa cleanup lógico por `RuntimeContentOwner` no exit do owner antigo, antes da remoção do root lógico antigo. F9F não adiciona placement físico, prefab adapter, scene adapter, `Transform`, `GameObject`, `Instantiate` ou `Destroy`.

## Regras

- Binding depende de F7 ContentAnchorSet e F8 RuntimeContent.
- Binding não cria `ContentAnchorManager` global; o owner canônico é o `FrameworkRuntimeHost`, sem service locator gameplay-facing.
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
