# F5-01 — ADR-LOCAL-001 — Local Identity

Status: Accepted  
Fase: F5  
Ordem no Plano: F5-01  
Tipo: Local / Identity  
Escopo: LocalContentIdentity  
Corte de aceite: F5A

---

## Contexto

A F4 fechou o baseline mínimo de Activity com `ActivityContentSet`, `ActivityReadinessState` e `ActivityLocalVisibilityAdapter`.

A fronteira de F4 é deliberada:

```text
ActivityLocalVisibilityAdapter = adapter local de visibilidade scene-authored.
ActivityContentSet = snapshot local/diagnóstico dos adapters de visibilidade.
```

Nenhum dos dois é materialização canônica de Activity e nenhum dos dois deve virar identidade funcional da F5.

A auditoria bruta do `NewScripts` mostra que o eixo local antigo usava `targetId` como cola textual para contributors, requirements, snapshot, restore, reset, release, placement, anchors e diagnostics. Esse padrão preserva capacidades úteis, mas cria acoplamento frágil por string.

A auditoria que abriu a F5 identificou outro risco: `FrameworkContentContributionMarker` e `IFrameworkContentContribution` existiam como API Experimental, sem discovery canônico nem consumer real. O marker também permitia fallback de `ContributionId` para `GameObject.name`, o que violava a política de identidade aceita na F1/F5. O F5C remove esse precursor em vez de manter trilho paralelo.

Portanto F5 precisa começar por identidade local própria antes de marker, discovery, requiredness ou contribution set.

---

## Decisão

Definir `LocalContentIdentity` como identidade funcional própria para contribuições locais scene-authored dentro de um escopo conhecido.

`LocalContentIdentity` identifica uma contribuição local. Ela não identifica qualquer objeto do jogo de forma universal e não substitui identidades de Route, Activity, Session, RuntimeContent, Content Anchor ou Actor.

Composição conceitual:

```text
LocalContentIdentity =
  scope owner identity
  + local scope kind
  + explicit local id
```

Exemplo conceitual:

```text
Activity:QA_PrimaryContentActivity
+ SceneAuthoredLocal
+ primary-panel
```

Forma textual diagnóstica esperada:

```text
local:<scope-domain>:<scope-owner>:<local-scope-kind>:<local-id>
```

A forma textual é para logs, diagnostics e comparação de smoke. A API técnica deve ser tipo de valor imutável e validável, não string solta.

---

## Regras obrigatórias

### 1. Sem fallback funcional por nome/path

Não usar como chave funcional:

```text
GameObject.name
Transform path
Hierarchy path
Scene path
Scene name
Component path
ownerPath
componentPath
targetId universal genérico
```

Esses valores podem existir apenas como diagnostics, labels, observability ou mensagem de validator.

### 2. Identidade local é explícita quando houver marker

Se um marker local existir, a identidade local deve ser obrigatória e explícita.

Ausência de identidade em marker local futuro deve gerar falha de validação estruturada. Não pode gerar fallback silencioso.

### 3. `targetId` não é modelo canônico

O padrão do `NewScripts` deve ser migrado conceitualmente assim:

| Padrão antigo | Decisão F5 |
|---|---|
| `targetId` como cola universal | Substituir por `LocalContentIdentity` tipada por escopo. |
| `sceneName + targetId` | Usar content set/entry como contexto de discovery e `LocalContentIdentity` como chave funcional. |
| `roleId` | Metadata/diagnóstico; não chave primária. |
| `componentPath` / `ownerPath` | Diagnóstico apenas. |
| `placementId`, `slotId`, `anchorId` | Tipos específicos futuros; não colapsar em targetId universal. |

### 4. `ActivityContentSet` F4 não é identidade F5

`ActivityContentSet` pode delimitar fonte/escopo de discovery em F5, mas não fornece a identidade funcional da contribuição.

Em particular, qualquer handle derivado de `sceneName`, `objectName`, hierarchy ou path dentro do snapshot F4 permanece diagnóstico/local ao adapter de visibilidade.

### 5. Precursor genérico não permanece como contrato F5

`FrameworkContentContributionMarker` e `IFrameworkContentContribution` eram precursores experimentais sem consumer real e com risco de fallback funcional por `GameObject.name`.

