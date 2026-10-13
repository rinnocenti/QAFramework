# QA-IF-ADR-036 — GameplayReady estrutural e disponibilidade operacional

Prova Play Mode independente para IF-ADR-036, usando Player/Input/Pause de IF-ADR-044 e o participante Required pendente no padrão de QA-NEW-002. A QA cria GameApplication, Route, Activity B, cenas e uma cópia própria da Activity A de IF-ADR-044. A cópia A usa Fade e uma cena de conteúdo própria, permitindo restaurar o baseline sob a política de oclusão do GameFlow sem alterar o veredito ou os assets da QA-044.

## Premissas arquiteturais

Pause e Resume são rejeitados pelo contrato de Pause quando um Transition Gate está ativo. Portanto, Pause simultâneo à transição, Pause persistente enquanto B aguarda o participante e Resume ainda gated não são sequências executáveis sob a política atual. A rejeição de Pause durante B é um resultado esperado certificado pela terceira etapa. Esta QA não certifica a composição simultânea Transition Gate + Pause.

Activity B preserva `GameplayReady`, `WaitVisible`, `Fade`, `InputInteractionAndGameplay` e um participante `Required` controlado. O participante mantém a preparação pendente enquanto B expõe prontidão estrutural e disponibilidade bloqueada. Ao completar a preparação, a transição termina e a disponibilidade retorna. Os ciclos de Pause/Resume ocorrem depois que o Transition Gate foi liberado.

## Preparação e execução

1. Abra QAFramework com Unity `6000.5.0f1` e aguarde importação/compilação.
2. Configure IF-ADR-042 e IF-ADR-044 conforme `Assets/QA-IF-ADR-044/README.md`.
3. Execute **Immersive Framework → QA → IF-ADR-036 → Configure GameplayReady Transition-Pause Proof**. O setup é repetível, grava assets somente em `Assets/QA-IF-ADR-036/Generated/` e acrescenta as cenas ao Build Settings preservando as entradas existentes.
4. Selecione `Assets/QA-IF-ADR-036/Generated/Settings/GameApplication_QaIfAdr036.asset` em Project Settings → Immersive Framework.
5. Abra `Assets/QA-IF-ADR-036/Generated/Scenes/QA_IF_ADR_036_Route.unity` e inicie um Play Mode novo. Aguarde a linha terminal `[IF-ADR-036]` e capture também os checkpoints anteriores.

## Sequência e PASS

O cenário cria um teclado QA, abre Joining pela observação pública da Route, faz Join de P1 pelo `PlayerSessionJoinTrigger` e fecha Joining. Executa oito checkpoints:

1. Activity A pronta e disponível.
2. B estruturalmente pronta, participante Required ainda `Preparing`, Transition Gate ativo, disponibilidade/action map bloqueados.
3. Pause solicitado durante a transição e rejeitado pela política, Pause permanece `Running`, B continua in-flight e gated.
4. Preparação Required concluída; B fica pronta e disponível após liberação do Transition Gate.
5. Pause permitido após a transição bloqueia disponibilidade e desabilita o Action Map sem perder prontidão estrutural.
6. Resume restaura disponibilidade e Action Map.
7. Segundo Pause volta a bloquear disponibilidade preservando prontidão.
8. Resume final restaura disponibilidade.

Cada checkpoint registra `GameplayReady`, `RuntimeGameplayAvailable`, classificação de disponibilidade, estado de Pause, Action Map corrente e estado habilitado do mapa `Player`, e, quando B existe, owner/occurrence, Actor, preparation/admission/occupancy tokens e revisions, input-binding token e estado/occurrence do participante. Durante Pause o mapa corrente pode ser `Global`; o critério físico é o mapa de gameplay `Player` desabilitado. As identidades e tokens capturados enquanto B está gated são comparados depois da liberação e durante os ciclos de Pause. Mudança de Gate/Pause não pode provocar nova admission ou preparation.

PASS exige `cases='8/8'`, a rejeição de Pause durante o Transition Gate com estado inalterado, prontidão estrutural independente dos bloqueios, bloqueio operacional durante o Gate/Pause, retorno após as liberações permitidas, evidências B estáveis e `cleanup='BaselineRestored'`. `Blocked` fica reservado às precondições ausentes antes da solicitação aceita de B; divergências depois disso são `Failed`.

O contrato público não oferece snapshot isolado do Transition Gate. O cenário observa B in-flight, participante Required pendente e disponibilidade/action map bloqueados; a classificação `BlockedByGate` não identifica sozinha qual Gate causou o bloqueio. `PlayerGameplayInputReader` e a observação por Slot usados como evidência são Experimental na versão resolvida.

## Cleanup e limites

Cleanup completa o participante Required se ainda estiver pendente, aguarda B sair de in-flight e aplica Resume somente depois que o Gate terminou. Se B é a Activity atual, solicita Leave pelo acesso scoped de B enquanto a representação contextual, o token de preparação e a admission de B ainda pertencem à ocorrência ativa; em seguida restaura Activity A vazia por Fade. Se B não se tornou atual, Leave usa a Activity que já está ativa. Fecha Joining, remove o teclado QA e exige Session vazio, Activity A ativa e Pause em Running. Falha de restauração informa `FreshBootRequired`, separada da primeira divergência causal.

Não existe nesta prova evidência de composição simultânea Transition Gate + Pause: a política vigente rejeita a requisição de Pause nessa condição. Cobrir simultaneidade exigiria outra política/contrato aprovado pelo Framework. A validação de import/compilação e Play Mode permanece manual; não foi executada neste ambiente.

## Verificação manual

- Confirme Persistent, Route, Activity A e Activity B no Build Settings.
- Execute em Play Mode novo com Scene Reload ativo; verifique as oito linhas `checkpoint=` e o terminal `cases='8/8' cleanup='BaselineRestored'`.
- Confirme no terceiro checkpoint `pauseRequestStatus='Rejected'`, Pause `Running`, request B in-flight, GameplayReady verdadeiro e disponibilidade falsa.
- Confirme a estabilidade de tokens/revisions de B e repita em outro Play Mode novo.
