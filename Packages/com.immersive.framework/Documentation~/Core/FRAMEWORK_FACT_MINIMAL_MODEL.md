# FrameworkFact Minimal Model

Status: F1C / Applied / Pending Compile-Smoke  
Fase: F1  
Tipo: Diagnostics / Foundation  

---

## Objetivo

Introduzir um modelo mínimo de diagnóstico estruturado para o framework.

A regra central do ADR-DIAG-001 é:

```text
Log humano = mensagem textual para leitura.
FrameworkFact = dado estruturado sobre algo que aconteceu ou foi validado.
```

F1C cria o vocabulário mínimo para facts, mas não substitui logs e não cria recorder global.

---

## Tipos criados

```text
Runtime/Diagnostics/FrameworkFact.cs
Runtime/Diagnostics/FrameworkFactCode.cs
Runtime/Diagnostics/FrameworkFactScope.cs
Runtime/Diagnostics/FrameworkFactSeverity.cs
```

### `FrameworkFact`

Representa um fato estruturado mínimo.

Campos:

| Campo | Papel |
|---|---|
| `Code` | Código estável do fato. |
| `Scope` | Domínio do fato: Application, Route, Activity, Content, etc. |
| `Severity` | Severidade estruturada. |
| `Source` | Origem técnica/autoral. |
| `Subject` | Sujeito observado. |
| `Reason` | Motivo curto. |
| `Details` | Detalhes opcionais. |

### `FrameworkFactCode`

Wrapper explícito para o código do fact.

Regras:

```text
- Não aceita null, vazio ou whitespace.
- Não gera fallback silencioso.
- Não usa Guid aleatório.
- Usa comparação ordinal.
```

### `FrameworkFactScope`

Escopos mínimos:

```text
Application
Session
Route
Activity
Content
Local
Surface
Runtime
Validation
QA
```

### `FrameworkFactSeverity`

Severidades mínimas:

```text
Info
Warning
Error
Fatal
```

---

## O que este corte não faz

F1C não cria:

```text
fact recorder público
service locator
event bus global
telemetry externa
dashboard
persistência de fatos
substituição dos logs atuais
integração ampla com validators
alteração de boot/route/activity lifecycle
```

---

## Como usar em cortes futuros

Exemplo conceitual:

```csharp
var fact = new FrameworkFact(
    new FrameworkFactCode("framework.boot.succeeded"),
    FrameworkFactScope.Application,
    FrameworkFactSeverity.Info,
    source: "ImmersiveFrameworkBootstrap",
    subject: "Game Application",
    reason: "Startup route resolved");
```

Este exemplo não implica que o boot já emite facts. Integrações devem ocorrer em cortes próprios.

---

## Validação esperada

Para fechar F1C:

```text
1. Unity compila sem erro CS.
2. Boot smoke passa.
3. Route Smoke passa.
4. Activity Smoke passa.
5. Clear Activity Smoke passa.
```

Se passar:

```text
F1C — CLOSED / COMPILE-SMOKE PASS
```