A linha F5 não deve manter esse trilho paralelo quando os marcadores reais de cena já existem:

```text
RouteContentBinding
ActivityLocalVisibilityAdapter
```

Decisão aplicada no F5C: remover o precursor genérico obsoleto e acrescentar `Local Content Id` explícito nos bindings/adapters scene-authored existentes.

Consequência: a identidade local entra onde o autor já configura a cena hoje. Não há componente `LocalContributionMarker` separado no F5C.

---

## Semântica de escopo

A identidade local é única dentro de um escopo funcional definido.

Escopos admitidos conceitualmente:

```text
Session local content
Route local content
Activity local content
Scene-authored local content dentro de um ContentSet conhecido
```

Escopo não deve ser inferido apenas por cena ou hierarquia. A cena carregada pode ser contexto de discovery, mas não o owner funcional da identidade.

---

## Relação com handles existentes

### `FrameworkContentIdentity`

`FrameworkContentIdentity` continua identificando handles de content em ContentFlow.

`LocalContentIdentity` é menor e mais específica: identifica uma contribuição local dentro de content conhecido.

### `RouteContentSet` / `ActivityContentSet`

Sets existentes podem fornecer fronteira de busca e diagnóstico.

Eles não definem, por si só, contribuição local, capability, requiredness ou runtime reference.

### Futuro `LocalContributionHandle`

`LocalContributionHandle` deve carregar `LocalContentIdentity` e metadata de contribuição, mas só deve entrar depois do tipo de identidade.

---

## Consequências

### Positivas

- Remove pressão para ressuscitar `targetId` como cola universal.
- Impede fallback invisível por rename de GameObject.
- Permite validators determinísticos.
- Cria base segura para `ActivityLocalVisibilityAdapter` / `RouteContentBinding`, `LocalContributionSet`, requiredness e Content Anchor.
- Mantém labels e paths úteis como diagnostics sem virarem contrato.

### Negativas / trade-offs

- Authoring local passa a exigir id explícito em markers.
- F5B precisa criar tipo novo antes de qualquer discovery útil.
- Markers experimentais existentes não podem ser reaproveitados sem ajuste.
- Migração futura do shape antigo precisará mapear `targetId` para identidade local tipada.

---

## Fora do escopo

F5A não implementa:

```text
LocalContentIdentity.cs
Local contribution authoring
LocalContributionDiscovery
LocalContributionSet
LocalContributionHandle
Local validators runtime/editor
ActivityContentProfile loading
Canonical Activity materialization
Additive Activity loading
Release/unload policy
RuntimeMaterialization
Content Anchor
Actors
Input
Camera
Reset
Snapshot
Save
Pooling
```

---

## Critérios de validação para cortes técnicos posteriores

- Binding/adapter scene-authored sem `Local Content Id` explícito falha em validation.
- Duplicidade da mesma identidade no mesmo escopo falha em validation.
- Paths/nomes aparecem apenas em diagnostics.
- `ActivityContentSet` F4 pode ser usado como fonte de escopo, mas não como chave funcional.
- `FrameworkContentContributionMarker`/`IFrameworkContentContribution` não permanecem como API paralela; o precursor obsoleto é removido.
- Required ausente deve falhar por policy estruturada quando requiredness entrar.
- Optional ausente deve gerar skip diagnosticado quando requiredness entrar.

---

## Impacto esperado no roadmap

F5 deve seguir esta ordem:

```text
F5A — ADR Local Identity
F5B — LocalContentIdentity type
F5C — explicit local ids on scene-authored bindings
F5D — Scoped discovery limitado a ContentSets conhecidos
F5E — LocalContributionSet
F5F — Required/Optional policy
F5G — Local validators
F5H — Local smoke
```

F5B não deve criar discovery. F5C não deve criar capability system. F5D não deve usar scan global como fonte de verdade.

---

## Relação com ADRs anteriores

Este ADR aplica:

```text
F1A-01 — ADR-ID-001 — Typed Identity Policy
F1A-03 — ADR-CONTENT-001 — Content Identity Domain
F4-01  — ADR-ACTIVITY-001 — ActivityContentSet and Readiness Baseline
```

A decisão reforça que strings podem existir como labels/diagnostics, mas não como chave funcional sem domínio explícito.
