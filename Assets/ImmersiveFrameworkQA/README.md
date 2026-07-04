# Immersive Framework QA Project

Este projeto prova que o framework funciona tecnicamente. Ele nao e FIRSTGAME e nao deve virar exemplo de jogo final.

Root canonico de QA:

```text
Assets/ImmersiveFrameworkQA/
```

Estrutura canonica do QA Project:

```text
Assets/ImmersiveFrameworkQA/
  README.md
  Documentation/
  Scenes/
  GameApplications/
  Routes/
  Activities/
  Prefabs/
  Materials/
  Scripts/
  UI/
  SmokeData/
```

A documentacao canonica do framework fica no package:

```text
Packages/com.immersive.framework/Documentation~/
```

Este README descreve apenas o uso local do projeto QA.

O package `com.immersive.framework` continua sendo o dono de documentacao canonica, ADRs, roadmap e guias oficiais. Este projeto pode manter relatorios locais de QA, mas nao deve ser o registro canonico da arquitetura do framework.

## Proposito

- concentrar smokes sinteticos;
- manter cenas artificiais de validacao;
- guardar probes tecnicos, casos negativos e botoes QA;
- validar regressao sem depender de um jogo real.

O QA pode usar nomes como `QA_`, `Smoke`, `Probe` e `Synthetic` quando eles representam um cenario tecnico.

FIRSTGAME nao deve entrar neste projeto. Assets de jogo final, scripts de gameplay final e fluxos jogaveis reais pertencem ao consumidor FIRSTGAME, nao ao QA sintetico.

## Onde ficam os smokes

Smokes e superficies QA devem ficar sob:

```text
Assets/ImmersiveFrameworkQA/
```

Superficie atual principal:

```text
Assets/ImmersiveFrameworkQA/UnityBuildSurface/
```

`UnityBuildSurface/` permanece como superficie QA existente ate uma migracao futura decidir se seus assets devem ser promovidos para as pastas canonicas de topo.

O `Active Game Application` normal do projeto deve permanecer apontado para o asset usado pelo cenario em validacao. Se uma cena QA dedicada usa `ActivityContentBinding`, os bindings devem apontar para activities QA, nao para assets finais de FIRSTGAME.

## Cenas principais

- `Scenes/StartupScene.unity`
- `Scenes/SecondScene.unity`
- `Scenes/AdditionalRouteScene.unity`
- `UnityBuildSurface/Scenes/UnityBuildSurfaceQA.unity`
- `UnityBuildSurface/Scenes/QA_UIGlobal.unity`
- `UnityBuildSurface/Scenes/TransitionRouteA.unity`
- `UnityBuildSurface/Scenes/TransitionRouteB.unity`
- `UnityBuildSurface/Scenes/ActivityAdditionalContent.unity`

## Papeis de teste

- `QA_CanonicalRoute`
- `QA_AlternateRoute`
- `QA_NoActivityRoute`
- `QA_PrimaryContentActivity`
- `QA_SecondaryContentActivity`
- `QA_NoContentActivity`

Esses nomes representam papeis de teste, nao nomes de gameplay.

## Smokes recomendados

Configure o `FrameworkQaCanvas` com estes papeis semanticos:

- Canonical Route: `QA_CanonicalRoute`
- Alternate Route: `QA_AlternateRoute`
- No-Activity Route: `QA_NoActivityRoute`
- Primary Activity: `QA_PrimaryContentActivity`
- Secondary Activity: `QA_SecondaryContentActivity`
- No-Content Activity: `QA_NoContentActivity`

O smoke de Activity Content positivo so deve ser usado em uma cena QA ou em uma cena que tenha `ActivityContentBinding` apontando explicitamente para uma Activity QA.

## Reset baseline

O `FrameworkQaCanvas` possui o botao `Reset QA Scenario` para voltar o Play Mode a um baseline sem parar o Player e sem limpar o Console.

O reset pode usar campos explicitos no Canvas:

- Reset Route
- Reset Activity
- Reset Reason

Se esses campos estiverem vazios, o Canvas tenta voltar para o `Startup Route` do `Active Game Application` normal do projeto e para a `Startup Activity` dessa rota.

## O que nao deve entrar aqui

- assets finais de FIRSTGAME;
- scripts de jogo final;
- cenas jogaveis usadas como produto;
- documentacao historica/canonica do framework;
- sistemas de gameplay que nao existem para validar o framework.

## Documentation Policy

- documentacao canonica do framework fica no package `Packages/com.immersive.framework/Documentation~/`;
- este projeto mantem so documentacao operacional de QA;
- documentacao legada em `Assets/_Documentation` nao deve voltar;
- novos smokes devem ser documentados em `Assets/ImmersiveFrameworkQA/Documentation`.

## Diferenca para FIRSTGAME

```text
QA prova comportamento tecnico com cenarios sinteticos.
FIRSTGAME prova que o framework e utilizavel para iniciar um jogo real minimo.
```

Se um arquivo deixar de ser probe/smoke e virar exemplo final de jogo, ele deve migrar para FIRSTGAME por um corte proprio e com cuidado de serializacao Unity.

## Separation State

`Assets/_Project` foi removido do QA Project apos a migracao controlada B6B-B6F.

`Assets/_Documentation` foi removido em `POST-RESET-B5 - QA Legacy Documentation Removal` e nao deve ser recriado.

Novos assets, smokes e notas operacionais QA devem permanecer sob `Assets/ImmersiveFrameworkQA/`.
