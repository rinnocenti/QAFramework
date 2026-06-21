# F1A-03 — ADR-CONTENT-001 — Content Identity Domain

Status: Accepted  
Fase: F1  
Ordem no Plano: F1A-03  
Tipo: ContentFlow  
Escopo: Content identity

---

## Contexto

`FrameworkContentHandle` e os futuros `SessionContentSet`, `RouteContentSet`, `ActivityContentSet`, `RuntimeContentHandle` e `SurfaceContentHandle` precisam de identidade estável.

No baseline atual, parte da identidade de conteúdo ainda pode derivar de path, scene name, resource label ou composição textual. Isso é aceitável como diagnóstico e etapa inicial, mas não deve virar contrato funcional público.

Este ADR aplica a política do `ADR-ID-001 — Typed Identity Policy` ao domínio de conteúdo.

## Decisão

Identidade de conteúdo deve ser composta por domínio explícito, escopo e valor estável.

Modelo conceitual:

| Parte | Papel |
|---|---|
| `ContentOwnerId` | Quem possui o conteúdo: Session, Route, Activity, Local, Runtime ou Surface owner. |
| `ContentScope` | Escopo de lifetime: Session, Route, Activity, Local, Runtime ou Transient. |
| `ContentKind` | Tipo de conteúdo: Scene, GameObject, Prefab, RuntimeObject, SurfaceBinding, DiagnosticOnly. |
| `ContentId` | Identidade funcional do conteúdo dentro do domínio. |

A identidade deve ser criada por fonte estável:

- referência de asset conhecida;
- scene asset/path validado quando o domínio for cena;
- explicit authoring id quando houver conteúdo local authored;
- runtime-generated id controlado pelo framework quando o objeto for materializado em runtime.

Fallback aleatório só pode ser usado como detalhe diagnóstico ou identidade transitória explicitamente marcada. Não pode ser contrato funcional estável para release, restore, stale checks ou requiredness.

## Regras

1. **Content identity não é path puro**  
   Path de cena ou asset pode participar da construção, mas o contrato deve deixar claro o domínio e o escopo.

2. **Owner e scope são parte do significado**  
   Dois conteúdos com mesmo nome/path em escopos diferentes não são automaticamente o mesmo conteúdo.

3. **Required content precisa identity válida**  
   Conteúdo required não pode receber fallback silencioso se a identidade não puder ser construída.

4. **Diagnostic-only deve ser explícito**  
   Handles criados apenas para observabilidade devem ser marcados como tal e não usados para ownership/release funcional.

5. **Release e stale checks dependem de identity estável**  
   Fases futuras de release/runtime não devem operar sobre nome de GameObject ou transform path.

## Política de aplicação incremental

Este ADR não exige reescrever todo o `ContentFlow` imediatamente.

Aplicação por fase:

| Fase | Aplicação |
|---|---|
| F1 | Criar política/tipos mínimos e revisar `FrameworkContentHandle`. |
| F2 | Aplicar a Session content mínimo. |
| F3 | Aplicar a Route content e RouteContentSet. |
| F4 | Aplicar a ActivityContentSet. |
| F5 | Integrar LocalContentIdentity. |
| F8/F9 | Integrar RuntimeContentHandle e SurfaceContentHandle. |

## Consequências

### Positivas

- Base consistente para ContentSet e release.
- Reduz órfãos e duplicidade de handles.
- Evita que path/name virem chave funcional universal.
- Prepara runtime materialization e surface binding.

### Negativas / trade-offs

- Exige revisão de `FrameworkContentHandle`.
- Exige cuidado com scene references e asset references.
- Pode exigir migração gradual de APIs experimentais existentes.

## Fora do escopo

- Resolver Addressables agora.
- Criar GUID global custom para todos os conteúdos.
- Implementar RuntimeMaterialization.
- Implementar SurfaceBinding.
- Implementar release completo.
- Conectar `RouteContentProfileAsset` como execução real.

## Critérios de validação

- `FrameworkContentHandle` não depende de path/name como única chave funcional pública.
- Required content não recebe fallback silencioso de identidade.
- Identidade transitória/diagnostic-only é marcada como tal.
- O modelo respeita `ADR-ID-001`.

## Impacto esperado

Afeta F1, F2, F3, F4, F5, F6, F8 e F9.

## Relação com roadmap

F1. Condiciona ContentFlow, RouteContentSet, ActivityContentSet, LocalContribution, RuntimeContentHandle e SurfaceContentHandle.

## Notas de implementação

A primeira implementação deve ser mínima e compatível com o baseline experimental. Não transformar este ADR em materializer genérico ou release system no F1.
