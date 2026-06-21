# ADR-BL-002 — Core vs Consumers

Status: Accepted  
Fase: F0A  
Tipo: Arquitetura  
Escopo: Core / Consumers

---

## Contexto

As auditorias mostram que o `NewScripts` ficou pesado porque subsistemas concretos entraram cedo demais no lifecycle central. O package atual já apresenta esse risco com `CameraFlow` e a dependência obrigatória de Cinemachine no runtime core.

## Decisão

O core do framework deve nascer como framework de lifecycle, content, identity, diagnostics, contribution, surface e runtime ownership. Subsistemas concretos devem entrar como consumers.

Regra operacional:

```text
Consumers declaram requisitos e recebem contexto.
Consumers não descobrem o mundo sozinhos.
Consumers não possuem o lifecycle core.
Consumers não forçam dependência obrigatória no core inicial.
```

Classificação aceita:

| Core inicial | Consumers intermediários | Consumers avançados | Gameplay capabilities |
|---|---|---|---|
| Session, Route, Activity, ContentFlow, LocalContribution, Diagnostics, Identity, Surface, Runtime ownership | Input, Snapshot/Save, Pause | Camera, Audio, Actor, Pooling | Projectile, Impact, Damage, Attributes |

Aplicação imediata sobre o baseline:

| Item | Classificação | Consequência |
|---|---|---|
| `CameraFlow` | Consumer avançado | Não dita core; não deve manter Cinemachine como dependência obrigatória. |
| `FrameworkQaCanvas` | Development tooling | Útil para smoke, mas não API de produto. |
| `ContentFlow` | Core experimental | Pode manter vocabulário, mas não estabiliza materializer antes de identity/owner/release. |
| `RouteContentRuntime` | Route baseline futuro | Não deve ser conectado em F0A/F0B. |

## Consequências

### Positivas

- Mantém o core menor e mais estável.
- Evita repetir pipelines monolíticas.
- Permite packages/adapters opcionais por subsistema.
- Dá uma regra objetiva para bloquear cortes prematuros.

### Negativas / trade-offs

- Camera, audio, actor, pooling e projectile ficam adiados.
- Pode ser necessário remover código existente do runtime core mesmo que ele compile hoje.
- Alguns smokes visuais dependentes de camera/tooling deixam de ser critério de core.

## Fora do escopo

- Definir API final de Camera, Audio, Actor, Save, Input ou Pooling.
- Criar packages opcionais agora.
- Criar Surface ou RuntimeMaterialization em F0A.

## Critérios de validação

- Nenhuma nova classe core referencia diretamente Cinemachine, PlayerInput, SaveBackend, Projectile, Damage ou Audio service.
- Consumers acessam lifecycle por contratos/contextos, não por statics/service locator.
- ADRs futuros de consumers citam esta fronteira.

## Impacto esperado

Define a fronteira que guia todo o roadmap e impede que consumers avancem antes de owner, identity, content set, release, surface e runtime.

## Relação com roadmap

F0A. Condiciona F10, F11 e F12.

## Notas de implementação

F0B deve aplicar esta decisão removendo ou congelando contradições existentes, especialmente `CameraFlow` no runtime core.
