# F0A-03 — ADR-BL-003 — Public API Status Policy

Status: Accepted  
Fase: F0A  
Ordem no Plano: F0A-03  
Tipo: API Policy  
Escopo: Package público

---

## Contexto

O package possui superfícies públicas em níveis diferentes de maturidade. Sem status explícito, APIs experimentais como materializer/contribution, QA tooling ou camera binding podem parecer estáveis cedo demais.

## Decisão

Toda superfície pública/semi-pública deve ser classificada como uma das categorias abaixo.

| Status | Significado | Regra de evolução |
|---|---|---|
| `Stable` | Pode ser consumida por jogos e módulos externos. | Mudanças exigem ADR/migração. |
| `Experimental` | Pode mudar sem compatibilidade. | Usada para validação controlada; não é contrato final. |
| `Internal` | Não deve ser usada por código de jogo. | Pode mudar livremente. |
| `Deferred` | Existe como planejamento ou código congelado. | Não faz parte do baseline ativo. |
| `Development Tooling` | Ferramenta de QA/editor/dev. | Não é API de produto. |
| `Removed` | Deve sair do package. | Remoção em corte técnico. |

Classificação inicial aceita para o baseline F0A:

| Superfície | Status |
|---|---|
| Bootstrap, Game Application, Route, Activity, Game Flow, Route request trigger e Activity request trigger | `Experimental` até F1/F3/F4 estabilizarem identities e states. |
| `ContentFlow` materializer/contribution | `Experimental`. |
| `RouteContentProfileAsset` | `Deferred / Planning-only`. |
| `RouteContentRuntime` | `Deferred`. |
| `CameraFlow` | `Deferred`. |
| `FrameworkQaCanvas` | `Development Tooling`. |
| `FrameworkLogger` | `Internal` fronteira técnica do framework. |
| `ValidationMode` | `Experimental`. |

## Consequências

### Positivas

- Reduz risco de API prematura.
- Deixa claro o que jogos podem usar com segurança.
- Permite manter vocabulário experimental sem declarar estabilidade falsa.

### Negativas / trade-offs

- Exige manutenção documental por fase.
- Pode gerar ruído se status for duplicado em excesso.
- F0B precisa alinhar comentários, README e Inspectors ao status aceito.

## Fora do escopo

- Garantir semver formal.
- Criar analyzer automático.
- Resolver packages opcionais.

## Critérios de validação

- `ContentFlow`, `RouteContentRuntime`, `CameraFlow`, `QA Canvas`, triggers e `ValidationMode` têm status claro.
- README e ADRs referenciam esta política.
- Nenhuma API nova entra sem status explícito.

## Impacto esperado

Necessário antes de expandir ContentFlow, identity, diagnostics e consumers.

## Relação com roadmap

F0A/F1.

## Notas de implementação

Inicialmente o status pode ser aplicado por documentação, comentários XML, namespaces, asmdefs ou Inspectors. Automação/analyzer fica fora de F0A.
