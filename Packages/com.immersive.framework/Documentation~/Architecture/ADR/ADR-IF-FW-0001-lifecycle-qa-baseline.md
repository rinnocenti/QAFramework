# ADR-IF-FW-0001 - Lifecycle QA Baseline

## Status

Accepted

## Data

2026-06-19

## Contexto

O `com.immersive.framework` chegou a um baseline funcional e arquitetural inicial com:

- application boot via `Active Game Application`;
- `Startup Route`;
- load da `Primary Scene`;
- `Startup Activity`;
- request de Activity;
- clear de Activity;
- request e switch de Route;
- `ActivityContentBinding`;
- eventos de Route e Activity usando `Foundation.Events`;
- `FrameworkQaCanvas` como superficie manual de QA;
- assets em `Assets/ImmersiveFrameworkQA` como `scenario targets`, nao como boot paralelo;
- `Reset QA Scenario` para repetir smokes no mesmo Play Mode sem limpar o Console.

Esse baseline precisa ficar registrado para evitar regressao de conceito: o framework continua bootando pela aplicacao normal do projeto, enquanto os assets QA servem apenas como alvos semanticamente nomeados para smokes manuais.

## Decisao

1. O `FrameworkQaCanvas` e a superficie manual de validacao do framework.
2. Os assets QA sao `scenario targets`, nao uma aplicacao paralela.
3. O `Active Game Application` continua sendo o `Game Application` normal do projeto.
4. `Reset QA Scenario` retorna ao baseline sem reiniciar o Play Mode.
5. Smokes manuais continuam aceitos como validacao inicial enquanto nao houver suite automatizada.
6. Logs canonicos emitidos por `FrameworkLogger` / `com.immersive.logging` sao a evidencia de validacao.

## Consequencias

- Smokes podem ser executados repetidamente na mesma sessao de Play Mode.
- O Console nao precisa ser limpo entre execucoes.
- Os cenarios de QA ficam nomeados semanticamente.
- O QA nao polui o lifecycle runtime.
- O framework continua sem UGUI, Actor, Input, Camera, Save ou Pooling neste baseline.
- Futuras features devem preservar esse modelo ou registrar um novo ADR.

## Smoke Matrix

### Boot Smoke

Espera boot em `Startup Route` + `Startup Activity` normal.

### Negative Smoke

`Clear Activity` -> `Clear Activity` sem Activity ativa.

### Activity Smoke

`Secondary Activity` -> `Primary Activity` -> `Clear` -> `Primary Activity`.

### Route Smoke

`Alternate Route` -> `Canonical Route`.

### Clear Activity Smoke

`Clear` -> `Primary Activity` -> `Clear`.

### No-Activity Route Smoke

Route sem `Startup Activity`.

### No-Content Activity Smoke

Activity sem `ActivityContentBinding` correspondente.

### Reset QA Scenario

Volta para `Reset Route` + `Reset Activity`, normalmente `Startup Route` + `Primary Activity`.

## Consequencias de manutencao

Este ADR permanece valido enquanto o modelo de QA manual do framework seguir as regras acima. Se o fluxo de QA passar a depender de outro owner, de outro ponto de boot ou de outro tipo de alvo, a mudanca deve registrar um novo ADR antes de alterar a documentacao basica.


## Amendment — IF-FW-2X QA Smoke Result Semantics

O `FrameworkQaCanvas` deve emitir `QA Smoke completed` somente quando a sequencia observar os resultados esperados das requests runtime.

Regras:

- request de Route em passo de mudanca exige `Succeeded`;
- request de Activity em passo de mudanca exige `Succeeded`;
- `Reset QA Scenario` pode aceitar `IgnoredAlreadyActive` quando o baseline ja estiver ativo;
- clears usados como preparacao podem aceitar `Succeeded` ou `IgnoredNoActiveActivity`;
- `Negative Smoke` exige que o segundo clear observe `IgnoredNoActiveActivity`;
- qualquer outcome inesperado deve gerar `QA Smoke aborted`, nao `QA Smoke completed`.

Essa mudanca nao altera o lifecycle runtime. Ela apenas endurece a evidencia manual de QA para evitar falso PASS.


## Amendment — IF-FW-2Y Authoring Validation Baseline

O framework passa a ter uma validação autoral editor-only para o baseline atual.

Escopo validado:

- `Active Game Application` em Project Settings;
- `Startup Route` no Game Application;
- `Primary Scene` na Route;
- `Startup Activity` opcional;
- `ActivityContentBinding` nas cenas abertas, incluindo Activity ausente e nesting.

Regras:

- validação autoral não altera lifecycle runtime;
- validação autoral não cria pipeline, stage, registry, manager ou service locator;
- logs explícitos do botão `Validate Authoring` usam `FrameworkLogger` / `com.immersive.logging`;
- Inspectors podem mostrar o mesmo diagnóstico sem emitir log a cada repaint;
- Actor, Input, Camera, Save, Pooling e UGUI permanecem fora do baseline.

Esse corte fortalece o uso do framework antes de novos domínios runtime, mantendo o modelo de QA/smoke aceito.
