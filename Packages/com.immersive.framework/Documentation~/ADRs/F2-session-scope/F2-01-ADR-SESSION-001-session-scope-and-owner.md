# F2-01 — ADR-SESSION-001 — Session Scope and Owner

Status: Draft / Deferred  
Fase: F2  
Ordem no Plano: F2-01  
Tipo: Session  
Escopo: Session runtime

---

## Contexto

O package tem `FrameworkRuntimeHost`, mas ainda não formaliza Session como scope de conteúdo, estado e ownership.

## Decisão

`FrameworkRuntimeHost` deve ser tratado como owner inicial da Session runtime. Ele não deve virar service locator.

Session deve possuir:

- active application;
- lifecycle status;
- startup route state;
- session diagnostics;
- `SessionContentSet` mínimo;
- boundaries para future persistent content.

Session não deve possuir diretamente camera/audio/input/actor concretos.

## Consequências

### Positivas

- Formaliza o topo do lifecycle.
- Prepara persistent content.
- Mantém composition explícita.

### Negativas / trade-offs

- Pode exigir ajuste de `FrameworkRuntimeState`.
- Alguns consumers precisarão esperar contexts futuros.

## Fora do escopo

- Service registry público.
- Player participation.
- Persistent subsystem hosts concretos.

## Critérios de validação

- Existe `SessionRuntimeState` ou equivalente.
- Host é owner claro da Session.
- Não há novo service locator público.

## Impacto esperado

Destrava `SessionContentSet` e ownership.

## Relação com roadmap

F2.

## Notas de implementação

O host pode continuar simples; não criar pipeline Session pesada.
