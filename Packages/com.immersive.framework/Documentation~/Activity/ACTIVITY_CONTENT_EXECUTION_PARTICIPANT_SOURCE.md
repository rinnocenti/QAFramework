# Activity Content Execution Participant Source

Status: PASS / PARTICIPANT SOURCE BOUNDARY / COVERED BY F10 CORE PASS  
Fase: F10K-F10M  
Escopo: Framework Core

---

## Contexto

F10B-F10J criaram contratos, collection, phase plan, executor runtime, smoke sintetico e integracao diagnostica no `ActivityFlowRuntime` usando collection vazia por padrao.

F10K substitui o vazio hardcoded por uma fronteira explicita de source:

```text
ActivityFlowRuntime
  -> IActivityContentExecutionParticipantSource
      -> ActivityContentExecutionParticipantSourceResult
          -> ActivityContentExecutionParticipantCollection
```

---

## Contratos adicionados

```text
ActivityContentExecutionParticipantSourceRequest
ActivityContentExecutionParticipantSourceStatus
ActivityContentExecutionParticipantSourceResult
IActivityContentExecutionParticipantSource
EmptyActivityContentExecutionParticipantSource
```

---

## Responsabilidade

A source boundary define como o lifecycle recebe participants conhecidos para uma transicao de Activity.

Ela carrega:

```text
route
previousActivity
nextActivity
source
reason
```

E retorna:

```text
collection de participants
status diagnostico
participant count
issue count
mensagem
```

---

## Fronteira

F10K nao descobre objetos fisicos e nao varre a cena.

A source deve receber participants por caminho explicito futuro. Ela nao pode depender de:

```text
GameObject.Find
FindObjectsOfType
service locator
global registry gameplay-facing
reflection de cena
gameplay-specific assumptions
```

---

## Source vazia padrao

Enquanto nao existir authoring/discovery de participants, o `ActivityFlowRuntime` usa:

```text
EmptyActivityContentExecutionParticipantSource
```

Resultado esperado:

```text
activityContentExecutionParticipantSource='SucceededNoParticipants'
activityContentExecutionParticipants='0'
activityContentExecution='SucceededNoContent'
```

Isso preserva o comportamento de F10I/F10J: a etapa existe no lifecycle, mas nao executa conteudo real.

---

## Nao responsabilidade

F10K nao adiciona:

```text
participant authoring
participant discovery fisico
ActivityFlow gameplay integration
Transform placement
GameObject hierarchy root
Instantiate
Destroy
Addressables
Pooling
Presentation
Actor/Player/Camera/Pause/Input/Save consumers
```

---

## Smoke relacionado

F10L adiciona `Run Activity Content Execution Participant Source Smoke`, validando uma source sintetica explicita injetada temporariamente no lifecycle.

## Proximo passo

Depois do PASS, o proximo corte pode definir como uma source real/controlada sera alimentada, ainda sem gameplay consumer e sem busca global.
