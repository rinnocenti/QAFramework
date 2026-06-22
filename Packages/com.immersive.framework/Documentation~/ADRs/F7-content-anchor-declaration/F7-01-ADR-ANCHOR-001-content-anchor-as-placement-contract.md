# F7-01 — ADR-ANCHOR-001 — Content Anchor as Placement Contract

Status: Draft / Deferred  
Fase: F7  
Ordem no Plano: F7-01  
Tipo: Content Anchor  
Escopo: Content Anchor declaration

---

## Contexto

No `NewScripts`, Content Anchor aparece presa a pause/camera/presentation. O framework deve modelar Content Anchor antes dos consumers concretos.

## Decisão

Content Anchor é contrato de espaço, não subsistema.

Content Anchor pode declarar:

- identity;
- roots;
- slots;
- anchors;
- scope;
- requiredness/validation.

Content Anchor não deve declarar:

- pause behavior;
- camera rig;
- UI behavior;
- presentation materialization;
- input policy.

Consumers consomem `ContentAnchorSet`.

## Consequências

### Positivas

- Impede Camera/Pause de capturarem o core.
- Cria UX authored reutilizável.
- Prepara RuntimeContentAnchorBinding.

### Negativas / trade-offs

- Mais uma camada conceitual antes de recursos visíveis.
- Requer linguagem amigável de Inspector depois.

## Fora do escopo

- Content Anchor binding com prefab.
- Camera/Pause/UI concrete.
- Runtime materialization.

## Critérios de validação

- ContentAnchorEndpoint sem identity falha em validator.
- Duplicate slot/anchor/root role falha.
- ContentAnchorSet é populado por scope.

## Impacto esperado

Pré-requisito de Pause, Camera e Presentation consumers.

## Relação com roadmap

F7.

## Notas de implementação

Content Anchor declaration pode existir antes de RuntimeRootRegistry.
