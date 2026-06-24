# RuntimeContentAnchorBinding Logical Runtime

Status: `F9I APPLIED / ACTIVITY ANCHOR BINDING SMOKE PENDING`
Corte base: `IF-FW-F9B-runtime-content-anchor-binding-logical-runtime`
Corte validado: `IF-FW-F9C-content-anchor-binding-smoke-diagnostics` — PASS
Corte anterior: `IF-FW-F9D-content-anchor-binding-lifecycle-policy` — PASS por regressão de smoke
Corte validado: `IF-FW-F9E-binding-runtime-host-ownership` — PASS por smoke regressivo
Corte atual: `IF-FW-F9I-activity-content-anchor-binding-smoke`
Escopo: Content Anchor binding / RuntimeContent bridge

---

## Objetivo

F9B adicionou o runtime lógico que executa o binding entre um `ContentAnchorBindingRequest`, um `ContentAnchorSet` já descoberto e um `RuntimeContentHandle` já registrado pelo `RuntimeContentRuntime`.

F9E define onde esse runtime vive no runtime real do framework. F9F conecta essa instância ao lifecycle lógico de Route/Activity exit:

```text
FrameworkRuntimeHost
  -> RuntimeContentRuntime
  -> RuntimeContentAnchorBinding
```

A cadeia de binding continua sendo:

```text
ContentAnchorBindingRequest
  -> FrameworkRuntimeHost.BindContentAnchor(...)
      -> RuntimeContentAnchorBinding.Bind(...)
          -> ContentAnchorSet.TryGetByBindingRequest(...)
          -> RuntimeContentRuntime.TryGetHandle(...)
          -> ContentAnchorBindingResult
              -> ContentAnchorContentHandle
```

Este corte ainda não executa placement físico. O cleanup automático remove apenas bindings lógicos antes da remoção do root lógico antigo.

---

## Tipo principal

```text
RuntimeContentAnchorBinding
```

`RuntimeContentAnchorBinding` é interno e lógico. Ele mantém apenas o registry de bindings já aceitos:

```text
Content Anchor identity + RuntimeContent identity
  -> ContentAnchorContentHandle
```

A partir de F9E, o owner canônico dessa instância é o `FrameworkRuntimeHost`. Callers internos não devem criar um novo binding runtime para o fluxo canônico do framework; devem passar pelo host.

---

## API interna controlada no host

F9E adiciona métodos internos no `FrameworkRuntimeHost`:

```text
BindContentAnchor(anchorSet, request, source, reason)
UnbindContentAnchor(request)
UnbindContentAnchor(handle)
UnbindContentAnchorRuntimeContent(identity, source, reason)
UnbindContentAnchorRuntimeOwner(owner, source, reason)
UnbindContentAnchorRuntimeScope(scope, source, reason)
SnapshotContentAnchorBindings()
SnapshotContentAnchorBindingsForRuntimeContent(identity)
SnapshotContentAnchorBindingsForRuntimeOwner(owner)
SnapshotContentAnchorBindingsForRuntimeScope(scope)
ContentAnchorBindingCount
```

Isso evita dois extremos ruins:

```text
não cria ContentAnchorBindingService global
não deixa cada smoke/consumer criar seu próprio RuntimeContentAnchorBinding isolado
```

---

## Regras de binding

O binding só sucede quando todos os itens abaixo são verdadeiros:

```text
request é válido
anchor existe no ContentAnchorSet fornecido
anchor resolvido corresponde exatamente ao request
RuntimeContentHandle existe no RuntimeContentRuntime do host
RuntimeContentHandle corresponde à RuntimeContentIdentity do request
RuntimeContentHandle não está Released
binding ainda não existe ou o binding existente continua válido
```

Falhas são reportadas por `ContentAnchorBindingResult`.

---

## Already bound

Se o mesmo anchor/runtime content já estiver vinculado e o handle existente ainda for válido, o runtime retorna:

```text
SucceededAlreadyBound
```

Isso torna o binding idempotente para o mesmo par lógico.

---

## Lifecycle policy de bindings

F9D adicionou operações explícitas de cleanup e snapshot para lifecycle lógico:

