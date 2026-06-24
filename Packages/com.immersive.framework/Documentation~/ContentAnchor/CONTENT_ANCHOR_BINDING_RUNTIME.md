# RuntimeContentAnchorBinding Logical Runtime

Status: `F9C APPLIED / BINDING SMOKE PENDING`
Corte base: `IF-FW-F9B-runtime-content-anchor-binding-logical-runtime`
Corte atual: `IF-FW-F9C-content-anchor-binding-smoke-diagnostics`
Escopo: Content Anchor binding / RuntimeContent bridge

---

## Objetivo

F9B adiciona o runtime lógico que executa o binding entre um `ContentAnchorBindingRequest`, um `ContentAnchorSet` já descoberto e um `RuntimeContentHandle` já registrado pelo `RuntimeContentRuntime`.

A cadeia agora é:

```text
ContentAnchorBindingRequest
  -> RuntimeContentAnchorBinding.Bind(...)
      -> ContentAnchorSet.TryGetByBindingRequest(...)
      -> RuntimeContentRuntime.TryGetHandle(...)
      -> ContentAnchorBindingResult
          -> ContentAnchorContentHandle
```

Este corte ainda não executa placement físico.

---

## Tipo adicionado

```text
RuntimeContentAnchorBinding
```

`RuntimeContentAnchorBinding` é interno e instanciável. Ele não é singleton, provider global ou service locator.

Ele mantém apenas o registry lógico de bindings já aceitos:

```text
Content Anchor identity + RuntimeContent identity
  -> ContentAnchorContentHandle
```

---

## Regras de binding

O binding só sucede quando todos os itens abaixo são verdadeiros:

```text
request é válido
anchor existe no ContentAnchorSet fornecido
anchor resolvido corresponde exatamente ao request
RuntimeContentHandle existe no RuntimeContentRuntime fornecido
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

## Remoção lógica de bindings

F9B adiciona remoção lógica local:

```text
Unbind(request)
Unbind(handle)
RemoveBindingsForRuntimeContent(identity)
RemoveBindingsForRuntimeOwner(owner)
```

Essas operações removem apenas a correlação lógica de binding. Elas não liberam runtime content e não executam cleanup físico.

---

## O que F9B não faz

F9B não adiciona:

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

F9C adiciona o botão dedicado no QA Canvas:

```text
Run Content Anchor Binding Smoke
```

O smoke valida:

```text
Route Content Anchor descoberto
-> RuntimeScopeContext da Route disponível
-> RuntimeMaterializationRequest sintético criado
-> RuntimeContentHandle sintético registrado/materializado
-> RuntimeContentAnchorBinding.Bind(...) retorna Succeeded
-> segundo Bind retorna SucceededAlreadyBound
-> Unbind remove o binding lógico
-> release lógico remove/unregistra o RuntimeContentHandle
```

Validação esperada:

```text
compile/import smoke
QA Smoke completed. name='Content Anchor Binding Smoke'
```
