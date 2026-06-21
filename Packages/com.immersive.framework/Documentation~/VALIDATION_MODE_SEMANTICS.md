# ValidationMode Semantics — F1D

Status: Applied / Pending compile-smoke  
Fase: F1  
Corte: F1D — ValidationMode semantics

---

## Objetivo

`ValidationMode` deixou de ser apenas enum decorativo.

O corte F1D define uma semântica mínima para controlar a severidade e o ruído dos diagnósticos de authoring sem alterar ownership de runtime, lifecycle, Route ou Activity.

---

## Regra obrigatória

```text
Configuração required falha em todos os modos.
```

Nenhum modo cria fallback silencioso para:

```text
Active Game Application ausente
Startup Route ausente
Primary Scene ausente
referências required inválidas
```

---

## Modos

| Modo | Required config | Warnings | Info diagnostics |
|---|---|---|---|
| `Strict` | Falha | Promovidos para erro | Incluídos |
| `Standard` | Falha | Permanecem warning | Incluídos |
| `Release` | Falha | Permanecem warning | Suprimidos |

---

## Implementação mínima

Arquivos principais:

```text
Runtime/Authoring/FrameworkValidationMode.cs
Runtime/Authoring/FrameworkValidationModePolicy.cs
Editor/Validation/FrameworkAuthoringValidationReport.cs
Editor/Validation/FrameworkAuthoringValidator.cs
Editor/Validation/FrameworkAuthoringValidationGui.cs
Editor/Settings/ImmersiveFrameworkSettingsProvider.cs
```

`FrameworkValidationModePolicy` centraliza a regra:

```text
RequiredConfigurationFails(mode) = true
TreatWarningsAsErrors(Strict) = true
IncludeInfoDiagnostics(Release) = false
```

---

## O que muda

- O relatório de validação de authoring agora carrega o `ValidationMode` usado.
- Em `Strict`, warnings do validator entram no relatório como erros.
- Em `Release`, diagnostics informativos são suprimidos.
- A UI de Project Settings mostra o resumo da política ativa.
- O log de validação inclui o modo usado.

---

## O que não muda

Este corte não altera:

```text
boot runtime obrigatório
Game Flow
Route Lifecycle
Activity Flow
Scene Lifecycle
FrameworkFact recorder
telemetry
identity primitives
ContentIdentity
RouteContentRuntime
Surface
RuntimeMaterialization
```

---

## Validação esperada

Após aplicar o corte:

```text
1. Unity compila sem erro CS.
2. Boot smoke passa.
3. Route Smoke passa.
4. Activity Smoke passa.
5. Clear Activity Smoke passa.
6. Project Settings > Immersive Framework > Validate Authoring continua funcionando.
```

Smoke negativo opcional:

```text
Strict + warning autoral conhecido deve contar como erro.
Release + cenário válido deve reduzir info diagnostics no relatório.
```

---

## Status

```text
F1D — APPLIED / PENDING COMPILE-SMOKE
```
