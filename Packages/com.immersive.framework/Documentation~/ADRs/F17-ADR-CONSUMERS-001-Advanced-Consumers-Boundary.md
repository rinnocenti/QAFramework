# F17-ADR-CONSUMERS-001 — Advanced Consumers Boundary

Status: Proposed  
Fase: F17 — Advanced Consumers  
Tipo: Consumers / Boundary / Integration  
Última atualização: 2026-06-25

---

## 1. Contexto

Camera, Audio, Actor e Pooling estavam originalmente previstos cedo demais como F11. A decisão pós-F10 reposiciona esses módulos para depois de Cycle Reset, Object Entry, Local/Object Reset, Unity Reset Adapters e Player baseline.

F17 recupera esses consumers sem permitir que eles capturem o core.

---

## 2. Dor original

O framework precisa integrar subsistemas técnicos reais, mas sem voltar ao problema do projeto anterior: subsistemas controlando lifecycle, descobrindo mundo sozinhos ou criando authorities globais.

---

## 3. Decisão

F17 define a fronteira de consumers avançados.

Consumers avançados incluem:

```text
Camera
Audio
Actor
Pooling
```

Eles devem consumir core contracts existentes:

```text
Lifecycle
Content Anchor
Runtime Materialization
RuntimeContentHandle
Object Entry
Reset
Contribution
Release
Diagnostics
```

Eles não definem esses contratos.

---

## 4. Escopo incluído

F17 pode incluir ADRs/cortes específicos para:

```text
Camera as Content Anchor consumer
Audio as lifecycle consumer
Actor runtime boundary
Pooling package boundary
Pooled materializer
```

---

## 5. Escopo excluído

F17 exclui gameplay pesado:

```text
Projectile gameplay
Damage system
Attributes system
Powerups
Combat rules
Inventory
Enemy AI framework
```

Actor baseline pode entrar como runtime object/consumer, mas não como sistema completo de gameplay.

---

## 6. Modelo conceitual

Consumer avançado deve seguir o fluxo:

```text
Framework core context
  -> consumer request/contract
    -> result/handle
      -> release/reset participation through existing contracts
```

Não permitido:

```text
consumer scans world globally
consumer owns Route/Activity lifecycle
consumer creates static global authority as primary API
consumer bypasses RuntimeContentHandle/ContentAnchor when materializing content
```

---

## 7. Camera

Camera deve ser consumer de Content Anchor/Route/Activity context.

Guardrails:

```text
Sem FrameworkCameraAuthority static global como contrato público.
Sem câmera definindo Route/Activity lifecycle.
Sem Cinemachine como dependência obrigatória do core, salvo decisão explícita de package/adapter.
```

---

## 8. Audio

Audio deve consumir lifecycle context.

Guardrails:

```text
Audio não possui Route/Activity.
Audio listener persistente deve ser Session content/consumer, não core obrigatório.
BGM/SFX não podem controlar transição de lifecycle.
```

---

## 9. Actor

Actor é runtime object/contribution, não lifecycle core.

Guardrails:

```text
Actor entra por RuntimeMaterialization/ObjectEntry.
Actor reset/release usa contracts existentes.
Actor não substitui Player/Participant baseline.
Actor não força gameplay capabilities.
```

---

## 10. Pooling

Pooling é infraestrutura técnica.

Guardrails:

```text
Pool não é owner de lifecycle.
Pool return é release policy/materializer behavior, não reset.
Pooling não deve ser projectile-first.
Pooling package permanece separado.
```

---

## 11. Diagnostics e validação

Cada consumer deve ter smoke específico, mas todos devem provar:

```text
consumer entered through core contract
consumer did not bypass owner
consumer produced result/handle
consumer released through explicit path
consumer did not leave orphan runtime content
```

---

## 12. Consequências

### Positivas

- Consumers entram quando o core já tem lifecycle, reset, object entry e release.
- Menor risco de Camera/Audio/Actor capturarem arquitetura.
- Pooling fica técnico, não gameplay-first.

### Custos

- Consumers úteis entram mais tarde.
- Cada consumer precisará de boundary próprio.

---

## 13. Guardrails gerais

- Nenhum consumer pode criar lifecycle paralelo.
- Nenhum consumer pode descobrir o mundo sozinho como fonte de verdade.
- Nenhum consumer pode usar nome/path como chave funcional.
- Nenhum consumer pode criar fallback silencioso quando required dependency faltar.
- Nenhum consumer pode transformar reset em release ou pool return.

---

## 14. Relação com fases futuras

F17 desbloqueia F18, onde gameplay capabilities passam a usar Actor/Runtime/Pooling/Input/Reset de forma controlada.
