# F2-02 — ADR-SESSION-002 — SessionContent Ownership Semantics

Status: Draft / Deferred  
Fase: F2  
Ordem no Plano: F2-02  
Tipo: Session / Content  
Escopo: SessionContentSet

---

## Contexto

Antes de RuntimeSpawned e persistent content, é necessário distinguir conteúdo session-scoped de registro diagnóstico.

## Decisão

Criar `SessionContentSet` mínimo com semântica explícita:

- `Registered`: conhecido pelo runtime, não necessariamente owned.
- `Owned`: deve ser liberado pelo owner da Session.
- `DiagnosticOnly`: aparece em status/smoke, mas não tem release.

Na primeira versão, pode conter apenas registros mínimos ou ficar vazio, desde que a semântica exista.

## Consequências

### Positivas

- Prepara roots e release.
- Evita misturar diagnóstico com ownership.
- Cria paralelismo com Route/Activity content sets.

### Negativas / trade-offs

- Pode parecer vazio no início.
- Exige disciplina para não virar manager global.

## Fora do escopo

- Runtime roots.
- Prefab materialization.
- Persistent services concretos.

## Critérios de validação

- `SessionContentSet` existe e é owned pela Session.
- Documentação diferencia registro e ownership.

## Impacto esperado

Pré-requisito de Runtime roots e persistent content.

## Relação com roadmap

F2.

## Notas de implementação

Não deve substituir `FrameworkRuntimeHost`; é dado/estado, não host.
