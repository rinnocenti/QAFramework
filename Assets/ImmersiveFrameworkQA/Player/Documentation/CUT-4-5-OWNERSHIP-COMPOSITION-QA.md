# Cortes 4 e 5 — QA de ownership e composition root

## Estado auditado

Os dois cortes já existem no package oficial:

```text
Corte 4
Local Player Camera Publication Ownership

Corte 5
Explicit Local Player Provisioning Composition Root
```

Esta entrega não altera `com.immersive.framework`. Ela adiciona QA técnico focado nos contratos que não estavam cobertos pela consolidação atual do QAFramework.

## Corte 4 — Camera publication ownership

### Authoring smoke

Execute em Edit Mode:

```text
Immersive Framework
  > QA
    > Camera
      > Cut 4 Run Local Player Camera Publication Ownership Authoring Smoke
```

Resultado esperado:

```text
[CUT4_LOCAL_PLAYER_CAMERA_PUBLICATION_OWNERSHIP_AUTHORING_SMOKE]
status='Passed'
cases='9'
```

Prova:

```text
LocalPlayerCameraRequestBinding é authoring evidence por padrão
Scene Auto-Publisher exige opt-in explícito
TryPublish sem opt-in falha explicitamente
publisher source é diagnosticável
Scene Auto-Publisher + Scene Local Player Admission do mesmo Player é rejeitado
binding evidence sem auto-publisher é aceito junto da admissão
Players distintos não são colapsados pelo validator
```

### Runtime smoke

Execute em uma sessão nova de Play Mode:

```text
Immersive Framework
  > QA
    > Camera
      > Cut 4 Run Local Player Camera Publication Ownership Runtime Smoke
```

Resultado esperado:

```text
[CUT4_LOCAL_PLAYER_CAMERA_PUBLICATION_OWNERSHIP_RUNTIME_SMOKE]
status='Passed'
cases='9'
```

Esse smoke reutiliza a lane real `P3K.7H` e prova:

```text
Player real é provisionado e admitido
PlayerGameplayAdmissionRuntimeContext é o publisher canônico
existe exatamente um request LocalPlayer no output para o Slot admitido
request e output mantêm identidade explícita
nenhum LocalPlayerCameraRequestBinding de cena publica em paralelo
release da admissão remove a publicação e o request do output
```

A lane é one-shot. Reentre em Play Mode antes de repetir.

## Corte 5 — Provisioning composition root

Execute em Edit Mode:

```text
Immersive Framework
  > QA
    > Player
      > Cut 5 Run Provisioning Composition Root Smoke
```

Resultado esperado:

```text
[CUT5_PROVISIONING_COMPOSITION_ROOT_SMOKE]
status='Passed'
cases='8'
```

Prova:

```text
authoring global sem Host Registration é ignorado
registro sem referência falha explicitamente
registro inválido bloqueia a composição
um registro resolve exatamente o authoring referenciado
registros duplicados falham
múltiplos roots de UIGlobal preservam uma única autoridade
um authoring não registrado não compete com o registrado
ausência de registro é explicitamente NotConfigured/unavailable
```

## Fora de escopo

```text
não altera o package
não muda cenas persistidas do QA
não cria fallback de discovery
não valida UX do FIRSTGAME
não duplica os smokes de arbitragem C9R
```