```text
UnbindRuntimeContent(identity, source, reason)
UnbindRuntimeOwner(owner, source, reason)
UnbindRuntimeScope(scope, source, reason)
UnbindAnchor(anchor, source, reason)
UnbindAnchorOwner(scope, owner, source, reason)
UnbindAnchorScope(scope, source, reason)
SnapshotBindingsForRuntimeContent(identity)
SnapshotBindingsForRuntimeOwner(owner)
SnapshotBindingsForRuntimeScope(scope)
SnapshotBindingsForAnchor(anchor)
SnapshotBindingsForAnchorOwner(scope, owner)
SnapshotBindingsForAnchorScope(scope)
```

F9E expõe a parte necessária no host para runtime owner/scope. F9F promove o cleanup por runtime owner para o fluxo canônico de lifecycle em Route/Activity exit. Cleanup por anchor continua disponível no `RuntimeContentAnchorBinding`, mas não é o caminho automático de lifecycle.

Ordem canônica para saída de escopo:

```text
unbind bindings do owner/scope
-> release lógico dos RuntimeContentHandle
-> remover RuntimeScopeRoot
```

F9F executa automaticamente o primeiro passo dessa ordem para o owner que está saindo: `unbind bindings do owner/scope`. F9I valida o mesmo caminho usando um `ActivityContentAnchor` aceito e um `RuntimeContentHandle` sintético Activity-scoped. Release físico, placement físico e adapters continuam fora deste corte.

---

## Diagnóstico

F9F mantém a contagem de bindings no runtime status do QA Canvas e adiciona diagnósticos de cleanup aos logs de Route/Activity request:

```text
contentAnchorBindings
routeContentAnchorBindingCleanup
routeContentAnchorBindingCleanupRemoved
activityContentAnchorBindingCleanup
activityContentAnchorBindingCleanupRemoved
```

O smoke `Run Content Anchor Binding Smoke` usa o binding runtime owned pelo `FrameworkRuntimeHost` e valida regressão de bind/unbind/release. O smoke `Run Content Anchor Binding Cleanup Smoke` cria um binding lógico persistente, libera o handle sintético e valida que a troca de Route remove o binding automaticamente no exit do owner.

---

## O que F9B-F9F não fazem

F9B-F9F não adicionam:

```text
Transform
GameObject
Instantiate
Destroy
Scene adapter
Prefab adapter
Addressables adapter
Pooling adapter
physical placement
physical release
Content Anchor authoring novo
Activity anchors
Pause
Camera
Actor
UI
Input
Save
```

---

## Smoke esperado

Regressão principal:

```text
Run Content Anchor Binding Smoke
Run Content Anchor Binding Cleanup Smoke
```

Resultado esperado:

```text
binding='Succeeded'
idempotentBinding='SucceededAlreadyBound'
unbound='True'
release='Succeeded'
releaseUnregistered='True'
bindingCountBefore='<valor inicial>'
bindingCount='<mesmo valor inicial>'
```

## F9F automatic logical cleanup

F9F injeta o `RuntimeContentAnchorBinding` owned pelo `FrameworkRuntimeHost` nos runtimes de Route/Activity e executa cleanup lógico por `RuntimeContentOwner` antes de remover o root lógico antigo. O cleanup não executa release físico, `Transform`, `GameObject`, `Instantiate`, `Destroy`, scene adapter, prefab adapter ou pool adapter.


## F9I Activity-scoped binding smoke

F9I adds `Run Activity Content Anchor Binding Smoke` to validate the Activity path:

```text
ActivityContentAnchor accepted by discovery
  -> Activity RuntimeScopeContext
  -> synthetic RuntimeMaterializationRequest/Result
  -> RuntimeContentHandle registered in Activity root
  -> FrameworkRuntimeHost.BindContentAnchor(...)
  -> SucceededAlreadyBound on second bind
  -> logical release/unregister of the synthetic handle
  -> Activity exit cleanup removes the remaining binding
```

The smoke still does not create Transform placement, GameObject runtime roots, prefab/scene/Addressables/pooling adapters, physical release or gameplay consumers.
