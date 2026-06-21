# ADR-BL-005 — Dependency Policy

Status: Accepted  
Fase: F0A  
Tipo: Package / Dependencies  
Escopo: UPM / asmdef

---

## Contexto

O package atual depende de `com.immersive.foundation`, `com.immersive.logging` e também de Cinemachine por causa do `CameraFlow`. Como camera é consumer avançado, Cinemachine não deve ser dependência obrigatória do core sem decisão explícita posterior.

## Decisão

Dependências do core devem ser mínimas e coerentes com a fronteira Core vs Consumers.

| Dependência | Status F0A | Decisão aceita |
|---|---|---|
| `com.immersive.foundation` | Core permitido | Permitido se usado para eventos/primitives. |
| `com.immersive.logging` | Core permitido | Permitido por `FrameworkLogger`. |
| `com.unity.cinemachine` | Não permitido como core obrigatório | Deve sair do core enquanto `CameraFlow` estiver `Deferred`. |
| `com.immersive.pooling` | Não permitido no core inicial | Só entra via ADR de Runtime/Pooling em F11 ou package opcional. |
| Dependências Git | Permitidas com controle | Devem ser pinadas por tag/hash ou política equivalente. |

Aplicação imediata: F0B deve remover a contradição entre `CameraFlow` deferred e Cinemachine obrigatório no package core. A solução preferida é remover/congelar `CameraFlow` do core inicial. Split opcional de camera fica fora de F0A/F0B.

## Consequências

### Positivas

- Evita acoplamento prematuro.
- Reduz atrito de import em projetos novos.
- Mantém consumers como opcionais.
- Alinha package/asmdef ao roadmap.

### Negativas / trade-offs

- Código de camera existente pode precisar sair do pacote core.
- Um consumer de camera posterior exigirá package/asmdef opcional.
- A remoção de Cinemachine do core pode quebrar cenas de teste antigas que dependiam do binding.

## Fora do escopo

- Criar packages opcionais agora.
- Resolver todos os asmdefs finais.
- Definir API final de camera.

## Critérios de validação

- `package.json` não inclui consumer avançado como dependência core sem ADR.
- `Immersive.Framework.Runtime.asmdef` não referencia assemblies de consumer avançado sem ADR.
- Git dependencies têm estratégia de pinagem.

## Impacto esperado

Afeta F0B diretamente e condiciona Camera, Pooling e outros consumers avançados.

## Relação com roadmap

F0A/F0B/F11.

## Notas de implementação

Se `CameraFlow` voltar no futuro, deve voltar como consumer explícito após Surface/Runtime e com dependência opcional ou justificada por ADR próprio.
