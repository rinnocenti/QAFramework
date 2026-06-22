# Route Local Callback Smoke

Status: APPLIED / PENDING COMPILE-SMOKE  
Roadmap: IF-FW-ROAD-3E — Route local callback smoke  
Cut: F3F  
Tipo: QA / Development Tooling  
Escopo: RouteContentRuntime callbacks

---

## Objetivo

F3F adiciona um smoke explícito para validar callbacks reais de `RouteContentRuntime`.

A F3D já provou que `RouteContentRuntime` executa o dispatch. F3F adiciona o caminho QA para provar que receivers locais realmente recebem:

```text
OnRouteContentExited
OnRouteContentEntered
```

## Novo componente de QA

```text
Runtime/Diagnostics/RouteContentLifecycleSmokeProbe.cs
```

Uso esperado em cena QA:

```text
RouteContentBinding
└── RouteContentLifecycleSmokeProbe
```

O probe é development-only e serve apenas para smoke. Ele não é API de produto, não possui ownership e não materializa conteúdo.

## Novo smoke no QA Canvas

`FrameworkQaCanvas` agora possui o preset:

```text
Run Route Callback Smoke
```

Esse smoke faz:

```text
1. request para Alternate Route
2. valida Route Content exit + enter com receivers reais
3. request para Canonical Route
4. valida Route Content exit + enter com receivers reais
```

Critério por step:

```text
routeContentEnter='Executed'
routeContentEnterBindings > 0
routeContentEnterReceivers > 0
routeContentEnterFailed='0'
routeContentExit='Executed'
routeContentExitBindings > 0
routeContentExitReceivers > 0
routeContentExitFailed='0'
```

O smoke também registra:

```text
QA Route Callback Smoke step completed.
QA Smoke completed. name='Route Callback Smoke'.
```

## Importante

F3F não cria nem modifica assets de cena. Se a cena QA não tiver `RouteContentBinding` com pelo menos um receiver local, o novo smoke deve falhar de forma visível. Isso é intencional: o objetivo é validar callbacks reais, não aceitar falso positivo por dispatch vazio.

## Fora do escopo

```text
additive scene loading
release policy
RouteContentProfile execution
Content Anchor
RuntimeMaterialization
LocalContributionSet
consumers
```

## Validação

Rodar primeiro o smoke padrão:

```text
Boot
Route Smoke
Activity Smoke
Clear Activity Smoke
```

Depois, com probes configurados nas cenas QA, rodar:

```text
Run Route Callback Smoke
```

Critério de fechamento:

```text
QA Smoke completed. name='Route Callback Smoke'
QA Route Callback Smoke step completed. step='alternate'
QA Route Callback Smoke step completed. step='canonical'
routeContentEnterReceivers > 0
routeContentExitReceivers > 0
```
