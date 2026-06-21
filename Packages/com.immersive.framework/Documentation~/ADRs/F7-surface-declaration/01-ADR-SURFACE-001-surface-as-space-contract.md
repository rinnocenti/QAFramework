# ADR-SURFACE-001 — Surface as Space Contract

Status: Draft / Deferred  
Fase: F7  
Tipo: Surface  
Escopo: Surface declaration

---

## Contexto

No `NewScripts`, Surface aparece presa a pause/camera/presentation. O framework deve modelar Surface antes dos consumers concretos.

## Decisão

Surface é contrato de espaço, não subsistema.

Surface pode declarar:

- identity;
- roots;
- slots;
- anchors;
- scope;
- requiredness/validation.

Surface não deve declarar:

- pause behavior;
- camera rig;
- UI behavior;
- presentation materialization;
- input policy.

Consumers consomem `SurfaceSet`.

## Consequências

### Positivas

- Impede Camera/Pause de capturarem o core.
- Cria UX authored reutilizável.
- Prepara RuntimeSurfaceBinding.

### Negativas / trade-offs

- Mais uma camada conceitual antes de recursos visíveis.
- Requer linguagem amigável de Inspector depois.

## Fora do escopo

- Surface binding com prefab.
- Camera/Pause/UI concrete.
- Runtime materialization.

## Critérios de validação

- SurfaceEndpoint sem identity falha em validator.
- Duplicate slot/anchor/root role falha.
- SurfaceSet é populado por scope.

## Impacto esperado

Pré-requisito de Pause, Camera e Presentation consumers.

## Relação com roadmap

F7.

## Notas de implementação

Surface declaration pode existir antes de RuntimeRootRegistry.
