# ADR-BL-002 — Core vs Consumers

Status: Proposed  
Fase: F0A  
Tipo: Arquitetura  
Escopo: Core / Consumers

---

## Contexto

As auditorias mostram que o `NewScripts` ficou pesado porque subsistemas concretos, como camera, audio, input, save, pause, actor, projectile, damage e pooling, entraram cedo demais no lifecycle central. O package atual já corre esse risco com `CameraFlow` no runtime core.

## Decisão

O core do framework deve expor lifecycle, content, identity, diagnostics, contribution, surface e runtime ownership. Subsistemas concretos devem ser consumers.

Regra:

```text
Consumers declaram requisitos e recebem contexto.
Consumers não descobrem o mundo sozinhos.
Consumers não possuem o lifecycle core.
```

Classificação:

| Core inicial | Consumers intermediários | Consumers avançados |
|---|---|---|
| Session, Route, Activity, ContentFlow, LocalContribution, Diagnostics, Identity | Input, Snapshot/Save, Pause | Camera, Audio, Actor, Pooling, Projectile, Damage, Attributes |

## Consequências

### Positivas

- Mantém o framework pequeno e estável.
- Evita repetir pipelines monolíticas.
- Permite packages opcionais e adapters por subsistema.

### Negativas / trade-offs

- Alguns recursos desejados ficam adiados.
- Exige mais disciplina nos cortes para não misturar consumer com core.

## Fora do escopo

- Definir APIs finais de Camera, Audio, Actor ou Save.
- Criar package split agora.

## Critérios de validação

- Nenhuma nova classe core referencia diretamente Cinemachine, PlayerInput, SaveBackend, Projectile, Damage ou Audio service.
- Consumers acessam lifecycle por contratos/contextos, não por statics/service locator.

## Impacto esperado

Define a fronteira que guiará todo o roadmap.

## Relação com roadmap

F0A. Condiciona F10, F11 e F12.

## Notas de implementação

Pode ser referenciado por todos os ADRs posteriores de consumers.
