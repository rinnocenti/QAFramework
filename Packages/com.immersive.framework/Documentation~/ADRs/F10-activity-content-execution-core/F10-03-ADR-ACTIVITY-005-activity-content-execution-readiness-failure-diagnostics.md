# F10-03 — ADR-ACTIVITY-005 — Activity Content Execution Readiness Failure Diagnostics

Status: Accepted / F10 planning only / implementation not started  
Fase: F10  
Ordem no Plano: F10-03  
Tipo: Activity / Readiness / Diagnostics  
Escopo: Framework Core

---

## Contexto

Activity lifecycle ja possui readiness baseline. F10 adicionara um novo eixo logico: conteudos podem participar da entrada/saida da Activity e contribuir para readiness.

Sem uma semantica minima de required/optional, falha e diagnostico, cada consumer futuro tendera a inventar sua propria regra de bloqueio.

## Decisao

Activity content execution deve suportar, no minimo, contribuicoes diagnosticaveis de readiness.

Cada participacao futura de content execution deve poder declarar ou resolver:

```text
requiredness: Required | Optional
enter status
exit status
blocking issue count
non-blocking issue count
source
reason
owner/scope/content identity
message diagnostic
```

Falhas devem ser classificadas como:

```text
blocking
non-blocking
skipped
not-applicable
```

Conteudo `Required` que falha em enter pode bloquear readiness da Activity. Conteudo `Optional` pode gerar diagnostic issue sem bloquear readiness, conforme policy.

## Diagnosticos minimos

F10 deve preparar diagnostics para responder:

```text
qual conteudo tentou entrar
qual conteudo tentou sair
qual conteudo foi skipped
qual conteudo falhou
qual falha bloqueou readiness
qual falha foi opcional
qual owner/scope originou a contribuicao
qual source/reason disparou a execucao
```

## Separacao de responsabilidades

Framework Core define status, aggregation e diagnostics.

Framework Core nao executa:

```text
GameObject mutation
Transform reset
Animator reset
camera blend
UI concrete show/hide
player/actor state mutation
physical release
```

Consumers/adapters futuros interpretam a execucao concreta e retornam resultados ao core.

## Relacao com Reset

Reset nao pertence a F10. F10 pode registrar que um conteudo participou de enter/exit. Reset Foundation futura definira request/result/policy de reset logico e depois adapters/consumers poderao implementar reset fisico.

## Relacao com Transition/Loading

Transition/Loading nao pertence a F10. F10 pode produzir status de readiness que uma fase futura de Transition/Loading venha a observar. A tela de loading/fade/progress concreta permanece fora do core atual.

## Consequencias

### Positivas

- Readiness fica auditavel.
- Required/optional ganha semantica comum.
- Consumers futuros nao precisam criar regras paralelas de readiness.

### Trade-offs

- Exige um modelo de result antes de adapter fisico.
- Pode aumentar logs se diagnostics nao forem compactos.
- Precisa preservar o QA Canvas como smoke de framework, nao gameplay.

## Validacao esperada de F10

Quando implementado, F10 deve validar:

- required content enter failure bloqueia readiness conforme policy;
- optional content enter failure nao bloqueia readiness por padrao;
- skipped content aparece como skipped e nao como sucesso silencioso;
- diagnostics agregam counts e status sem vazar detalhes de gameplay;
- exit failures sao diagnosticadas;
- nenhum GameObject/Transform/Animator/Addressables/Pooling e acionado pelo core.
