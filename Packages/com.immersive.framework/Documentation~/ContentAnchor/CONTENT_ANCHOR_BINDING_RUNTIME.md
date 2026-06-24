# RuntimeContentAnchorBinding Logical Runtime

Status: `F9E APPLIED / HOST OWNERSHIP PENDING COMPILE`
Corte base: `IF-FW-F9B-runtime-content-anchor-binding-logical-runtime`
Corte validado: `IF-FW-F9C-content-anchor-binding-smoke-diagnostics` — PASS
Corte anterior: `IF-FW-F9D-content-anchor-binding-lifecycle-policy` — PASS por regressão de smoke
Corte atual: `IF-FW-F9E-binding-runtime-host-ownership`
Escopo: Content Anchor binding / RuntimeContent bridge

---

## Objetivo

F9B adicionou o runtime lógico que executa o binding entre um `ContentAnchorBindingRequest`, um `ContentAnchorSet` já descoberto e um `RuntimeContentHandle` já registrado pelo `RuntimeContentRuntime`.

F9E define onde esse runtime vive no runtime real do framework:

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

Este corte ainda não executa placement físico.

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

F9E expõe a parte necessária no host para runtime owner/scope. Cleanup por anchor continua disponível no `RuntimeContentAnchorBinding`, mas não foi promovido como fluxo canônico de lifecycle ainda.

Ordem recomendada futura para saída de escopo:

```text
unbind bindings do owner/scope
-> release lógico dos RuntimeContentHandle
-> remover RuntimeScopeRoot
```

F9E ainda não executa essa ordem automaticamente no lifecycle. Esse cleanup automático deve ser um corte separado.

---

## Diagnóstico

F9E adiciona contagem de bindings ao runtime status do QA Canvas e ao boot log e aos logs de Route/Activity request:

```text
contentAnchorBindings
```

O smoke `Run Content Anchor Binding Smoke` agora usa o binding runtime owned pelo `FrameworkRuntimeHost` e valida que o binding count volta ao valor inicial após unbind/release.

---

## O que F9B-F9E não fazem

F9B-F9E não adicionam:

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
automatic Route/Activity binding cleanup
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
