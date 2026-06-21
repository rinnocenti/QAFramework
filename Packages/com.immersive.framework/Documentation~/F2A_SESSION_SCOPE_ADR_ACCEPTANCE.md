# F2A — Session scope ADR review and acceptance

Status: CLOSED / ADRS ACCEPTED  
Fase: F2  
Tipo: ADR checkpoint  
Runtime changes: none

---

## Resultado

F2A aceita os ADRs necessários para iniciar os cortes técnicos de Session scope.

```text
F2-01 — ADR-SESSION-001 — Accepted
F2-02 — ADR-SESSION-002 — Accepted
F2-03 — ADR-SETTINGS-001 — Accepted
```

Este corte não altera runtime, asmdefs, package dependencies, editor tooling ou comportamento em Play Mode.

---

## O que foi decidido

### 1. Session tem owner formal

`FrameworkRuntimeHost` é aceito como owner inicial da Session runtime, mas não como service locator público.

A Session passa a ser o escopo superior do runtime e deve ter estado explícito em corte técnico posterior.

### 2. SessionContentSet tem semântica mínima

`SessionContentSet` deve existir como dado/estado owned pela Session, não como manager global.

A semântica aceita diferencia:

```text
Registered
Owned
DiagnosticOnly
```

A primeira versão pode ser vazia, desde que a semântica exista.

### 3. Settings source fica decidido para F2

`Resources.Load<ImmersiveFrameworkSettingsAsset>(ImmersiveFrameworkSettingsAsset.ResourcesPath)` é aceito como fonte temporária e explícita de settings para o bootstrap atual.

Não há fallback silencioso: settings ausente ou `GameApplicationAsset` ausente devem falhar visivelmente.

---

## Relação com roadmap

F2A cobre ou destrava:

```text
IF-FW-ROAD-2A — ADR: Session Scope
IF-FW-ROAD-2B — SessionRuntimeState explícito
IF-FW-ROAD-2C — SessionContentSet mínimo
IF-FW-ROAD-2D — SessionContentOwnership semantics
IF-FW-ROAD-2E — Settings source decision
IF-FW-ROAD-2F — Session smoke
```

---

## Guardrails para F2 técnico

Os cortes técnicos de F2 não devem abrir:

```text
Route baseline
Activity content/readiness
Surface
Runtime roots
Runtime materialization
Persistent scenes
Camera
Audio
Input
Actor
Pooling
Save
Pause
```

Também não devem criar:

```text
service locator público
registry global mutável
fallback silencioso
pipeline de Session pesada
```

---

## Próximo corte autorizado

```text
F2B — SessionRuntimeState explicit boundary
```

Escopo esperado:

```text
- declarar/refinar estado explícito de Session;
- conectar estado de Session ao host atual;
- manter boot e route smoke passando;
- não criar SessionContentSet ainda, salvo se for impossível separar.
```

`SessionContentSet` deve ficar para F2C se F2B puder permanecer pequeno.
