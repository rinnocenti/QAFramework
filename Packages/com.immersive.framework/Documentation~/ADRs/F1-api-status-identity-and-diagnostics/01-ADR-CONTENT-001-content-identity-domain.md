# ADR-CONTENT-001 — Content Identity Domain

Status: Draft / Deferred  
Fase: F1  
Tipo: ContentFlow  
Escopo: Content identity

---

## Contexto

`FrameworkContentHandle` precisa de identidade estável. Path/name/fallback Guid instável não devem virar contrato público.

## Decisão

Definir domínio de identidade de content handles:

- `ContentOwnerId`: quem possui o conteúdo.
- `ContentScope`: Session, Route, Activity, Local, Runtime.
- `ContentKind`: Scene, GameObject, Prefab, Surface, Runtime.
- `ContentId`: valor tipado por domínio.

A identidade deve ser criada por fonte estável: asset reference, explicit authoring id ou runtime-generated id controlado. Fallback aleatório não é identity funcional.

## Consequências

### Positivas

- Base para ContentSet e release.
- Evita órfãos e registros duplicados.
- Permite comparação segura entre content handles.

### Negativas / trade-offs

- Pode exigir revisão de `RoutePrimaryScene`.
- Exige cuidado com scene references.

## Fora do escopo

- Resolver Addressables.
- Criar GUID global custom para tudo.

## Critérios de validação

- `FrameworkContentHandle` não depende de path/name como única chave.
- Fallback instável é removido ou explicitamente diagnóstico.

## Impacto esperado

Afeta F1, F2, F3, F4, F6 e F8.

## Relação com roadmap

F1.

## Notas de implementação

Deve alinhar com ADR-ID-001.
