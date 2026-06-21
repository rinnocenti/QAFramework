# F2C — SessionContentSet Minimal Model

Status: APPLIED / PENDING COMPILE-SMOKE  
Fase: F2  
ADR base: `F2-02 — ADR-SESSION-002 — SessionContent Ownership Semantics`

---

## Objetivo

Criar o modelo mínimo de `SessionContentSet` e a semântica explícita de ownership de conteúdo conhecido pela Session.

Este corte não cria loading de conteúdo, persistent scenes, runtime roots, release policy ou consumers.

---

## O que foi criado

```text
Runtime/SessionLifecycle/SessionContentOwnership.cs
Runtime/SessionLifecycle/SessionContentEntry.cs
Runtime/SessionLifecycle/SessionContentSet.cs
```

### `SessionContentOwnership`

Define a semântica mínima aceita no ADR:

| Valor | Significado |
|---|---|
| `Registered` | A Session conhece o item, mas não necessariamente o possui. |
| `Owned` | A Session declara ownership do item. Release real fica para fase futura. |
| `DiagnosticOnly` | O item aparece apenas em diagnóstico/status e não implica release. |

### `SessionContentEntry`

Representa um `FrameworkContentHandle` com ownership explícito.

Regras:

```text
- exige handle válido;
- exige scope Session;
- não aceita ownership indefinido;
- não registra Route primary scene como Session content.
```

### `SessionContentSet`

Representa o conjunto imutável de entries conhecidos pela Session.

A primeira implementação pode ser vazia. Isso é intencional: F2C cria a fronteira sem puxar persistent scenes, audio listener, camera, input, actors ou runtime materialization para o core inicial.

---

## Integração com SessionRuntimeState

`SessionRuntimeState` agora contém:

```text
SessionContentSet SessionContentSet
HasSessionContent
SessionContentCount
```

`FrameworkRuntimeState` continua como fachada de compatibilidade e expõe o `SessionContentSet` via `SessionState`.

---

## O que não foi feito

F2C não cria:

```text
content loading
persistent scenes
scene additive ownership
release policy
Surface
RuntimeMaterialization
Audio listener ownership
Camera rig ownership
Input map ownership
Actor runtime ownership
Pooling services
registry global
service locator
```

---

## Validação esperada

Aplicar o pacote e rodar o smoke padrão:

```text
1. Unity compila sem erro CS.
2. Boot passa.
3. Route Smoke passa.
4. Activity Smoke passa.
5. Clear Activity Smoke passa.
```

Se passar:

```text
F2C — CLOSED / COMPILE-SMOKE PASS
```

---

## Próximo corte provável

```text
F2D — Settings source cleanup / Session smoke checkpoint
```

A fase F2 só deve fechar depois de registrar o checkpoint final de Session scope.
