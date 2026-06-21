# ADR-BL-005 — Dependency Policy

Status: Proposed  
Fase: F0A  
Tipo: Package / Dependencies  
Escopo: UPM / asmdef

---

## Contexto

O package atual depende de `com.immersive.foundation`, `com.immersive.logging` e também de Cinemachine por causa do `CameraFlow`. Como camera é consumer avançado, Cinemachine não deve ser dependência obrigatória do core sem decisão explícita.

## Decisão

Dependências do core devem ser mínimas.

| Dependência | Status recomendado |
|---|---|
| `com.immersive.foundation` | Core permitido, se usado para eventos/primitives. |
| `com.immersive.logging` | Core permitido, por `FrameworkLogger`. |
| `com.unity.cinemachine` | Não deve ser core obrigatório enquanto CameraFlow estiver Deferred/Experimental. |
| `com.immersive.pooling` | Não entra no core antes de Runtime/Pooling ADR. |

Dependências Git devem ser pinadas por tag/hash ou política equivalente.

## Consequências

### Positivas

- Evita acoplamento prematuro.
- Reduz risco de import em projetos novos.
- Mantém consumers como opcionais.

### Negativas / trade-offs

- Camera pode exigir package separado/asmdef adicional.
- Reorganização de asmdefs pode ser necessária.

## Fora do escopo

- Criar packages opcionais agora.
- Resolver todos os asmdefs finais.

## Critérios de validação

- `package.json` não inclui consumer avançado como dependência core sem ADR.
- Git dependencies têm estratégia de pinagem.

## Impacto esperado

Afeta `CameraFlow`, futura integração Pooling e split de consumers.

## Relação com roadmap

F0A/F0B/F11.

## Notas de implementação

Se CameraFlow for mantido ativo, precisa de ADR que justifique Cinemachine no baseline.
