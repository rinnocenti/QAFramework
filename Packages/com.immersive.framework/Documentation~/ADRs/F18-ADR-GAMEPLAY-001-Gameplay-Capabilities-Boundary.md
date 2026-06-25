# F18-ADR-GAMEPLAY-001 — Gameplay Capabilities Boundary

Status: Proposed  
Fase: F18 — Gameplay Capabilities  
Tipo: Gameplay / Capabilities / Boundary  
Última atualização: 2026-06-25

---

## 1. Contexto

F18 é a fase em que gameplay pesado pode começar a entrar. Até aqui, o framework deve ter core lifecycle, content, contribution, release, runtime materialization, Content Anchor, Activity execution, Cycle Reset, Object Entry, Local/Object Reset, Unity Reset Adapters, Player baseline e consumers avançados.

Somente depois disso faz sentido plugar Projectile, Damage, Attributes, Powerups e capacidades avançadas de Actor.

---

## 2. Dor original

O projeto antigo possuía várias capacidades úteis de gameplay, mas muitas estavam acopladas a lifecycle, pipeline, scene loading, pool, actor e reset de forma difícil de migrar.

F18 preserva as capacidades como conceito, mas impede que gameplay dite o core.

---

## 3. Decisão

Gameplay capabilities entram no fim da sequência.

F18 pode incluir:

```text
Projectile as RuntimeSpawned
Impact as capability
Damage as actor/object capability
Attributes as snapshot-capable capability
Powerups as gameplay capability
Advanced Actor capabilities
```

Essas capacidades devem consumir contracts existentes.

---

## 4. Escopo incluído

F18 inclui desenho e implementação incremental de gameplay capabilities apoiadas em:

```text
RuntimeContentHandle
Object Entry
Actor/Player participation
Pooling boundary
Reset participants
Snapshot participants
Release policy
Input contracts
Diagnostics
```

---

## 5. Escopo excluído

F18 não deve redesenhar:

```text
Route lifecycle
Activity lifecycle
Scene composition
Content Anchor core
Runtime materialization core
Cycle Reset core
Object Entry core
Pooling core
Input ownership core
```

Se uma gameplay capability exigir mudar essas bases, o corte deve parar e abrir ADR de revisão.

---

## 6. Modelo conceitual

Gameplay capability é contribuição de um objeto/actor/player ao comportamento do jogo.

Ela não é owner de lifecycle.

```text
Object/Actor/Player
  -> contributes gameplay capability
    -> receives lifecycle/reset/snapshot context
      -> mutates its own state by contract
```

---

## 7. Projectile

Projectile deve ser runtime-spawned content.

Guardrails:

```text
Projectile não controla pool diretamente.
Projectile não controla Activity release.
Projectile recebe lifetime/release policy por RuntimeContentHandle.
Impact pode solicitar finalização, mas não possui lifecycle global.
```

---

## 8. Impact / Damage

Impact e Damage devem ser capabilities separadas.

Guardrails:

```text
Impact detecta/interpreta contato.
Damage aplica mutação por contrato.
Damage não acessa internals de Actor diretamente.
Damage não controla projectile lifecycle diretamente.
```

---

## 9. Attributes

Attributes devem ser capability snapshot-capable.

Guardrails:

```text
Attributes participam de SnapshotSet.
Attributes podem participar de Reset quando policy mandar.
Attributes não definem save backend.
Attributes não definem Actor lifecycle.
```

---

## 10. Powerups

Powerups devem ser gameplay capabilities, não core.

Guardrails:

```text
Powerup reset depende de ResetContext.
Powerup persistence depende de Snapshot policy.
Powerup pickup/spawn depende de Object/Runtime content contracts.
```

---

## 11. Diagnostics e validação

Cada gameplay capability deve validar:

```text
entry through object/actor contribution
clear lifecycle owner
reset behavior by context
snapshot behavior if applicable
release behavior if runtime-spawned
no orphan content
no fallback by name/path
```

---

## 12. Consequências

### Positivas

- Gameplay entra sobre fundação sólida.
- Capacidades do NewScripts são preservadas sem copiar anti-padrões.
- Reset, snapshot, release e pooling ficam separados.

### Custos

- Gameplay útil entra mais tarde.
- Algumas capacidades precisarão ser redesenhadas em vez de migradas diretamente.

---

## 13. Guardrails gerais

- Gameplay não altera ownership de Route/Activity.
- Gameplay não cria pipeline paralelo.
- Gameplay não usa service locator público.
- Gameplay não usa nome/path como identidade funcional.
- Gameplay não transforma pool return em reset.
- Gameplay não transforma snapshot restore em reset.
- Gameplay não ignora required/optional policy.

---

## 14. Relação com fases futuras

F18 é o ponto onde o framework começa a virar base de jogo real, mas sempre por capabilities isoladas.

Cortes futuros devem ser capability-first e contract-driven:

```text
Projectile capability
Impact capability
Damage capability
Attributes capability
Powerup capability
Advanced Actor capability
```
