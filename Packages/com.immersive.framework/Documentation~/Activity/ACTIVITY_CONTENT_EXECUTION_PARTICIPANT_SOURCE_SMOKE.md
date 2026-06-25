# Activity Content Execution Participant Source Smoke

Status: PASS / COVERED BY F10 ACTIVITY CONTENT EXECUTION CORE PASS  
Fase: F10L  
Escopo: Framework Core / QA diagnostics

---

## Contexto

F10K introduziu `IActivityContentExecutionParticipantSource` e manteve uma source vazia por padrao. F10L valida que uma source explicita pode ser injetada temporariamente no lifecycle e que seus participants sao usados por clear/restore de Activity.

---

## Smoke adicionado

```text
Run Activity Content Execution Participant Source Smoke
```

Fluxo validado:

```text
Primary Activity ativa
  -> injeta SyntheticActivityContentExecutionParticipantSource
  -> Clear Activity
      -> source retorna 2 participants
      -> Exit executa 1 participant required
  -> Restore Primary Activity
      -> source retorna 2 participants
      -> Enter executa 1 required + 1 optional
  -> restaura EmptyActivityContentExecutionParticipantSource
```

---

## Criterios esperados

```text
clearExecution='Succeeded'
clearSource='Succeeded'
clearParticipants='2'
clearExit='Succeeded'
clearExitRequests='1'
clearExitResults='1'
clearExitRequired='1'
clearBlockingIssues='0'
clearBlocksReadiness='False'

restoreExecution='Succeeded'
restoreSource='Succeeded'
restoreParticipants='2'
restoreEnter='Succeeded'
restoreEnterRequests='2'
restoreEnterResults='2'
restoreEnterRequired='1'
restoreEnterOptional='1'
restoreBlockingIssues='0'
restoreBlocksReadiness='False'
```

---

## Fronteira preservada

F10L nao adiciona authoring/discovery real de participants. A source sintetica e local ao QA smoke.

Nao entra:

```text
GameObject.Find
FindObjectsOfType
scene scan
Presentation
Actor/Player/Camera/Pause/Input/Save consumers
Transform placement
Instantiate
Destroy
Addressables
Pooling
```

---

## Proximo passo

Depois do PASS, o proximo corte pode definir o modelo controlado de source/registry real para participants, ainda sem gameplay consumer e sem busca global.
