# QA-READY-PROGRESS-02A — Terminal Paths and Shared Envelope Parity

Data: 2026-08-04  
Tipo: technical QA / negative regression  
Baseline local obrigatório: `QA-READY-PROGRESS-01 + FIX1 + FIX2`  
Package esperado: `IF-READY-PROGRESS-03`

## Objetivo

Fechar os terminais que podem ser provados sem criar um segundo host implícito:

- falha real de participante Required em request direto;
- Required Released no envelope;
- occurrence de substituição rejeitada;
- conclusão tardia de occurrence antiga rejeitada;
- observação terminal duplicada idempotente;
- cancelamento por owned-operation unwind;
- assinatura compartilhada dos wrappers Direct Activity, Route Startup Activity e Game Application Startup Activity.

## Decisão de corte

O QA público atual não possui uma fixture canônica de startup que permita iniciar um segundo `GameApplication` isolado sem disputar:

- o host persistente;
- UIGlobal;
- Loading/Transition oficiais;
- cenas persistentes e authority do QA Hub.

Por isso o Q2 foi dividido:

```text
Q2A
  terminais reais do request direto
  + envelope compartilhado
  + ownership causal
  + guard de assinatura dos três wrappers

Q2B
  Route Startup Activity end-to-end
  + Game Application Startup Activity end-to-end
  usando fixture de startup escopada e canônica
```

Q2A não afirma que os dois startups end-to-end passaram.

## Superfície criada

Menu:

```text
Immersive Framework
  > QA
    > Regressions
      > Game Flow
        > Run Participant-Aware Readiness Loading Terminal Regression
```

Arquivo:

```text
Assets/ImmersiveFrameworkQA/GameFlow/InternalEditor/
  QaParticipantAwareReadinessLoadingTerminalRegression.cs
```

## Prova runtime direta

O regression cria:

```text
WaitCovered
FadeWithLoading
InputInteractionAndGameplay
4 Required
1 Optional
```

Fluxo:

```text
request direto inicia
→ todos entram Preparing
→ um Required entra Failed
→ request termina como FailedCommittedTargetNotReady
→ destino permanece authoritative
→ snapshot registra 1 Required Failed
→ último progresso permanece abaixo de 1
→ nenhum Update publica 100%
→ Loading permanece visível
→ Transition permanece coberta
→ recovery gate permanece ativo
→ cleanup limpa Activity
→ participantes são Released
→ superfícies são escondidas explicitamente
→ gate e authority inicial são restaurados
```

## Matriz isolada do envelope

O mesmo `ActivityEntryLoadingProgressEnvelope` é exercitado com os rótulos:

```text
DirectActivity
RouteStartupActivity
GameApplicationStartupActivity
```

A matriz prova:

```text
Required Failed não publica 100%;
Required Released não publica 100%;
replacement occurrence não avança o envelope capturado;
late old occurrence não avança o envelope atual;
duplicate terminal não cria reports adicionais.
```

Isso prova a semântica compartilhada do envelope, não a execução end-to-end dos dois startups.

## Ownership causal

`QaOwnedAsyncOperation<T>` é exercitado com cancelamento durante `UnwindAsync`:

```text
operation exists;
completion callback is issued;
terminal is reached;
cancellation is typed;
duplicate AwaitTerminalAsync reuses the same terminal task.
```

## Restrições preservadas

```text
sem Task.Delay;
sem timeout automático;
sem frame polling;
sem parsing de logs;
sem FindObjectOfType;
sem busca global de componentes;
sem reflection;
sem alteração do package;
sem alteração de cenas, prefabs, assets ou ProjectSettings;
sem scripts de aplicação no ZIP.
```

A única busca de componentes percorre a cena persistente pertencente ao host oficial já resolvido.

## Validação Unity requerida

1. Copiar os arquivos completos para os caminhos indicados.
2. Import/compile com zero erros.
3. Executar Foundation: 20 casos.
4. Executar QA-01: 18 casos.
5. Executar QA-02: 26 casos.
6. Executar QA-03: 42 casos.
7. Executar QA-READY-PROGRESS-01: 32 casos.
8. Executar QA-READY-PROGRESS-02A: 34 casos.
9. Confirmar no final:

```text
status='Passed'
cases='34'
Loading hidden
Transition hidden
TransitionGateSnapshot.HasBlockers == false
Route/Activity authority restored
```

## Fora de escopo

```text
Route Startup Activity end-to-end;
Game Application Startup Activity end-to-end;
fixture de segundo host;
FIRSTGAME;
retry UI;
continuous participant progress.
```

## Próximo corte

```text
QA-READY-PROGRESS-02B
  Canonical Scoped Startup Fixture and End-to-End Startup Parity
```

## Commit sugerido

```text
test(qa): cover readiness progress terminal paths
```
