# Content Anchor Binding Contracts

Status: `F9C APPLIED / BINDING SMOKE PENDING`
Corte: `IF-FW-F9A-content-anchor-binding-contracts`
Escopo: Content Anchor binding / RuntimeContent bridge

---

## Objetivo

F9A cria a primeira ponte formal entre os anchors authored de F7 e os contratos de RuntimeContent de F8.

A fronteira agora é:

```text
ContentAnchorBindingRequest
  -> RuntimeContentAnchorBinding
      -> ContentAnchorBindingResult
          -> ContentAnchorContentHandle
ContentAnchorSet.TryGetByBindingRequest(...)
```

Este corte ainda não resolve objeto de cena, não move `Transform`, não instancia prefab e não cria consumer.

---

## Tipos adicionados

```text
ContentAnchorBindingRequest
ContentAnchorBindingResult
ContentAnchorBindingStatus
ContentAnchorContentHandle
ContentAnchorSet.TryGetByBindingRequest(...)
```

### `ContentAnchorBindingRequest`

Pedido explícito para vincular runtime content a um Content Anchor.

Carrega:

```text
anchor scope
anchor owner
anchor kind
anchor id
runtime scope context
runtime content id
runtime materialization resource
source/reason diagnóstico
```

O request referencia o anchor por identidade explícita. Ele não cria fallback quando o anchor não existe. `ContentAnchorSet.TryGetByBindingRequest(...)` resolve somente contra declarações já descobertas/autorizadas no set passivo.

### `ContentAnchorBindingResult`

Resultado tipado de uma tentativa de binding.

Pode reportar:

```text
Succeeded
SucceededAlreadyBound
FailedMissingAnchor
RejectedMismatchedAnchor
RejectedMismatchedRuntimeContent
RejectedReleasedRuntimeContent
FailedRuntimeMaterialization
FailedRuntimeRegistration
```

### `ContentAnchorContentHandle`

Handle passivo que correlaciona:

```text
ContentAnchorDeclaration
RuntimeContentHandle
ContentAnchorBindingRequest
```

Ele não possui lifecycle próprio e não executa release físico.

---

## Regras

- Binding usa identidade explícita de Content Anchor.
- Binding usa `RuntimeScopeContext` explícito; não consulta estado global.
- Binding não cria Content Anchor ausente.
- Binding não cria runtime root ausente.
- Binding não registra materialização física.
- Binding não move ou parenta objeto.
- Binding não destrói objeto.
- Binding não é Pause, Camera, Actor, UI, Pooling ou Transition.

---

## Separação F8/F9

F8 define:

```text
RuntimeContentRuntime
RuntimeScopeContext
RuntimeMaterializationRequest/Result
RuntimeContentHandle
Runtime release policy
```

F9A define:

```text
como um request de Content Anchor se correlaciona com um RuntimeContentHandle
```

F9B define:

```text
RuntimeContentAnchorBinding runtime lógico
binding registry lógico local
idempotência SucceededAlreadyBound
remoção lógica local de bindings
```

F9B ainda não define:

```text
placement físico
binding release order execution integrado ao lifecycle
binding smoke dedicado aplicado em F9C
```

---

## Smoke esperado

Para F9C, o smoke esperado é:

```text
compile/import smoke
Run Content Anchor Binding Smoke
```

Não há Play Mode behavior novo neste corte.
