# F8D1 — F8 Plan Realignment

Status: `F8K PASS / F8 CLOSED`

Este documento registra o realinhamento oficial da Fase 8 após a implementação de `F8D`.

Ele substitui a leitura anterior de que o próximo corte técnico seria imediatamente `RuntimeMaterializationRequest` / `RuntimeMaterializationResult`.

---

## Decisão central

F8 continua sendo a fase de runtime roots, materialization e release. Porém, depois de `F8D`, ficou explícito que request/result não devem ser o próximo passo direto.

A nova ordem oficial é:

```text
F8D logical RuntimeScopeRoot + RuntimeRootRegistry
  -> F8E RuntimeContentRuntime + RuntimeScopeContext
  -> F8F lifecycle root integration
  -> F8G RuntimeMaterializationRequest / RuntimeMaterializationResult
  -> F8H transition guard + scoped cancellation model
  -> F8I Materialization adapter boundary
  -> F8J runtime release execution
  -> F8K materialization/release smoke and closure
```

Motivo:

```text
Antes de criar prefabs ou request/result públicos, o framework precisa saber quem é o owner runtime do registry, como Route/Activity criam e encerram roots, e como operações pendentes são rejeitadas/canceladas quando o escopo muda.
```

---

## Estado aceito até F8D

Já está aplicado:

| Corte | Estado | Entrega |
|---|---|---|
| `F8A` | `CLOSED / DOCS` | ADR/detail audit de Runtime Root != Content Anchor. |
| `F8B` | `CLOSED / PASS` | Primitivas de ownership/scope/state/identity para runtime-created content. |
| `F8C` | `CLOSED / PASS` | `RuntimeContentHandle` passivo com transições e diagnostics de release. |
| `F8D` | `CLOSED / PASS` | `RuntimeScopeRoot` lógico e `RuntimeRootRegistry` interno mínimo. |

F8D permanece deliberadamente passivo:

```text
sem hierarchy root físico
sem Transform parent
sem Instantiate
sem Destroy
sem request/result
sem materializer físico
sem release execution
sem Content Anchor binding
```

---

## Decisões aceitas em F8D1

### 1. `RuntimeRootRegistry` precisa de um owner runtime interno

`RuntimeRootRegistry` não deve ser acessado por gameplay scripts, consumers futuros ou service locator.

A próxima peça deve ser um owner interno, provisoriamente descrito como:

```text
RuntimeContentRuntime
```

Responsabilidades esperadas:

- manter o `RuntimeRootRegistry` da sessão runtime atual;
- criar roots explicitamente por `RuntimeContentOwner`;
- fornecer operações internas controladas para registro, materialização e release futuros;
- não criar API pública global;
- não materializar prefabs;
- não resolver Content Anchors;
- não destruir conteúdo authored de cena.

### 2. Contexto deve ser passado, não consultado globalmente

F8 não deve criar um `IRuntimeContextProvider` público ou global para materializer perguntar qual Route/Activity está ativa.

O shape aceito é:

```text
RuntimeScopeContext / request context
```

Esse contexto deve ser criado pelo lifecycle owner que já conhece o escopo atual e passado para operações runtime internas.

Regra:

```text
Materializer não consulta estado global.
Quem solicita materialization fornece owner/scope/source/reason de forma explícita.
```

### 3. Não criar registry paralelo por Activity

F8 não deve criar `ActivityRuntimeContentRegistry` ou `IActivityHandleRegistry` como registry separado.

O registry canônico é o registry de runtime roots:

```text
RuntimeContentRuntime
  -> RuntimeRootRegistry
    -> RuntimeScopeRoot(owner)
      -> RuntimeContentHandle
```

Activity e Route devem integrar-se por owner/scope, não por uma coleção paralela.

### 4. Roots precisam entrar no lifecycle real antes de qualquer adapter físico

O próximo bloco técnico depois do owner runtime deve conectar roots aos escopos reais:

```text
Route enter    -> cria Route runtime root
Activity enter -> cria Activity runtime root
Activity clear/switch/exit -> libera Activity runtime root
Route switch/exit -> libera Activity root antes de Route root
```

### 5. Teardown deve ser bottom-up

A regra aceita é:

```text
Entrada: Session -> Route -> Route content -> Activity -> Activity content -> Runtime content
Saída:   Runtime content -> Activity content -> Activity -> Route content -> Route -> Session
```

Para F8, isso significa:

- runtime-created Activity content deve ser liberado antes do Activity content authored sair completamente;
- runtime-created Route content deve ser liberado antes do Route scope ser encerrado;
- Route exit deve encerrar Activity runtime content antes de Route runtime content.

### 6. Transições concorrentes precisam de contrato mínimo

F8 deve formalizar que operações runtime não podem mutar escopos em transição inválida.

Política inicial:

