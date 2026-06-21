# ADR-0005 — Content Materialization Freeze e plano top-down de escopos

## Status

Accepted.

## Contexto

A evolução recente mostrou um risco de arquitetura reativa: câmera revelou limites do modelo de conteúdo local, então a implementação começou a avançar em Route materialization antes de fechar a leitura geral de escopos.

O framework já possuía escopos gerais claros:

```text
Session
Route
Activity
Local
```

A direção correta é evoluir de cima para baixo, criando uma base mínima e homogênea por escopo antes de aprofundar qualquer subsistema ou execução específica.

## Decisão

Congelar o baseline atual de materialização em:

```text
IF-FW-4C — ContentFlow Core + Route Content Set Baseline
IF-FW-4D — Route Content Profile Planning Baseline
```

Ambos ficam aceitos com `COMPILE PASS`. Smoke é opcional porque esses cortes adicionam linguagem, authoring e diagnóstico, mas ainda não executam composição nova de cenas.

A execução additive de Route scenes fica deferida. O corte gerado como `IF-FW-4E — Route Scene Composition Execution` não faz parte do baseline aceito e não deve ser aplicado como próximo passo arquitetural.

## Regras congeladas

- `ContentFlow` é a linguagem comum para materialização.
- Conteúdo não é sinônimo de `GameObject.SetActive`.
- `RouteContentBinding` e `ActivityContentBinding` são adapters locais simples, não materialização canônica.
- Route deve ser resolvida antes de Activity, mas sem aprofundar Route enquanto Session/Activity/Local ainda não têm baseline mínimo.
- CameraFlow, AudioFlow, Pause, Actor, Presentation e Save ficam fora do trilho atual.
- Não manter fallback, trilho fantasma ou adapter torto para subsistemas estruturais.
- Não copiar a pipeline antiga do `NewScripts`; usar seus padrões como referência conceitual.

## Escopos canônicos

| Escopo | Responsabilidade | Exemplos futuros |
|---|---|---|
| Session | Conteúdo persistente da sessão/framework runtime. | output camera rig, audio listener authority, runtime roots, input/session services. |
| Route | Conteúdo que vive enquanto a Route está ativa. | primary scene, route scenes, route surfaces, route-level UI, pause surface. |
| Activity | Conteúdo que vive enquanto a Activity está ativa dentro da Route. | activity scenes, activity prefabs, activity actors, reset groups, presentation content. |
| Local | Conteúdo authored leve já presente no contexto. | painéis pequenos, props locais, triggers, mocks de QA. |

## Próxima sequência planejada

```text
IF-FW-4E-R1 — Content Scope Sets Baseline
IF-FW-4F — Runtime Root Scope Baseline
IF-FW-4G — Local Binding Reclassification
IF-FW-4H — Route Materialization Execution
IF-FW-4I — Activity Materialization Baseline
IF-FW-4J — Contribution Discovery Baseline
```

A numeração `4E-R1` indica substituição do antigo `4E` gerado cedo demais.

## Consequências

Benefícios:

- evita avanço reativo guiado por câmera/áudio;
- estabiliza a linguagem de materialização antes dos subsistemas;
- preserva o que já compila em 4C/4D;
- prepara uma evolução top-down sem jogar fora os aprendizados do `NewScripts`.

Custos aceitos:

- Route additive scene composition fica adiada;
- câmera e áudio ficam parados;
- Activity content real ainda não entra;
- os bindings locais continuam existindo como adapters simples até reclassificação formal.

## Critério de fechamento

Este freeze está respeitado se:

- o package compila com 4C/4D;
- nenhum arquivo de CameraFlow/Cinemachine é reintroduzido como trilho canônico;
- o antigo `4E` route-additive não é aplicado;
- os docs indicam explicitamente que o próximo passo é resolver escopos, não executar Route additive;
- `ActivityContentBinding` não é descrito como content materialization canônica.
