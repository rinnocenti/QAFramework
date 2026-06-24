# F0A-06 - ADR-BL-006 - Minimal Bootstrap and Incremental Construction

Status: Accepted
Fase: F0A
Ordem no Plano: F0A-06
Tipo: Baseline / Bootstrap
Escopo: Package atual

---

## Contexto

`com.immersive.framework` deve nascer como package Unity pequeno, incremental e separado dos packages tecnicos congelados:

```text
com.immersive.foundation
com.immersive.logging
com.immersive.pooling
```

Esses packages podem fornecer primitives tecnicas, mas nao devem possuir lifecycle de jogo, Session, Route, Activity, Actor, Input, Camera, Save ou diagnostics especificos do framework.

Base 2.0 / `NewScripts` e referencia arquitetural e fonte de aprendizados, nao skeleton a ser copiado.

## Decisao

O framework deve iniciar com bootstrap minimo e construcao incremental.

Nao criar antecipadamente:

- module graph completo;
- registries genericos;
- descriptors extensivos;
- pipelines vazios;
- adapters sem uso;
- hosts globais;
- managers preventivos;
- assets guarda-chuva para dominios ainda inexistentes.

Criar apenas o que o corte atual usa.

## UX publica

Conceitos publicos devem fazer sentido no Inspector:

| Conceito | Papel |
|---|---|
| `Game Application` | asset raiz do jogo/aplicacao |
| `Immersive Framework Settings` | configuracao de projeto que aponta para a aplicacao ativa |
| `Validation Mode` | severidade de validacao |
| `Startup Route` | primeira Route, quando Route existir |
| `Game Flow` | fluxo macro, quando existir |
| `Activity Flow` | fluxo de Activities, quando existir |
| `Diagnostics` | informacoes de validacao e boot |

Termos como `Bootstrap`, `Composition`, `Pipeline`, `Stage`, `Command`, `Fact`, `Snapshot`, `Registry`, `Installer`, `Host`, `Runtime Config Set` e `Descriptor` podem existir internamente quando necessarios, mas nao devem ser o fluxo principal de authoring.

## Bootstrap minimo

O bootstrap inicial pode:

- resolver o `Game Application` ativo;
- validar configuracao minima;
- aplicar logging minimo necessario;
- criar contexto runtime minimo;
- produzir diagnostics basicos de boot;
- iniciar o primeiro owner real de lifecycle quando esse owner existir.

O bootstrap nao decide diretamente:

- carregamento de cena;
- Route lifecycle;
- Activity lifecycle;
- Actor lifecycle;
- input;
- camera;
- save;
- lifetime de pools;
- pause;
- gameplay.

Essas decisoes pertencem a owners especificos, criados somente quando suas responsabilidades forem necessarias.

## Validation Mode

`Validation Mode` e o conceito publico para severidade de validacao.

Modos previstos:

```text
Strict
Standard
Release
```

Nenhum modo permite fallback silencioso para configuracao obrigatoria ausente. Configuracao obrigatoria ausente continua sendo erro.

## Consequencias

Beneficios:

- menor superficie inicial;
- menor chance de duplicacao;
- Inspector mais claro;
- owners de lifecycle mais rastreaveis;
- menor risco de managers globais.

Custos aceitos:

- conceitos futuros podem nascer depois;
- refactors iniciais sao esperados;
- nem toda extensao futura precisa existir no skeleton inicial.

## Criterios de aceite

O ADR e respeitado se o package:

- nao criar o framework completo antecipadamente;
- nao expor pipeline/stage/command/fact/snapshot no Inspector como authoring principal;
- usar `Game Application` como raiz publica;
- usar `Immersive Framework Settings` como configuracao de projeto;
- usar `Validation Mode` como conceito publico;
- mantiver bootstrap como detalhe interno;
- nao copiar a estrutura da Base 2.0;
- preservar fail fast para configuracao obrigatoria ausente;
- nao introduzir fallback silencioso.

## Relacao com roadmap

Este ADR pertence ao baseline F0A. Ele nao cria roadmap paralelo e nao autoriza F10.

