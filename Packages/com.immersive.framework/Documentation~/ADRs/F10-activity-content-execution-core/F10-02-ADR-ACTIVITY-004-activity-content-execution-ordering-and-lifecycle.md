# F10-02 — ADR-ACTIVITY-004 — Activity Content Execution Ordering and Lifecycle

Status: Accepted / Implemented by F10B-F10L / F10 closed  
Fase: F10  
Ordem no Plano: F10-02  
Tipo: Activity / Lifecycle Ordering  
Escopo: Framework Core

---

## Contexto

F8 criou RuntimeContent roots/context/handles e release logico. F9 criou ContentAnchor binding logico, host ownership e cleanup automatico em Route/Activity exit.

F10 deve usar essas fronteiras sem reabrir F8/F9 e sem criar materializacao fisica. O problema a resolver e a posicao da execucao de conteudo de Activity dentro do lifecycle.

## Decisao

A entrada futura de Activity deve seguir esta ordem conceitual:

```text
Activity request accepted
  -> Activity RuntimeContent root/context available
  -> Activity Content Anchor discovery available
  -> Activity content enter execution
  -> Activity readiness aggregation
  -> Activity active/ready result
```

A saida futura de Activity deve seguir esta ordem conceitual:

```text
Activity exit requested
  -> Activity content exit execution
  -> logical Content Anchor binding cleanup
  -> logical RuntimeContent release/unregister when policy requires
  -> Activity RuntimeContent root removal
  -> Activity exited result
```

F10 nao deve transformar essa ordem em uma pipeline monolitica. Cada etapa deve permanecer diagnosticavel, substituivel por contrato e alinhada ao owner/scope atual.

## Relacao com F8/F9

F10 usa:

```text
RuntimeContentOwner(Activity)
RuntimeScopeContext(Activity)
RuntimeContentHandle
RuntimeReleasePolicy
ContentAnchorBinding cleanup
ActivityContentAnchorSet
```

F10 nao muda:

```text
RuntimeContent identity model
RuntimeRootRegistry semantics
ContentAnchor identity model
RuntimeContentAnchorBinding ownership
F9 cleanup policy
```

## Regras de ordenacao

- Enter execution ocorre depois de existir Activity runtime context.
- Enter execution pode contribuir readiness.
- Exit execution ocorre antes de remover o root logico da Activity.
- Binding cleanup continua ocorrendo antes da remocao do owner/root antigo.
- Release fisico continua fora do core.
- Falha em exit deve ser diagnosticada sem destruir recursos fisicos por fallback.

## Fora do escopo

- Scene loading concrete execution.
- Prefab materializer.
- Addressables adapter.
- Transform placement.
- Physical hierarchy root.
- Presentation consumer.
- Actor/Player/Camera/Pause/Input/Save consumers.
- Reset/snapshot restore.

## Consequencias

### Positivas

- Define ordem previsivel para entrada/saida de conteudo.
- Evita que consumers futuros chamem lifecycle fora de ordem.
- Mantem F8/F9 como fronteiras logicas estaveis.

### Trade-offs

- Exige tipos de result/diagnostics futuros.
- Exige cuidado para nao duplicar readiness existente.
- Pode exigir smoke de ordering antes de qualquer adapter fisico.

## Validacao esperada de F10

F10B-F10L validaram:

- enter execution ocorre com Activity context disponivel quando source explicita fornece participants;
- exit execution ocorre durante clear de Activity antes do root removal final;
- cleanup logico de bindings permanece preservado;
- root removal nao ocorre com handles vivos nao liberados quando policy exige release/unregister;
- diagnostics de ordering aparecem em Activity Request e QA smoke;
- nenhuma operacao fisica Unity e executada pelo core.
