# ADR-BL-003 — Public API Status Policy

Status: Proposed  
Fase: F0A  
Tipo: API Policy  
Escopo: Package público

---

## Contexto

O package possui superfícies públicas em níveis diferentes de maturidade. Sem status explícito, APIs experimentais como materializer/contribution podem parecer estáveis cedo demais.

## Decisão

Toda superfície pública/semi-pública deve ser classificada como:

| Status | Significado |
|---|---|
| `Stable` | Pode ser consumida por jogos e módulos externos. Mudanças exigem ADR/migração. |
| `Experimental` | Pode mudar sem compatibilidade. Usada para cortes internos ou validação controlada. |
| `Internal` | Não deve ser usada por código de jogo. |
| `Deferred` | Existe como planejamento ou código congelado, não faz parte do baseline ativo. |
| `Removed` | Deve sair do package. |

O status deve aparecer em documentação, comentários XML ou estrutura de namespace/assembly quando aplicável.

## Consequências

### Positivas

- Reduz risco de API prematura.
- Ajuda validators e docs.
- Permite manter código experimental sem mentir sobre maturidade.

### Negativas / trade-offs

- Exige manutenção documental.
- Pode criar ruído se usado em excesso.

## Fora do escopo

- Garantir estabilidade semver formal.
- Criar analyzer automático nesta fase.

## Critérios de validação

- `ContentFlow`, `RouteContentRuntime`, `CameraFlow`, `QA Canvas` e triggers têm status claro.
- README/ADRs referenciam a política.

## Impacto esperado

Necessário antes de expandir ContentFlow e consumers.

## Relação com roadmap

F0A/F1.

## Notas de implementação

Pode ser implementado inicialmente só por documentação e comentários.