| Caso | Decisão inicial |
|---|---|
| Materializar em root inexistente | Rejeitar. Não criar fallback root. |
| Materializar em root releasing/released | Rejeitar. |
| Registrar handle em owner diferente | Rejeitar. |
| Registrar handle duplicado | Rejeitar ou retornar already-registered quando for exatamente o mesmo handle. |
| Release duplicado | Seguro e diagnosticado. Nunca destruir duas vezes. |
| Request de Route/Activity durante transição incompatível | Rejeitar/ignorar com diagnóstico explícito. |

### 7. Cancelamento scoped entra em F8 antes dos consumers

F8 deve preparar cancelamento por escopo antes de Input/Pause/Save/Actor/Pooling.

Modelo esperado:

```text
Session token
  -> Route linked token
    -> Activity linked token
      -> materialization/release operation token
```

Regra:

```text
Operação cancelada não registra handle ativo e não termina como materialized.
```

### 8. F8 não cria `ITickable`/update dispatcher

`FrameworkUpdateDispatcher`, `ITickable` e controle de tick gerenciado ficam fora de F8.

Motivo:

```text
Tick gerenciado é policy de consumer/pause/gameplay, não pré-requisito para ownership/materialization/release.
```

### 9. F8 não resolve Addressables, DOTS/ECS/Subscenes ou split de asmdefs

Esses temas são válidos, mas não pertencem ao corte atual.

F8 deve primeiro provar:

```text
request -> IRuntimeMaterializationAdapter boundary -> handle/release state -> release on scope exit -> zero orphan
```

Addressables deve entrar depois como provider/adapter opcional, não como dependência do core F8.

---

## Novo plano oficial de F8

| Corte | Status | Entrega |
|---|---|---|
| `F8A` | `CLOSED` | ADR/detail audit: Runtime Root != Content Anchor; F8 não cria consumers. |
| `F8B` | `CLOSED` | Runtime ownership primitives. |
| `F8C` | `CLOSED` | `RuntimeContentHandle` passivo. |
| `F8D` | `CLOSED` | `RuntimeScopeRoot` lógico + `RuntimeRootRegistry` interno. |
| `F8D1` | `APPLIED / DOCS ONLY` | Realinhamento do plano F8 e registro de decisões. |
| `F8E` | `CLOSED / PASS` | `RuntimeContentRuntime` + `RuntimeScopeContext`. |
| `F8F` | `CLOSED / PASS` | Lifecycle root integration: Route/Activity criam e encerram roots lógicos. |
| `F8G` | `CLOSED / PASS` | `RuntimeMaterializationRequest` / `RuntimeMaterializationResult`. |
| `F8H` | `CLOSED / PASS` | Transition guard + scoped cancellation model. |
| `F8I` | `CLOSED / COMPILE-SMOKE PASS` | `IRuntimeMaterializationAdapter` boundary; adapters físicos ficam fora do core. |
| `F8J` | `CLOSED / COMPILE-SMOKE PASS` | Runtime release request/result/policy, release adapter boundary and logical release by handle/scope. |
| `F8K` | `PASS / F8 CLOSED` | Runtime Content Smoke, `ApplyMaterializationResult` registry handoff and F8 closure gate. |

---

## Backlog registrado fora de F8

### FX1 — Settings Source Hardening

Status: `BACKLOG / POST-F9 OR POST-F10`

Decidir se `Resources.Load` continua como fonte oficial de settings ou se será substituído por provider explícito.

Regras:

- não usar path absoluto;
- não criar fallback silencioso por scan global;
- manter fail-fast para settings obrigatórios ausentes.

### FX2 — Assembly Boundary Audit

Status: `BACKLOG / AFTER F8-F9 STABILIZATION`

Auditar separação futura de assemblies.

Possíveis fronteiras:

```text
Runtime core
Unity runtime
Authoring
QA/dev tooling
Editor
```

Não aplicar antes de F8/F9 estabilizarem as fronteiras reais de runtime content.

### FX3 — Historical CameraFlow Documentation Hygiene

Status: `BACKLOG / DOCS ONLY`

O package atual não deve tratar CameraFlow/Cinemachine/FrameworkCameraAuthority como superfície ativa.

Se documentos históricos mencionarem CameraFlow, a leitura oficial é:

```text
CameraFlow é histórico/removido/deferred. Não influencia F8.
```

### Future asset provider / Addressables adapter

Status: `BACKLOG / AFTER LOCAL PREFAB MATERIALIZER`

Depois que F8K passar no Runtime Content Smoke, F8 pode ser fechado. Provider de asset, prefab/cena/pool/Addressables adapters e Content Anchor binding ficam em fases próprias.

F8 não depende de Addressables.

---

## Próximo corte autorizado

```text
F8E — RuntimeContentRuntime + RuntimeScopeContext
```

`RuntimeMaterializationRequest` só deve existir depois do owner runtime interno e da semântica de contexto por escopo. Isso já foi aplicado em F8G. F8J aplica release lógico. F8K adiciona o smoke de fechamento e o handoff explícito `ApplyMaterializationResult`, sem materialização física no core.
