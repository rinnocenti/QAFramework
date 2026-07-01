# FXX-CLOSEOUT - CONS-F - Participant Consolidation Track Closeout

## Status

Track 2 - Participant consolidation: **fechado documentalmente**.

O piloto Common Participant Executor foi aprovado para os domínios-alvo já consolidados, sem abrir autorização para expansão automática para outros domínios.

## Cortes

### CONS-A - Participant Common Primitives Alignment

Objetivo: alinhar os primitivos internos mínimos para suportar a consolidação de Participant sem migrar domínio real.

Arquivos principais criados/alterados:
- `Packages/com.immersive.framework/Runtime/Common/Participants/ParticipantRequiredness.cs`
- `Packages/com.immersive.framework/Runtime/Common/Participants/ParticipantValidation.cs`
- `Assets/_Documentation/Closeouts/FXX-CLOSEOUT-CONS-A-Participant-Common-Primitives-Alignment.md`

### CONS-B - Participant Executor Core

Objetivo: criar o executor comum interno e os tipos de execução mecânicos.

Arquivos principais criados/alterados:
- `Packages/com.immersive.framework/Runtime/Common/Participants/ParticipantExecutionEntry.cs`
- `Packages/com.immersive.framework/Runtime/Common/Participants/ParticipantExecutionIssue.cs`
- `Packages/com.immersive.framework/Runtime/Common/Participants/ParticipantExecutionIssueSeverity.cs`
- `Packages/com.immersive.framework/Runtime/Common/Participants/ParticipantExecutionResult.cs`
- `Packages/com.immersive.framework/Runtime/Common/Participants/ParticipantExecutor.cs`
- `Packages/com.immersive.framework/Runtime/Diagnostics/ParticipantExecutorSyntheticSmokeRunner.cs`
- `Assets/_Documentation/Closeouts/FXX-CLOSEOUT-CONS-B-Common-Participant-Executor.md`

### CONS-C - CycleReset ParticipantExecutor Pilot

Objetivo: migrar `CycleResetRuntime` para o executor comum interno.

Arquivos principais criados/alterados:
- `Packages/com.immersive.framework/Runtime/CycleReset/CycleResetRuntime.cs`
- `Assets/_Documentation/Closeouts/FXX-CLOSEOUT-CONS-C-CycleReset-ParticipantExecutor-Pilot.md`

### CONS-D - ObjectReset ParticipantExecutor Pilot

Objetivo: migrar `ObjectResetRuntime` para o executor comum interno e corrigir o caminho inválido de participante.

Arquivos principais criados/alterados:
- `Packages/com.immersive.framework/Runtime/ObjectReset/ObjectResetRuntime.cs`
- `Packages/com.immersive.framework/Runtime/ObjectReset/ObjectResetQaSmokeRunner.cs`
- `Assets/_Documentation/Closeouts/FXX-CLOSEOUT-CONS-D-ObjectReset-ParticipantExecutor-Pilot.md`

### CONS-F - Participant Consolidation Closeout

Objetivo: fechar o track, registrar preservações públicas, validar os pilotos e bloquear expansão automática sem ADR/corte próprio.

Arquivo criado:
- `Assets/_Documentation/Closeouts/FXX-CLOSEOUT-CONS-F-Participant-Consolidation-Track-Closeout.md`

## Executor comum: confirmação de escopo

O executor comum em `Runtime/Common/Participants` permanece:

- `internal`
- mecânico
- sem API pública nova
- sem semântica de domínio
- sem `MonoBehaviour`
- sem superfície pública dependente de UnityEngine

O papel do executor é apenas orquestrar execução participante e acumulação mecânica de resultado/issue. Ele não passa a decidir política de ciclo, objeto, snapshot, activity ou qualquer outro domínio.

## Preservação pública

As seguintes superfícies públicas foram preservadas:

- `ICycleResetParticipant`
- shells públicos de `CycleReset`
- `IObjectResetParticipant`
- shells públicos de `ObjectReset`
- enums públicos já existentes

Não houve substituição de contrato público por um tipo comum novo.

## Validação

Smokes recebidos como `PASS`:

- Participant Executor Synthetic Smoke - PASS
- Cycle Reset Runtime Host Smoke - PASS
- Cycle Reset Trigger Smoke - PASS
- Cycle Reset Bridge Smoke - PASS
- Object Reset Runtime Executor Smoke - PASS
- Object Reset Runtime Host Integration Smoke - PASS
- Object Reset Trigger Smoke - PASS
- Object Reset Bridge Smoke - PASS
- Object Reset Foundation Closure Smoke - PASS

Caso negativo esperado:

- `RejectedTargetNotFound` no Object Reset Host Integration é o caminho negativo esperado e confirma a rejeição correta de alvo ausente/rejeitado.

## Bug fix aceito

O comportamento de `ObjectReset` com participante inválido agora falha com issue explícita.

Isso foi aceito como correção necessária do piloto, sem ampliar o escopo para outros domínios.

## O que não foi migrado

O track não consolidou automaticamente:

- `Snapshot`
- `ActivityContentExecution`
- `LocalContribution`
- triggers genéricos de flow

## Decisão

- O piloto `CycleReset` / `ObjectReset` está aprovado.
- Não expandir automaticamente para `Snapshot` ou `ActivityContentExecution`.
- Qualquer nova migração exige ADR próprio e corte próprio.

## Próximo track recomendado

Recomendação de sequência:

1. `Route` / `Activity` lifecycle operation kernel, começando por `LIFECYCLE-A`.
2. `RuntimeContent` / `ContentAnchor` materialization service, se a prioridade for atacar primeiro o gargalo de materialização.

## Riscos remanescentes

- O `ParticipantExecutor` comum pode virar abstração ampla demais se novos domínios forem migrados sem ADR.
- `result/status` shells continuam fora de Common e devem permanecer assim neste estágio.
- O helper de flow trigger segue como track separado.
- A validação manual continua necessária para confirmar import/compile no Unity antes de qualquer expansão.

## Warning não bloqueante

`UnityPauseInputActionAdapter` legado ainda aparece em Play Mode e deve ser limpo em um track separado de `Pause` / `InputMode`.

## Arquivos alterados neste corte

- `Assets/_Documentation/Closeouts/FXX-CLOSEOUT-CONS-F-Participant-Consolidation-Track-Closeout.md`

