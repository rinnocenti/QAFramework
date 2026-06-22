# F2-01 — ADR-SESSION-001 — Session Scope and Owner

Status: Accepted  
Fase: F2  
Cut de decisão: F2A  
Ordem no Plano: F2-01  
Tipo: Session  
Escopo: Session runtime

---

## Contexto

F0 fechou o baseline do package e F1 fechou as regras mínimas de API status, diagnostics, typed identity e content identity. A próxima dependência do roadmap é F2: formalizar Session como scope antes de Route baseline, Activity content set, Content Anchor, RuntimeMaterialization ou consumers.

O package já possui `FrameworkRuntimeHost` e `FrameworkRuntimeState`, mas a Session ainda não está declarada como boundary arquitetural explícito. Sem isso, `SessionContentSet`, startup route, diagnostics e futuros conteúdos persistentes podem cair em um manager global ou em service locator.

---

## Decisão

A Session é o escopo runtime superior do framework durante uma execução do jogo.

`FrameworkRuntimeHost` fica aceito como owner inicial da Session runtime. Ele pode orquestrar boot, startup route e requests, mas não pode virar service locator público.

A Session deve ter estado próprio explícito, por refinamento de `FrameworkRuntimeState` ou por novo `SessionRuntimeState` em corte técnico posterior. Esse estado deve cobrir, no mínimo:

- identidade da Session;
- referência à `GameApplicationAsset` ativa;
- status/lifecycle da Session;
- startup route declarada/resolvida;
- diagnostics/facts associados ao escopo Session;
- ponte para `SessionContentSet` mínimo.

A identidade da Session deve seguir a política F1:

```text
FrameworkIdentityDomain.Session + FrameworkIdentityValue
```

Strings continuam válidas para labels, nomes de objeto, reason e mensagens humanas, mas não devem ser usadas como chave funcional nova sem domínio.

---

## Restrições

A Session não deve possuir diretamente consumers concretos:

```text
Camera
Audio
Input
Actor
Pooling
Save
Pause
Projectile
Damage
Attributes
```

A Session também não deve criar:

```text
public service registry
public service locator
global dependency manager
runtime lifecycle pipeline pesado
fallback silencioso para configuração obrigatória ausente
```

---

## Consequências

### Positivas

- Formaliza o topo do lifecycle.
- Cria owner claro para conteúdo session-scoped.
- Prepara `SessionContentSet` sem criar manager global.
- Mantém F3/F4/F7/F8 dependentes de uma base de Session explícita.

### Trade-offs

- Pode exigir pequeno ajuste em `FrameworkRuntimeState`.
- Alguns consumers precisarão esperar phases posteriores.
- `FrameworkRuntimeHost` continua simples, mas passa a ter boundary arquitetural formal.

---

## Fora do escopo

- Player participation.
- Runtime roots.
- Persistent scenes.
- Content Anchor.
- Runtime materialization.
- Route baseline.
- Activity content/readiness.
- Concrete consumer hosts.

---

## Critérios de validação da implementação posterior

- Existe estado explícito de Session ou refinamento equivalente de `FrameworkRuntimeState`.
- O host é owner claro da Session.
- A Session possui identidade tipada.
- Não há service locator público novo.
- Boot continua: Session válida → Startup Route → Route Smoke.

---

## Relação com roadmap

Cobre:

```text
IF-FW-ROAD-2A — ADR: Session Scope
IF-FW-ROAD-2B — SessionRuntimeState explícito
IF-FW-ROAD-2F — Session smoke
```

Prepara:

```text
IF-FW-ROAD-2C — SessionContentSet mínimo
IF-FW-ROAD-2D — SessionContentOwnership semantics
```

---

## Notas de implementação

O corte técnico seguinte deve ser pequeno:

```text
F2B — SessionRuntimeState explicit boundary
```

Não criar pipeline de Session pesada. Não introduzir consumers. Não abrir F3 antes de Session owner e state ficarem claros.
