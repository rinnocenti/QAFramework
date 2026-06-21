# F2-02 — ADR-SESSION-002 — SessionContent Ownership Semantics

Status: Accepted  
Fase: F2  
Cut de decisão: F2A  
Ordem no Plano: F2-02  
Tipo: Session / Content  
Escopo: SessionContentSet

---

## Contexto

F1F estabilizou a identidade funcional de `FrameworkContentHandle` como composição de owner identity, scope, kind e content id. F2 pode então definir um `SessionContentSet` mínimo sem depender de Route, Activity, Surface ou RuntimeMaterialization.

Antes de conteúdo persistente, runtime roots ou consumers, o framework precisa distinguir registro diagnóstico de ownership real. Sem essa distinção, `SessionContentSet` vira um manager global ou passa a liberar objetos que não possui.

---

## Decisão

Criar `SessionContentSet` mínimo com semântica explícita de ownership.

O set pertence à Session. Ele é dado/estado de Session, não host e não service locator.

Cada item de Session content deve ter identidade compatível com F1:

```text
FrameworkContentIdentity
FrameworkContentHandle
FrameworkIdentityKey owner
FrameworkContentScope.Session
```

A primeira implementação pode conter zero itens reais, desde que o tipo e a semântica existam.

---

## Estados de ownership

A semântica aceita para itens de Session content é:

| Estado | Significado |
|---|---|
| `Registered` | O runtime conhece o item, mas não necessariamente o possui. |
| `Owned` | A Session é responsável por release/teardown quando essa responsabilidade existir. |
| `DiagnosticOnly` | O item aparece em status/smoke/facts, mas não possui release. |

Essa enumeração pode ser implementada com nome equivalente, desde que preserve a distinção.

---

## Restrições

`SessionContentSet` não deve conter, nesta fase:

```text
Route primary scene handles
Activity local content
Surface bindings
Runtime materialized prefabs
Camera rigs
Audio listener ownership
Input maps
Actor runtime instances
Pooling services
```

Route primary scene continua sendo Route content. Activity binding continua sendo Activity/local visibility adapter. Audio/camera/input são consumers futuros.

---

## Consequências

### Positivas

- Prepara conteúdo persistente sem abrir consumers.
- Evita misturar diagnóstico com ownership.
- Cria paralelismo controlado com futuros `RouteContentSet` e `ActivityContentSet`.
- Usa `FrameworkContentIdentity` já validado em F1F.

### Trade-offs

- O set pode parecer vazio no início.
- A utilidade principal é arquitetural até F6/F8/F11.
- Exige disciplina para não registrar tudo como Session content.

---

## Fora do escopo

- Runtime roots.
- Persistent scenes.
- Additive scene loading.
- Prefab materialization.
- Surface placement.
- Consumer ownership.
- Release policy completa.

---

## Critérios de validação da implementação posterior

- `SessionContentSet` existe e é owned pela Session.
- A documentação diferencia registro, ownership e diagnóstico.
- O set não captura Route/Activity content indevidamente.
- O smoke de boot continua passando mesmo se o set inicial estiver vazio.
- Nenhum consumer avançado entra por causa do set.

---

## Relação com roadmap

Cobre:

```text
IF-FW-ROAD-2C — SessionContentSet mínimo
IF-FW-ROAD-2D — SessionContentOwnership semantics
```

Depende de:

```text
F1F — Content identity / FrameworkContentHandle review
F2-01 — Session Scope and Owner
```

---

## Notas de implementação

Implementar em corte pequeno depois do estado de Session:

```text
F2C — SessionContentSet minimal semantics
```

Não substituir `FrameworkRuntimeHost`. Não criar registry global. Não usar `SessionContentSet` como atalho para consumers.
