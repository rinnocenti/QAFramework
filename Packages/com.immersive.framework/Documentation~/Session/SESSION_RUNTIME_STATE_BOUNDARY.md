# F2B — SessionRuntimeState explicit boundary

Status: APPLIED / PENDING COMPILE-SMOKE  
Fase: F2  
Tipo: Technical cut  
Runtime behavior: unchanged baseline flow expected

---

## Objetivo

F2B cria uma fronteira explícita de estado para o escopo Session sem transformar o host em service locator e sem abrir `SessionContentSet` ainda.

O corte implementa a decisão aceita em F2A:

```text
IF-FW-ROAD-2B — SessionRuntimeState explícito
```

---

## O que foi feito

### 1. Criado `SessionRuntimeState`

Arquivo:

```text
Runtime/SessionLifecycle/SessionRuntimeState.cs
```

`SessionRuntimeState` passa a ser o snapshot interno do escopo Session.

Ele guarda:

```text
GameApplication
CurrentRoute
RouteLifecycleResult
RouteContentSet
PrimarySceneResult
ActivityFlowResult
SessionStarted
```

E expõe atalhos diagnósticos:

```text
CurrentRouteName
CurrentActivityName
ActiveSceneName
ActiveScenePath
HasActiveRoute
HasActiveActivity
```

### 2. `FrameworkRuntimeState` virou fachada de compatibilidade

Arquivo alterado:

```text
Runtime/ApplicationLifecycle/FrameworkRuntimeState.cs
```

`FrameworkRuntimeState` continua existindo para evitar troca ampla de chamadas internas, mas agora delega seu estado para `SessionRuntimeState`.

Isso separa a fronteira de Session sem reescrever Game Flow, Route Lifecycle ou Activity Flow neste corte.

### 3. `FrameworkRuntimeHost` expõe estado de Session internamente

Arquivo alterado:

```text
Runtime/ApplicationLifecycle/FrameworkRuntimeHost.cs
```

O host agora possui:

```csharp
public SessionRuntimeState SessionState => _state.SessionState;
```

Isso deixa explícito que o host possui o estado de Session, mas não expõe managers, registries, services ou consumers.

---

## O que não foi feito

F2B não implementa:

```text
SessionContentSet
SessionContentOwnership
Session composition context
persistent scenes
startup event/signal novo
Route baseline
Activity readiness
Surface
RuntimeMaterialization
service locator
registry global mutável
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
F2B — CLOSED / COMPILE-SMOKE PASS
```

---

## Próximo corte após smoke

```text
F2C — SessionContentSet minimal model
```

Esse próximo corte deve tratar `IF-FW-ROAD-2C` e `IF-FW-ROAD-2D`, sem puxar persistent scenes, consumers ou Route baseline antes da hora.
