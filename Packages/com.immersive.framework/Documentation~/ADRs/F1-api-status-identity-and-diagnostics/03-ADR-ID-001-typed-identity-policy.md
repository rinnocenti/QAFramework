# ADR-ID-001 — Typed Identity Policy

Status: Draft / Deferred  
Fase: F1  
Tipo: Identity  
Escopo: Framework-wide

---

## Contexto

O `NewScripts` usa muitos IDs textuais e concatenações. O package atual também usa content ids baseados em path/name/fallback. Isso pode gerar stale matches, colisões e identidade instável.

## Decisão

Identidade funcional deve ter domínio explícito e tipo próprio. Strings podem existir como label, debug name ou source, mas não como chave funcional sem wrapper/validação.

Princípios:

```text
SessionId ≠ RouteId ≠ ActivityId ≠ ContentId ≠ LocalContentIdentity ≠ SurfaceIdentity
```

Path, scene name, GameObject name e transform path são diagnósticos, não identidade primária.

## Consequências

### Positivas

- Reduz bugs por comparação entre domínios.
- Facilita validators.
- Prepara Local, Surface e RuntimeSpawned.

### Negativas / trade-offs

- Mais tipos pequenos.
- Mais conversões authoring/runtime.

## Fora do escopo

- Migrar todos os IDs em um corte só.
- Criar serialization complexa agora.

## Critérios de validação

- Novas APIs não aceitam string crua para identity funcional.
- Warnings/validators detectam identity vazia ou duplicada.

## Impacto esperado

Pré-requisito para ContentFlow, Local, Surface e Runtime.

## Relação com roadmap

F1.

## Notas de implementação

Este ADR deve preceder `LocalContentIdentity`, `SurfaceIdentity` e `RuntimeContentHandle`.
