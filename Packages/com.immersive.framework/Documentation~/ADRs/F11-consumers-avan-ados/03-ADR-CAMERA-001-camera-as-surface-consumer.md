# ADR-CAMERA-001 — Camera as Surface Consumer

Status: Draft / Deferred  
Fase: F11  
Tipo: Consumer / Camera  
Escopo: Camera

---

## Contexto

`CameraFlow` atual está ambíguo. O `NewScripts` mostra que camera deve consumir Surface/Anchor, não ser core lifecycle.

## Decisão

Camera deve ser consumer de Surface/Anchor.

- Camera request usa SurfaceSet/Anchor.
- Binding produz handle.
- Release segue scope.
- Cinemachine fica em adapter/package opcional, não core obrigatório se possível.
- Não usar static global authority como centro do framework.

## Consequências

### Positivas

- Reintegra CameraFlow em forma correta.
- Evita dependência prematura de Cinemachine.
- Permite cameras route/activity sem capturar core.

### Negativas / trade-offs

- Requer Surface baseline.
- Pode exigir reorganização do código existente.

## Fora do escopo

- Camera advanced blending.
- Split package definitivo.
- AudioListener policy.

## Critérios de validação

- Camera ativa muda por Route/Activity via Surface.
- Sem static global como única autoridade.
- Release do scope desfaz binding.

## Impacto esperado

Reentrada correta de CameraFlow.

## Relação com roadmap

F11.

## Notas de implementação

Se CameraFlow for removido em F0B, este ADR orienta reintrodução futura.
