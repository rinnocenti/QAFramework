# F8-01 — ADR-RUNTIME-001 — Runtime Ownership and Roots

Status: Draft / Deferred  
Fase: F8  
Ordem no Plano: F8-01  
Tipo: RuntimeSpawned  
Escopo: Runtime roots

---

## Contexto

`NewScripts` criou roots runtime por adapters e nomes de GameObject. O package deve ter ownership formal antes de materialização runtime.

## Decisão

Criar runtime ownership por escopo:

```text
Session
Route
Activity
Transient
```

`RuntimeRootRegistry` resolve roots por scope. Materializers não fazem `GameObject.Find` nem criam roots ad hoc por nome.

Cada root tem owner, lifetime e cleanup policy.

## Consequências

### Positivas

- Evita roots órfãos.
- Prepara materializer, pooling e actors.
- Remove dependência de nomes/hierarchy.

### Negativas / trade-offs

- Exige state e lifecycle mais claros.
- Pode precisar de GameObject root no runtime.

## Fora do escopo

- Pooling.
- Actor materialization.
- Content Anchor binding.

## Critérios de validação

- Root existe para scope materializado.
- Activity exit limpa Activity root/content.
- Route exit limpa Route root/content.
- Nenhum `GameObject.Find` para roots.

## Impacto esperado

Base para Runtime materialization.

## Relação com roadmap

F8.

## Notas de implementação

Pode ser minimalista no início.
