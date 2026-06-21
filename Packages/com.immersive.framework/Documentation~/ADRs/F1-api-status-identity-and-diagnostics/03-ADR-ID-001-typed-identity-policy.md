# ADR-ID-001 — Typed Identity Policy

Status: Accepted  
Fase: F1  
Tipo: Identity  
Escopo: Framework-wide

---

## Contexto

O framework precisa crescer para `ContentSet`, `LocalContribution`, `Surface`, `RuntimeContentHandle`, Snapshot e consumers sem repetir o problema do sistema anterior: muitas chaves textuais, concatenações e caminhos sendo usados como identidade funcional.

No baseline atual ainda existem identificadores simples baseados em asset, cena, path ou texto. Isso é aceitável como etapa inicial, mas não pode virar contrato público para APIs novas. Path, nome de cena, nome de GameObject, transform path e labels são úteis para diagnóstico, mas são frágeis como chave funcional.

Sem uma política explícita, F1/F2/F3/F4 podem criar APIs que aceitam `string` crua para conceitos diferentes e permitir comparações indevidas entre domínios.

## Decisão

Identidade funcional deve ter **domínio explícito** e **tipo próprio**.

Regra central:

```text
String crua pode ser label, debug name, source, reason ou texto de diagnóstico.
String crua não deve ser a API pública de identidade funcional nova.
```

Domínios diferentes não são intercambiáveis:

```text
SessionId ≠ RouteId ≠ ActivityId ≠ ContentId ≠ LocalContentIdentity ≠ SurfaceIdentity ≠ RuntimeContentId
```

Novos modelos de identidade devem seguir estas regras:

1. **Domínio no tipo**  
   O nome/tipo deve comunicar o domínio: `RouteId`, `ActivityId`, `ContentId`, `SurfaceIdentity`, etc.

2. **Valor validável**  
   O tipo deve conseguir distinguir valor ausente/inválido de valor válido.

3. **Sem fallback silencioso**  
   Se uma identidade required não puder ser construída, o fluxo deve falhar ou gerar diagnóstico explícito. Não gerar Guid aleatório como identidade funcional de longo prazo.

4. **Debug separado de identidade**  
   `DisplayName`, `DebugName`, `Path`, `SceneName`, `Source` e `Reason` não substituem identity.

5. **Interop com Unity controlada**  
   Referências de asset, scene path e GUID Unity podem ser fonte para construir identity, mas o contrato público do framework deve expor o domínio do framework, não o detalhe cru.

6. **Comparação entre domínios proibida por shape**  
   APIs novas devem dificultar ou impedir comparar `ActivityId` com `RouteId`, `ContentId` com `SurfaceIdentity`, etc.

## Política de migração

Este ADR não exige migrar todo o baseline em um único corte. Ele define a trava para o que vier depois.

Aplicação imediata:

| Caso | Regra F1 |
|---|---|
| API nova | Não aceitar `string` crua como identidade funcional. |
| API existente experimental | Pode permanecer, mas deve ser marcada como `Experimental` ou revisada em corte próprio. |
| Labels/logs | Podem continuar string. |
| `source` / `reason` | Continuam string, pois são diagnóstico/contexto humano. |
| Paths Unity | Podem aparecer em diagnostics; não como única chave funcional estável de API nova. |

## Consequências

### Positivas

- Reduz colisões e stale matches.
- Impede comparação acidental entre domínios.
- Prepara `ContentIdentity`, `LocalContentIdentity`, `SurfaceIdentity`, `RuntimeContentHandle` e Snapshot.
- Dá regra objetiva para revisar APIs existentes.

### Negativas / trade-offs

- Cria mais tipos pequenos.
- Exige conversões explícitas entre authoring Unity e runtime framework.
- Pode aumentar o custo inicial dos cortes F1/F3/F4/F5.

## Fora do escopo

- Migrar todas as strings existentes agora.
- Criar analyzer automático.
- Criar sistema global de GUIDs custom para todos os objetos.
- Definir serialização final de todos os IDs.
- Trocar labels/logs/source/reason por tipos fortes.

## Critérios de validação

- ADRs posteriores de Content, Local, Surface e Runtime referenciam esta política.
- APIs novas não usam `string` crua como identidade funcional.
- Diagnóstico pode continuar textual, mas não vira chave funcional.
- Identidades required sem valor válido não recebem fallback silencioso.

## Impacto esperado

Este ADR é pré-requisito para:

- `Content Identity Domain`;
- `FrameworkContentHandle` revisado;
- `LocalContentIdentity`;
- `SurfaceIdentity`;
- `RuntimeContentHandle`;
- Snapshot identities;
- policies de stale/foreign references.

## Relação com roadmap

F1. Condiciona F2, F3, F4, F5, F7, F8, F9 e F10.

## Notas de implementação

O primeiro corte técnico após F1A deve criar uma convenção leve de API/status/identity antes de tentar migrar domínios inteiros.
