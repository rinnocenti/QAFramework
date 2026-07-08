# F49L — Player Binding Diagnostics QA

## Objetivo

Validar o reporter passivo de diagnóstico de binding de player.

## Escopo

O smoke cobre:

- summary pronta;
- summary ausente;
- topologias ausentes;
- bloqueio de view binding;
- bloqueio de control binding;
- ausência não bloqueante de participantes;
- propagação de issues;
- boundary passivo.

## Fora de escopo

Não valida:

- camera activation;
- input activation;
- movement;
- binding real;
- FIRSTGAME.

## Como rodar

1. Recriar o Hub:

```text
Immersive Framework QA > Hub > Create or Refresh Hub and Player QA Scenes
```

2. Abrir o Hub.
3. Clicar em:

```text
Player Binding Diagnostics QA
```

## Resultado esperado

```text
[F49L_PLAYER_BINDING_DIAGNOSTICS_QA] status='Succeeded'
```
