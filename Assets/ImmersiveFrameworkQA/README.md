# Immersive Framework QA Assets

Esta pasta contem assets de QA manual para os smokes do Immersive Framework.

Esses assets nao sao assets de producao.
Eles existem apenas para gerar logs padronizados durante validacao manual e desenvolvimento.

O `Active Game Application` normal do projeto deve permanecer apontado para o `Game Application` do projeto, nao para um asset QA.
Se voce criar uma cena QA dedicada, os `ActivityContentBinding` dessa cena devem apontar para activities QA, nao para activities de gameplay. A `StartupScene` do projeto permanece como cena normal de produto/dev.

Papeis de teste:

- `QA_CanonicalRoute`
- `QA_AlternateRoute`
- `QA_NoActivityRoute`
- `QA_PrimaryContentActivity`
- `QA_SecondaryContentActivity`
- `QA_NoContentActivity`

Esses nomes representam papeis de teste, nao nomes de gameplay.

Use estes assets como referencia dedicada do `FrameworkQaCanvas`.


## Smokes recomendados

Configure o `FrameworkQaCanvas` com estes papeis semanticos:

- Canonical Route: `QA_CanonicalRoute`
- Alternate Route: `QA_AlternateRoute`
- No-Activity Route: `QA_NoActivityRoute`
- Primary Activity: `QA_PrimaryContentActivity`
- Secondary Activity: `QA_SecondaryContentActivity`
- No-Content Activity: `QA_NoContentActivity`

Esses campos sao alvos de cenario. Eles nao devem substituir o `Active Game Application` normal do projeto.

O smoke de Activity Content positivo so deve ser usado em uma cena QA ou em uma cena que tenha `ActivityContentBinding` apontando explicitamente para uma Activity QA. Em cenas normais, e esperado que uma Activity QA sem binding correspondente deixe o conteudo inativo.

## Reset baseline

O `FrameworkQaCanvas` possui o botao `Reset QA Scenario` para voltar o Play Mode a um baseline sem parar o Player e sem limpar o Console.

O reset pode usar campos explicitos no Canvas:

- Reset Route
- Reset Activity
- Reset Reason

Se esses campos estiverem vazios, o Canvas tenta voltar para o `Startup Route` do `Active Game Application` normal do projeto e para a `Startup Activity` dessa rota. Assim os smokes podem ser repetidos no mesmo Play Mode sem transformar os assets QA em boot paralelo.

